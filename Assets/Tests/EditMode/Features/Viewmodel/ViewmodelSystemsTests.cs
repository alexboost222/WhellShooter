using Engine.Configs;
using Features.CharacterFeature.Components;
using Features.ViewmodelFeature.Components;
using Features.ViewmodelFeature.Configs;
using Features.ViewmodelFeature.Systems;
using Features.WeaponsFeature.Components;
using Features.WeaponsFeature.Configs;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Viewmodel
{
    /// <summary>
    /// The viewmodel's layering, end to end on a headless world: blends → motion → recoil → springs →
    /// pose. Only the Transform-writing ends (spawn, apply) are left to play mode.
    /// </summary>
    public sealed class ViewmodelSystemsTests
    {
        private const float Frame = 1f / 60f;

        private EcsTestWorld _world;
        private ViewmodelConfig _config;
        private WeaponDefinition _rifle;
        private Entity _body;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ViewmodelConfig>();
            _rifle = new WeaponDefinition { Name = "Rifle" };
            var weapons = ScriptableObject.CreateInstance<WeaponsConfig>();
            weapons.SetWeapons("Rifle", _rifle);

            _world = new EcsTestWorld(_config, weapons);
            _world.Add<ViewmodelBlendSystem>();
            _world.Add<ViewmodelMotionSystem>();
            _world.Add<ViewmodelRecoilSystem>();
            _world.Add<ViewmodelSpringSystem>();
            _world.Add<ViewmodelPoseSystem>();

            _body = _world.World.CreateEntity();
            _world.Set(_body, new CharacterMotion { Grounded = true });
            _world.Set(_body, new Look());
            _world.Set(_body, new Weapon { ConfigId = ConfigIds.Hash("Rifle") });
            _world.Set(_body, new ViewmodelState());
            _world.Set(_body, new ViewmodelSprings());
            _world.Set(_body, new ViewmodelPose());
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        private ref ViewmodelPose Pose => ref _world.Get<ViewmodelPose>(_body);
        private ref ViewmodelState State => ref _world.Get<ViewmodelState>(_body);
        private ref ViewmodelSprings Springs => ref _world.Get<ViewmodelSprings>(_body);

        [Test]
        public void AtRest_TheWeaponSitsAtTheHipPose_AtHipFieldOfView()
        {
            _world.Tick(Frame, 120);

            // Breathing is the only thing moving, and it is millimetres.
            Assert.That(math.distance(Pose.WeaponPosition, _rifle.Viewmodel.HipPosition), Is.LessThan(0.005f));
            Assert.That(Pose.FieldOfView, Is.EqualTo(_config.HipFieldOfView).Within(1e-3f));
        }

        [Test]
        public void Aiming_ReachesTheSightsInAimTime_AndNarrowsTheView()
        {
            _world.Get<Weapon>(_body).Aiming = true;

            _world.Tick(Frame, Mathf.CeilToInt(_rifle.Viewmodel.AimTime / Frame) + 1);

            Assert.That(State.AimBlend, Is.EqualTo(1f));
            Assert.That(math.distance(Pose.WeaponPosition, _rifle.Viewmodel.AimPosition), Is.LessThan(0.002f));
            Assert.That(Pose.FieldOfView, Is.EqualTo(_rifle.Viewmodel.AimFieldOfView).Within(1e-3f));
        }

        [Test]
        public void Sprinting_MovesToTheSprintPose()
        {
            _world.Get<CharacterMotion>(_body).Sprinting = true;

            _world.Tick(Frame, 60);

            Assert.That(State.SprintBlend, Is.EqualTo(1f));
            Assert.That(math.distance(Pose.WeaponPosition, _rifle.Viewmodel.SprintPosition), Is.LessThan(0.005f));
            Assert.That(Pose.FieldOfView, Is.GreaterThan(_config.HipFieldOfView));
        }

        [Test]
        public void AShot_KicksTheWeaponBackAndThenItSettles()
        {
            _world.Tick(Frame);
            _world.Get<Weapon>(_body).Shots = 1;

            _world.Tick(Frame, 3);
            Assert.That(Springs.RecoilPosition.Value.z, Is.LessThan(-0.005f), "kicked towards the face");
            Assert.That(Springs.CameraRotation.Value.x, Is.LessThan(0f), "view punched up");
            Assert.That(State.SeenShots, Is.EqualTo(1));

            _world.Tick(Frame, 120);
            Assert.That(math.length(Springs.RecoilPosition.Value), Is.LessThan(1e-3f));
            Assert.That(math.length(Springs.CameraRotation.Value), Is.LessThan(1e-3f));
        }

        [Test]
        public void SeveralShotsInOneFrame_KickHarderThanOne()
        {
            _world.Tick(Frame);
            _world.Get<Weapon>(_body).Shots = 1;
            _world.Tick(Frame);
            var one = Springs.RecoilPosition.Velocity.z;

            _world.Dispose();
            SetUp();
            _world.Tick(Frame);
            _world.Get<Weapon>(_body).Shots = 3;
            _world.Tick(Frame);

            Assert.That(Springs.RecoilPosition.Velocity.z, Is.LessThan(one * 2f));
        }

        [Test]
        public void Aiming_SoftensRecoil()
        {
            _world.Tick(Frame);
            _world.Get<Weapon>(_body).Shots = 1;
            _world.Tick(Frame);
            var hip = Springs.RecoilPosition.Velocity.z;

            _world.Dispose();
            SetUp();
            State.AimBlend = 1f;
            _world.Get<Weapon>(_body).Aiming = true;
            _world.Tick(Frame);
            _world.Get<Weapon>(_body).Shots = 1;
            _world.Tick(Frame);

            Assert.That(Springs.RecoilPosition.Velocity.z, Is.GreaterThan(hip), "smaller kick towards the face");
        }

        [Test]
        public void Turning_SwaysTheWeaponAgainstTheTurn()
        {
            _world.Tick(Frame);

            for (var i = 0; i < 10; i++)
            {
                _world.Get<Look>(_body).Yaw += math.radians(3f);
                _world.Tick(Frame);
            }

            Assert.That(Springs.MotionTargetRotation.y, Is.LessThan(0f));
            Assert.That(Springs.MotionRotation.Value.y, Is.LessThan(0f));
        }

        [Test]
        public void TheFirstFrame_DoesNotSwayFromWhereverLookStarted()
        {
            _world.Get<Look>(_body).Yaw = 3f;

            _world.Tick(Frame);

            Assert.That(Springs.MotionTargetRotation, Is.EqualTo(float3.zero));
        }

        [Test]
        public void AHardLanding_DipsTheWeaponAndTheCamera_ASoftOneLess()
        {
            _world.Tick(Frame);
            ref var motion = ref _world.Get<CharacterMotion>(_body);
            motion.Landings = 1;
            motion.LandingSpeed = _config.LandReferenceSpeed;
            _world.Tick(Frame);
            var hard = Springs.MotionPosition.Velocity.y;

            Assert.That(hard, Is.LessThan(0f));
            Assert.That(Springs.CameraRotation.Velocity.x, Is.GreaterThan(0f), "camera nods down");

            _world.Tick(Frame, 120);
            ref var soft = ref _world.Get<CharacterMotion>(_body);
            soft.Landings = 2;
            soft.LandingSpeed = _config.LandReferenceSpeed * 0.25f;
            _world.Tick(Frame);

            Assert.That(Springs.MotionPosition.Velocity.y, Is.GreaterThan(hard));
        }

        [Test]
        public void Walking_Bobs_StandingStill_DoesNot()
        {
            _world.Get<CharacterMotion>(_body).Velocity = new float3(0f, 0f, 4.5f);
            _world.Tick(Frame, 60);

            Assert.That(State.BobWeight, Is.GreaterThan(0.9f));
            var phase = State.BobPhase;
            _world.Tick(Frame);
            Assert.That(State.BobPhase, Is.Not.EqualTo(phase));

            _world.Get<CharacterMotion>(_body).Velocity = float3.zero;
            _world.Tick(Frame, 60);
            Assert.That(State.BobWeight, Is.LessThan(0.01f));
        }

        [Test]
        public void InTheAir_ThereIsNoBob()
        {
            ref var motion = ref _world.Get<CharacterMotion>(_body);
            motion.Velocity = new float3(0f, 0f, 4.5f);
            motion.Grounded = false;

            _world.Tick(Frame, 60);

            Assert.That(State.BobWeight, Is.LessThan(0.01f));
        }
    }
}

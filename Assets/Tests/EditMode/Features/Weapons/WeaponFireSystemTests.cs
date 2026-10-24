using Engine.Configs;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.WeaponsFeature.Components;
using Features.WeaponsFeature.Configs;
using Features.WeaponsFeature.Systems;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;
using CharacterTag = Features.CharacterFeature.Components.Character;

namespace Game.Tests.EditMode.Features.Weapons
{
    public sealed class WeaponFireSystemTests
    {
        private const float Tick = 0.02f;

        private EcsTestWorld _world;
        private WeaponsConfig _weapons;
        private Entity _body;

        [SetUp]
        public void SetUp()
        {
            _weapons = ScriptableObject.CreateInstance<WeaponsConfig>();
            _weapons.SetWeapons("Rifle",
                new WeaponDefinition { Name = "Rifle", RoundsPerMinute = 600f, Automatic = true },
                new WeaponDefinition { Name = "Pistol", RoundsPerMinute = 600f, Automatic = false });

            _world = new EcsTestWorld(_weapons, ScriptableObject.CreateInstance<CharacterConfig>());
            _world.Add<WeaponEquipSystem>();
            _world.Add<WeaponFireSystem>();

            _body = _world.World.CreateEntity();
            _world.Set(_body, new CharacterTag());
            _world.Set(_body, new CharacterIntent());
            _world.Set(_body, new CharacterMotion { Position = new float3(1f, 0f, 2f) });
            _world.Set(_body, new Look { Yaw = math.radians(90f) });
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void ABodyWithoutAWeapon_IsGivenTheDefault()
        {
            _world.Tick(Tick);

            Assert.That(_world.Get<Weapon>(_body).ConfigId, Is.EqualTo(ConfigIds.Hash("Rifle")));
        }

        [Test]
        public void Automatic_FiresAtItsRate_WhileHeld()
        {
            _world.Get<CharacterIntent>(_body).Fire = true;

            // 600 rpm = one shot per 0.1 s = per 5 ticks; 50 ticks is one second.
            _world.Tick(Tick, 50);

            Assert.That(_world.Get<Weapon>(_body).Shots, Is.EqualTo(10));
        }

        [Test]
        public void SemiAutomatic_FiresOncePerPress()
        {
            Equip("Pistol");
            ref var intent = ref _world.Get<CharacterIntent>(_body);
            intent.Fire = true;
            intent.FirePressed = true;

            _world.Tick(Tick, 50);

            Assert.That(_world.Get<Weapon>(_body).Shots, Is.EqualTo(1));
            Assert.That(_world.Get<CharacterIntent>(_body).FirePressed, Is.False, "the latch is consumed");
        }

        [Test]
        public void ATapShorterThanATick_StillFires()
        {
            _world.Get<CharacterIntent>(_body).FirePressed = true;

            _world.Tick(Tick);

            Assert.That(_world.Get<Weapon>(_body).Shots, Is.EqualTo(1));
        }

        [Test]
        public void AShot_LeavesARequestFromTheEyeAlongTheLook()
        {
            _world.Get<CharacterIntent>(_body).FirePressed = true;

            _world.Tick(Tick);

            Assert.That(_world.Has<ShotRequest>(_body), Is.True);
            var shot = _world.Get<ShotRequest>(_body);
            Assert.That(shot.Origin, Is.EqualTo(new float3(1f, 1.6f, 2f)));
            Assert.That(shot.Direction.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(shot.WeaponId, Is.EqualTo(ConfigIds.Hash("Rifle")));
        }

        [Test]
        public void Sprinting_NeitherFiresNorAims()
        {
            _world.Get<CharacterMotion>(_body).Sprinting = true;
            ref var intent = ref _world.Get<CharacterIntent>(_body);
            intent.Fire = true;
            intent.Aim = true;

            _world.Tick(Tick, 10);

            Assert.That(_world.Get<Weapon>(_body).Shots, Is.EqualTo(0));
            Assert.That(_world.Get<Weapon>(_body).Aiming, Is.False);
        }

        [Test]
        public void ReleasingTheTrigger_DoesNotBankShots()
        {
            _world.Tick(Tick, 50);

            ref var intent = ref _world.Get<CharacterIntent>(_body);
            intent.Fire = true;
            _world.Tick(Tick);

            Assert.That(_world.Get<Weapon>(_body).Shots, Is.EqualTo(1));
        }

        [Test]
        public void Aim_FollowsTheIntent()
        {
            _world.Get<CharacterIntent>(_body).Aim = true;

            _world.Tick(Tick);

            Assert.That(_world.Get<Weapon>(_body).Aiming, Is.True);
        }

        private void Equip(string weapon)
        {
            _world.Set(_body, new Weapon { ConfigId = ConfigIds.Hash(weapon) });
        }
    }
}

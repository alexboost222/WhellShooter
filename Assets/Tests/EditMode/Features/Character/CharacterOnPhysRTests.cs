using Engine.Configs;
using Engine.DI;
using Engine.Ecs;
using Engine.Messages;
using Engine.Physics;
using Engine.Runtime;
using Features.Bodies;
using Features.Bodies.Components;
using Features.Bodies.Enums;
using Features.CharacterFeature;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.DestroyFeature;
using NUnit.Framework;
using PhysR;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Character
{
    /// <summary>
    /// CharacterFeature driving a real phys-r capsule (Docs/11): intent and look in, CharacterMotion
    /// out, ticked through the Runner so the physics hook and AfterPhysics run as in play.
    /// </summary>
    public sealed class CharacterOnPhysRTests
    {
        private const float Dt = 0.02f;

        private World _world;
        private Runner _runner;
        private PhysicsWorld _physics;
        private CharacterConfig _config;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            var bus = new MessageBus();
            _runner = new Runner(bus);
            _physics = new PhysicsWorld(50);
            _config = ScriptableObject.CreateInstance<CharacterConfig>();

            var container = new Container();
            var catalog = new ConfigCatalog(new Config[] { _config });
            container.Register(catalog);
            container.Register(_world);
            container.Register(bus);
            container.Register(_runner);
            container.Register(_physics);
            container.Register(new TimeService());
            container.AddResolver(new ConfigResolver(catalog));
            container.AddResolver(new StashResolver(_world));
            container.AddResolver(new MessagePipeResolver(bus));

            new BodiesFeature().Install(container, _runner);
            new CharacterFeature().Install(container, _runner);
            new EntitiesDestroyFeature().Install(container, _runner);
            _runner.Initialize();

            var floor = _world.CreateEntity();
            floor.SetComponent(new BodyCreateRequest
            {
                Kind = BodyKind.Static,
                Shape = BodyShape.Box,
                Size = new float3(50f, 0.5f, 50f),
                Position = new float3(0f, -0.5f, 0f),
            });
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            _physics.Dispose();
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void ABody_GetsItsCapsule_AndStandsOnTheFloor()
        {
            var body = Body(new float3(1f, 0f, 2f));

            Tick(10);

            Assert.That(body.Has<PhysicsCharacter>(), Is.True);
            var motion = body.GetComponent<CharacterMotion>();
            Assert.That(motion.Grounded, Is.True);
            Assert.That(motion.Position.x, Is.EqualTo(1f).Within(0.01f));
            Assert.That(motion.Position.y, Is.EqualTo(0f).Within(0.05f));
        }

        [Test]
        public void ForwardIntent_WalksAtWalkSpeed_AlongTheLook()
        {
            var body = Body(float3.zero);
            body.SetComponent(new Look { Yaw = math.PI / 2f });
            Tick(5);

            Hold(body, new CharacterIntent { Move = new float2(0f, 1f) }, 100);

            var motion = body.GetComponent<CharacterMotion>();
            Assert.That(math.length(motion.Velocity.xz), Is.EqualTo(_config.WalkSpeed).Within(0.15f));
            Assert.That(motion.Velocity.x, Is.GreaterThan(0f), "yaw +90° faces +X");
            Assert.That(motion.PreviousPosition.x, Is.LessThan(motion.Position.x), "this tick's result, not last tick's");
        }

        [Test]
        public void Sprinting_AndAiming_ChangeTheTopSpeed()
        {
            var sprinter = Body(new float3(-5f, 0f, 0f));
            var aimer = Body(new float3(5f, 0f, 0f));
            Tick(5);

            for (var i = 0; i < 100; i++)
            {
                sprinter.SetComponent(new CharacterIntent { Move = new float2(0f, 1f), Sprint = true });
                aimer.SetComponent(new CharacterIntent { Move = new float2(0f, 1f), Aim = true });
                Tick();
            }

            Assert.That(sprinter.GetComponent<CharacterMotion>().Sprinting, Is.True);
            Assert.That(math.length(sprinter.GetComponent<CharacterMotion>().Velocity.xz),
                Is.EqualTo(_config.SprintSpeed).Within(0.2f));
            Assert.That(math.length(aimer.GetComponent<CharacterMotion>().Velocity.xz),
                Is.EqualTo(_config.AimSpeed).Within(0.15f));
        }

        [Test]
        public void AJump_ReachesTheConfiguredHeight_AndCountsTakeOffAndLanding()
        {
            var body = Body(float3.zero);
            Tick(10);
            var before = body.GetComponent<CharacterMotion>();

            body.SetComponent(new CharacterIntent { JumpPressed = true });
            Tick();
            Assert.That(body.GetComponent<CharacterIntent>().JumpPressed, Is.False, "the latch is consumed");

            var apex = 0f;
            for (var i = 0; i < 80; i++)
            {
                Tick();
                apex = math.max(apex, body.GetComponent<CharacterMotion>().Position.y);
            }

            var motion = body.GetComponent<CharacterMotion>();
            Assert.That(apex, Is.EqualTo(_config.JumpHeight).Within(0.1f));
            Assert.That(motion.Jumps - before.Jumps, Is.EqualTo(1));
            Assert.That(motion.Landings - before.Landings, Is.EqualTo(1), "settling after the spawn counts as its own landing");
            Assert.That(motion.Grounded, Is.True);
        }

        [Test]
        public void Teleport_MovesTheCapsule()
        {
            var body = Body(float3.zero);
            Tick(5);

            body.SetComponent(new CharacterTeleportRequest { Position = new float3(10f, 0f, -4f), Yaw = 1f });
            Tick(3);

            var motion = body.GetComponent<CharacterMotion>();
            Assert.That(motion.Position.x, Is.EqualTo(10f).Within(0.01f));
            Assert.That(motion.Position.z, Is.EqualTo(-4f).Within(0.01f));
        }

        private Entity Body(float3 feet)
        {
            var entity = _world.CreateEntity();
            entity.SetComponent(new global::Features.CharacterFeature.Components.Character());
            entity.SetComponent(new CharacterIntent());
            entity.SetComponent(new Look());
            entity.SetComponent(new CharacterMotion { Position = feet, PreviousPosition = feet });
            return entity;
        }

        private void Hold(Entity body, CharacterIntent intent, int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                body.SetComponent(intent);
                Tick();
            }
        }

        private void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                _runner.FixedTick(Dt);
            }
        }
    }
}

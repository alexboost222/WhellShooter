using Engine.Configs;
using Engine.DI;
using Engine.Ecs;
using Engine.Messages;
using Engine.Physics;
using Engine.Runtime;
using Features.Bodies;
using Features.Bodies.Components;
using Features.Bodies.Enums;
using Features.Bodies.Systems;
using Features.DestroyFeature;
using NUnit.Framework;
using PhysR;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Game.Tests.EditMode.Features.Bodies
{
    /// <summary>
    /// BodiesFeature against a headless world and a real phys-r world, ticked by hand through the
    /// Runner (so the physics hook runs exactly as in play): requests in, links and mirrors out.
    /// </summary>
    public sealed class BodiesFeatureTests
    {
        private const float Dt = 0.02f;

        private World _world;
        private Runner _runner;
        private Container _container;
        private PhysicsWorld _physics;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            var bus = new MessageBus();
            _runner = new Runner(bus);
            _physics = new PhysicsWorld(50);

            _container = new Container();
            var catalog = new ConfigCatalog(new Config[0]);
            _container.Register(catalog);
            _container.Register(_world);
            _container.Register(bus);
            _container.Register(_runner);
            _container.Register(_physics);
            _container.AddResolver(new ConfigResolver(catalog));
            _container.AddResolver(new StashResolver(_world));
            _container.AddResolver(new MessagePipeResolver(bus));

            new BodiesFeature().Install(_container, _runner);
            new EntitiesDestroyFeature().Install(_container, _runner);
            _runner.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            _physics.Dispose();
        }

        // --- bodies ---------------------------------------------------------------------------

        [Test]
        public void BodyRequest_IsConsumed_AndTheBodyIsLinked()
        {
            var entity = StaticBox(new float3(0f, 0f, 10f), new float3(1f));

            Tick();

            Assert.That(entity.Has<BodyCreateRequest>(), Is.False);
            Assert.That(entity.Has<PhysicsBody>(), Is.True);
            Assert.That(entity.Has<BodyPose>(), Is.False, "static bodies have no pose to mirror");

            Assert.That(_physics.Raycast(float3.zero, new float3(0f, 0f, 1f), 100f, out var hit), Is.True);
            Assert.That(hit.Body, Is.EqualTo(entity.GetComponent<PhysicsBody>().Handle));
        }

        [Test]
        public void ADynamicBox_FallsAndComesToRestOnTheFloor()
        {
            Floor();
            var box = _world.CreateEntity();
            box.SetComponent(new BodyCreateRequest
            {
                Kind = BodyKind.Dynamic,
                Shape = BodyShape.Box,
                Size = new float3(0.5f),
                Position = new float3(0f, 3f, 0f),
                Mass = 10f,
            });

            Tick();
            Assert.That(box.GetComponent<BodyPose>().Position.y, Is.LessThan(3f), "it falls from the first step");

            Tick(150);

            var pose = box.GetComponent<BodyPose>();
            Assert.That(pose.Position.y, Is.EqualTo(0.5f).Within(0.05f));
            Assert.That(pose.PreviousPosition.y, Is.EqualTo(pose.Position.y).Within(0.01f));
        }

        [Test]
        public void TaggingDestroy_ReleasesTheNativeBody()
        {
            var entity = StaticBox(new float3(0f, 0f, 10f), new float3(1f));
            Tick();

            entity.SetComponent(new Destroy());
            Tick();

            Assert.That(_physics.Raycast(float3.zero, new float3(0f, 0f, 1f), 100f, out _), Is.False);
            Assert.That(_container.Resolve<BodiesStorage>().Count, Is.Zero);
        }

        [Test]
        public void RemovingTheLink_ReleasesTheNativeBody()
        {
            var entity = StaticBox(new float3(0f, 0f, 10f), new float3(1f));
            Tick();

            entity.RemoveComponent<PhysicsBody>();
            Tick();

            Assert.That(_physics.Raycast(float3.zero, new float3(0f, 0f, 1f), 100f, out _), Is.False);
        }

        [Test]
        public void IdenticalShapes_AreShared()
        {
            StaticBox(new float3(0f, 0f, 10f), new float3(1f));
            StaticBox(new float3(5f, 0f, 10f), new float3(1f));
            StaticBox(new float3(9f, 0f, 10f), new float3(2f));

            Tick();

            Assert.That(_physics.Native.ShapeCount, Is.EqualTo(2));
        }

        // --- characters -----------------------------------------------------------------------

        [Test]
        public void ACharacter_StandsWhereItsFeetWereAsked()
        {
            Floor();
            var character = Character(new float3(2f, 0f, 3f));

            Tick(10);

            Assert.That(character.Has<CharacterCreateRequest>(), Is.False);
            Assert.That(character.Has<PhysicsCharacter>(), Is.True);

            var state = character.GetComponent<PhysicsCharacterState>();
            Assert.That(state.Grounded, Is.True);
            Assert.That(state.Feet.x, Is.EqualTo(2f).Within(0.01f));
            Assert.That(state.Feet.y, Is.EqualTo(0f).Within(0.05f));
            Assert.That(state.Feet.z, Is.EqualTo(3f).Within(0.01f));
        }

        [Test]
        public void ForwardInput_WalksAlongTheYaw()
        {
            Floor();
            var north = Character(new float3(-5f, 0f, 0f));
            var east = Character(new float3(5f, 0f, 0f));

            for (var i = 0; i < 50; i++)
            {
                north.SetComponent(new PhysicsCharacterInput { Move = new float2(0f, 1f), Yaw = 0f });
                east.SetComponent(new PhysicsCharacterInput { Move = new float2(0f, 1f), Yaw = math.PI / 2f });
                Tick();
            }

            var a = north.GetComponent<PhysicsCharacterState>().Feet;
            Assert.That(a.z, Is.GreaterThan(2f));
            Assert.That(a.x, Is.EqualTo(-5f).Within(0.05f));

            var b = east.GetComponent<PhysicsCharacterState>().Feet;
            Assert.That(b.x, Is.GreaterThan(7f));
            Assert.That(b.z, Is.EqualTo(0f).Within(0.05f));
        }

        [Test]
        public void ACharacterWithoutInput_StandsStill()
        {
            Floor();
            var character = Character(float3.zero);
            Tick(5);

            character.SetComponent(new PhysicsCharacterInput { Move = new float2(0f, 1f) });
            Tick(20);
            character.RemoveComponent<PhysicsCharacterInput>();
            Tick(100);

            Assert.That(math.length(character.GetComponent<PhysicsCharacterState>().Velocity), Is.LessThan(0.01f),
                "last tick's input must not repeat");
        }

        [Test]
        public void MaxSpeedScale_CapsTheTopSpeed()
        {
            Floor();
            var character = _world.CreateEntity();
            character.SetComponent(new CharacterCreateRequest { WalkSpeed = 4.5f });

            for (var i = 0; i < 150; i++)
            {
                character.SetComponent(new PhysicsCharacterInput { Move = new float2(0f, 1f), MaxSpeedScale = 0.5f });
                Tick();
            }

            var speed = math.length(character.GetComponent<PhysicsCharacterState>().Velocity.xz);
            Assert.That(speed, Is.EqualTo(2.25f).Within(0.1f));
        }

        [Test]
        public void JumpHeight_SetsTheApex()
        {
            Floor();
            var character = _world.CreateEntity();
            character.SetComponent(new CharacterCreateRequest { Gravity = 22f, JumpHeight = 1.1f });
            Tick(10);

            character.SetComponent(new PhysicsCharacterInput { Jump = true });
            Tick();
            Assert.That(character.GetComponent<PhysicsCharacterState>().Jumped, Is.True);
            character.RemoveComponent<PhysicsCharacterInput>();

            var apex = 0f;
            var landed = false;
            for (var i = 0; i < 100 && !landed; i++)
            {
                Tick();
                var state = character.GetComponent<PhysicsCharacterState>();
                apex = math.max(apex, state.Feet.y);
                landed = state.Landed;
            }

            Assert.That(apex, Is.EqualTo(1.1f).Within(0.1f));
            Assert.That(landed, Is.True);
        }

        [Test]
        public void Teleport_SnapsTheFeet_AndThePreviousFeet()
        {
            Floor();
            var character = Character(float3.zero);
            Tick(5);

            character.SetComponent(new PhysicsTeleportRequest { Feet = new float3(10f, 0f, -4f) });
            Tick();

            var state = character.GetComponent<PhysicsCharacterState>();
            Assert.That(character.Has<PhysicsTeleportRequest>(), Is.False);
            Assert.That(state.Feet.x, Is.EqualTo(10f).Within(0.01f));
            Assert.That(state.Feet.z, Is.EqualTo(-4f).Within(0.01f));
            Assert.That(state.PreviousFeet.x, Is.EqualTo(10f).Within(0.01f), "nothing interpolates across a teleport");
        }

        [Test]
        public void ACharacterIsHitByRays_ThroughItsBody()
        {
            Floor();
            var character = Character(new float3(0f, 0f, 5f));
            Tick();

            Assert.That(_physics.Raycast(new float3(0f, 1f, 0f), new float3(0f, 0f, 1f), 100f, out var hit), Is.True);
            Assert.That(hit.Body, Is.EqualTo(character.GetComponent<PhysicsCharacter>().Body));

            var staticOnly = new QueryFilter(uint.MaxValue, default, QueryFlags.ExcludeMoving);
            Assert.That(_physics.Raycast(new float3(0f, 1f, 0f), new float3(0f, 0f, 1f), 100f, staticOnly, out _),
                Is.False, "a shot occluder sees the static world only");
        }

        // --- helpers --------------------------------------------------------------------------

        private void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                _runner.FixedTick(Dt);
            }
        }

        private void Floor()
        {
            StaticBox(new float3(0f, -0.5f, 0f), new float3(50f, 0.5f, 50f));
        }

        private Entity StaticBox(float3 center, float3 halfExtents)
        {
            var entity = _world.CreateEntity();
            entity.SetComponent(new BodyCreateRequest
            {
                Kind = BodyKind.Static,
                Shape = BodyShape.Box,
                Size = halfExtents,
                Position = center,
            });
            return entity;
        }

        private Entity Character(float3 feet)
        {
            var entity = _world.CreateEntity();
            entity.SetComponent(new CharacterCreateRequest { Feet = feet });
            return entity;
        }
    }
}

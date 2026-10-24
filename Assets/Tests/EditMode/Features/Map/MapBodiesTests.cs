using System.Collections.Generic;
using Engine.Configs;
using Engine.DI;
using Engine.Ecs;
using Engine.Messages;
using Engine.Physics;
using Engine.Runtime;
using Features.Bodies;
using Features.Bodies.Components;
using Features.Bodies.Systems;
using Features.DestroyFeature;
using Features.Map;
using Features.Map.Components;
using Features.Map.Configs;
using Features.Map.Enums;
using Features.Map.Messages;
using Features.Map.Systems;
using Features.Map.Utils;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Map
{
    /// <summary>
    /// The generated arena in a real phys-r world (Docs/11): Map + Bodies on a headless world,
    /// ticked through the Runner. Characters walk through doors, into walls and up the stairs.
    /// </summary>
    public sealed class MapBodiesTests
    {
        private const float Dt = 0.02f;

        private World _world;
        private Runner _runner;
        private Container _container;
        private PhysicsWorld _physics;
        private MapConfig _config;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            var bus = new MessageBus();
            _runner = new Runner(bus);
            _physics = new PhysicsWorld(50);

            _config = ScriptableObject.CreateInstance<MapConfig>();
            _config.Set(MapSettings.Default, new List<MapPrefabSet>());

            _container = new Container();
            var catalog = new ConfigCatalog(new Config[] { _config });
            _container.Register(catalog);
            _container.Register(_world);
            _container.Register(bus);
            _container.Register(_runner);
            _container.Register(_physics);
            _container.AddResolver(new ConfigResolver(catalog));
            _container.AddResolver(new StashResolver(_world));
            _container.AddResolver(new MessagePipeResolver(bus));

            new BodiesFeature().Install(_container, _runner);
            new MapFeature().Install(_container, _runner);
            new EntitiesDestroyFeature().Install(_container, _runner);
            _runner.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            _physics.Dispose();
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void EveryCollisionBox_BecomesAStaticBody()
        {
            var layout = Generate(42);

            var expected = MapCollision.Build(layout, MapSettings.Default).Count;
            Assert.That(_container.Resolve<BodiesStorage>().Count, Is.EqualTo(expected));
            Assert.That(_world.Filter.With<MapBody>().With<PhysicsBody>().Build().GetLengthSlow(), Is.EqualTo(expected));
        }

        [Test]
        public void Regenerating_ReplacesTheBodies()
        {
            Generate(1);
            var layout = Generate(2);

            var expected = MapCollision.Build(layout, MapSettings.Default).Count;
            Assert.That(_container.Resolve<BodiesStorage>().Count, Is.EqualTo(expected), "the old arena's bodies are released");
        }

        [Test]
        public void EverySpawn_StandsOnSolidGround()
        {
            var layout = Generate(7);

            var characters = new List<Entity>();
            foreach (var spawn in layout.Spawns)
            {
                characters.Add(Character(spawn.Position));
            }

            Tick(30);

            foreach (var character in characters)
            {
                var state = character.GetComponent<PhysicsCharacterState>();
                Assert.That(state.Grounded, Is.True);
                Assert.That(state.Feet.y, Is.EqualTo(0f).Within(0.1f));
            }
        }

        [Test]
        public void ACharacter_WalksThroughADoor()
        {
            var layout = Generate(7);
            var door = First(layout, MapPieceKind.WallDoor);

            var travelled = WalkAcross(door, 2f, 90);

            Assert.That(travelled, Is.GreaterThan(1f), "it ends up on the far side of the wall");
        }

        [Test]
        public void ACharacter_IsStoppedByAWall()
        {
            var layout = Generate(7);
            var wall = First(layout, MapPieceKind.Wall);

            var travelled = WalkAcross(wall, 2f, 90);

            Assert.That(travelled, Is.LessThan(-0.3f), "it stays on its own side");
        }

        [Test]
        public void ACharacter_ClimbsTheStairsOntoTheRoof()
        {
            MapLayout layout = null;
            MapPiece stairs = default;
            for (var seed = 1; seed < 64; seed++)
            {
                layout = Generate(seed);
                if (TryFirst(layout, MapPieceKind.Stairs, out stairs))
                {
                    break;
                }
            }

            Assert.That(stairs.Kind, Is.EqualTo(MapPieceKind.Stairs), "some seed has roof access");

            var yaw = math.radians(stairs.Yaw);
            var forward = math.mul(quaternion.RotateY(yaw), new float3(0f, 0f, 1f));
            var character = Character(stairs.Position - forward * (layout.CellSize * 0.5f + 0.6f));
            Tick(10);

            for (var i = 0; i < 150; i++)
            {
                character.SetComponent(new PhysicsCharacterInput { Move = new float2(0f, 1f), Yaw = yaw });
                Tick();
            }

            var feet = character.GetComponent<PhysicsCharacterState>().Feet;
            Assert.That(feet.y, Is.EqualTo(MapSettings.Default.StoreyHeight).Within(0.1f), "it stands on the roof");
        }

        // --- helpers --------------------------------------------------------------------------

        private MapLayout Generate(int seed)
        {
            _container.Resolve<IPublisher<GenerateMap>>().Publish(new GenerateMap { Seed = seed });
            Tick();
            return _container.Resolve<MapStorage>().Layout;
        }

        /// <summary>
        /// Starts <paramref name="start"/> m in front of a wall piece, walks straight through its
        /// centre for <paramref name="ticks"/>, returns how far past the wall line the feet got.
        /// </summary>
        private float WalkAcross(MapPiece piece, float start, int ticks)
        {
            var yaw = math.radians(piece.Yaw);
            var normal = math.mul(quaternion.RotateY(yaw), new float3(0f, 0f, 1f));
            var character = Character(piece.Position - normal * start);
            Tick(10);

            for (var i = 0; i < ticks; i++)
            {
                character.SetComponent(new PhysicsCharacterInput { Move = new float2(0f, 1f), Yaw = yaw });
                Tick();
            }

            var feet = character.GetComponent<PhysicsCharacterState>().Feet;
            return math.dot(feet - piece.Position, normal);
        }

        private Entity Character(float3 feet)
        {
            var entity = _world.CreateEntity();
            entity.SetComponent(new CharacterCreateRequest { Feet = feet + new float3(0f, 0.05f, 0f) });
            return entity;
        }

        private static MapPiece First(MapLayout layout, MapPieceKind kind)
        {
            Assert.That(TryFirst(layout, kind, out var piece), Is.True, $"the layout has a {kind}");
            return piece;
        }

        private static bool TryFirst(MapLayout layout, MapPieceKind kind, out MapPiece piece)
        {
            foreach (var candidate in layout.Pieces)
            {
                if (candidate.Kind == kind)
                {
                    piece = candidate;
                    return true;
                }
            }

            piece = default;
            return false;
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

using System.Collections.Generic;
using Engine.Configs;
using Engine.DI;
using Engine.Ecs;
using Engine.Messages;
using Engine.Runtime;
using Features.DestroyFeature;
using Features.Map;
using Features.Map.Components;
using Features.Map.Configs;
using Features.Map.Messages;
using Features.Map.Systems;
using Features.Map.Utils;
using Features.Network.Systems;
using NUnit.Framework;
using Scellecs.Morpeh;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Map
{
    /// <summary>
    /// MapFeature against a headless world ticked by hand: messages and requests in, layout and
    /// spawn entities out. Only the FixedUpdate phase is ticked — the view needs real GameObjects.
    /// </summary>
    public sealed class MapFeatureTests
    {
        private World _world;
        private Runner _runner;
        private Container _container;
        private MapConfig _config;
        private Filter _spawnPoints;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            var bus = new MessageBus();
            _runner = new Runner(bus);

            _config = ScriptableObject.CreateInstance<MapConfig>();
            _config.Set(MapSettings.Default, new List<MapPrefabSet>());

            _container = new Container();
            var catalog = new ConfigCatalog(new Config[] { _config });
            _container.Register(catalog);
            _container.Register(_world);
            _container.Register(bus);
            _container.Register(_runner);
            _container.AddResolver(new ConfigResolver(catalog));
            _container.AddResolver(new StashResolver(_world));
            _container.AddResolver(new MessagePipeResolver(bus));

            _container.Register(new NetRole(true));
            new MapFeature().Install(_container, _runner);
            new EntitiesDestroyFeature().Install(_container, _runner);
            _runner.Initialize();

            _spawnPoints = _world.Filter
                .With<SpawnPoint>()
                .Build();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void NothingIsGenerated_UntilAsked()
        {
            Tick();

            Assert.That(_container.Resolve<MapStorage>().Layout, Is.Null);
            Assert.That(_spawnPoints.GetLengthSlow(), Is.Zero);
        }

        [Test]
        public void GenerateMapMessage_BuildsTheLayout_AndItsSpawnPoints()
        {
            Publish(77);
            Tick();

            var storage = _container.Resolve<MapStorage>();
            Assert.That(storage.Layout, Is.Not.Null);
            Assert.That(storage.Layout.Seed, Is.EqualTo(77));
            Assert.That(storage.Version, Is.EqualTo(1));
            Assert.That(_spawnPoints.GetLengthSlow(), Is.EqualTo(storage.Layout.Spawns.Count));
        }

        [Test]
        public void SpawnPointEntities_MatchTheLayout()
        {
            Publish(5);
            Tick();

            var layout = _container.Resolve<MapStorage>().Layout;
            var stash = _world.GetStash<SpawnPoint>();
            var positions = new List<Unity.Mathematics.float3>();
            foreach (var entity in _spawnPoints)
            {
                positions.Add(stash.Get(entity).Position);
            }

            foreach (var spawn in layout.Spawns)
            {
                Assert.That(positions, Does.Contain(spawn.Position));
            }
        }

        [Test]
        public void Regenerating_ReplacesTheSpawnPoints()
        {
            Publish(1);
            Tick();
            var first = new List<Entity>();
            foreach (var entity in _spawnPoints)
            {
                first.Add(entity);
            }

            Publish(2);
            Tick();

            var storage = _container.Resolve<MapStorage>();
            Assert.That(storage.Layout.Seed, Is.EqualTo(2));
            Assert.That(storage.Version, Is.EqualTo(2));
            Assert.That(_spawnPoints.GetLengthSlow(), Is.EqualTo(storage.Layout.Spawns.Count));
            foreach (var entity in first)
            {
                Assert.That(entity.IsDisposed(), Is.True);
            }
        }

        [Test]
        public void ARequestComponent_IsConsumed()
        {
            var request = _world.CreateEntity();
            _world.GetStash<MapGenerateRequest>().Set(request, new MapGenerateRequest { Seed = 9 });
            _world.Commit();

            Tick();

            Assert.That(_container.Resolve<MapStorage>().Layout.Seed, Is.EqualTo(9));
            Assert.That(request.IsDisposed(), Is.True);

            Tick();
            Assert.That(_container.Resolve<MapStorage>().Version, Is.EqualTo(1), "a consumed request does not fire twice");
        }

        [Test]
        public void SeveralRequestsInOneTick_TheLastWins_AndOnlyOneMapIsBuilt()
        {
            Publish(3);
            Publish(4);
            Tick();

            var storage = _container.Resolve<MapStorage>();
            Assert.That(storage.Layout.Seed, Is.EqualTo(4));
            Assert.That(storage.Version, Is.EqualTo(1));
        }

        private void Publish(int seed)
        {
            _container.Resolve<IPublisher<GenerateMap>>().Publish(new GenerateMap { Seed = seed });
        }

        private void Tick()
        {
            _runner.FixedTick(1f / 50f);
        }
    }
}

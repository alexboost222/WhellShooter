using Engine.Configs;
using Engine.DI;
using Engine.Ecs;
using Engine.Messages;
using Engine.Physics;
using Engine.Net;
using Engine.Runtime;
using NUnit.Framework;
using Scellecs.Morpeh;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Meta
{
    /// <summary>
    /// The real feature list against a headless world, wired the way Bootstrap wires it. Boot is the
    /// validation pass, so the first thing worth proving is that the graph builds at all.
    /// </summary>
    public sealed class FeaturesTests
    {
        private World _world;
        private Runner _runner;
        private PhysicsWorld _physics;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            var bus = new MessageBus();
            _runner = new Runner(bus);

            var container = new Container();
            // The real configs: features inject them, and boot is the validation pass.
            var catalog = ConfigCatalog.FromResources();
            container.Register(catalog);
            container.Register(_world);
            container.Register(bus);
            container.Register(_runner);
            _physics = container.Register(new PhysicsWorld(PhysicsWorld.TickRateOf(0.02f)));
            container.Register(new TimeService()).Install(_runner);
            container.AddResolver(new ConfigResolver(catalog));
            container.AddResolver(new StashResolver(_world));
            container.AddResolver(new MessagePipeResolver(bus));
            container.Register<INetTransport>(new LoopbackNetwork().CreateServer());

            global::Meta.Features.Register(container, _runner);
            _runner.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            _physics.Dispose();
        }

        [Test]
        public void EveryFeatureRegisters()
        {
            Assert.That(global::Meta.Features.Count, Is.GreaterThan(0));
            Assert.That(_runner.StepCount(RunnerPhase.FixedUpdate), Is.GreaterThan(0));
        }

        [Test]
        public void AnEntityTaggedDestroy_IsGoneAfterOneTick()
        {
            var doomed = _world.CreateEntity();
            _world.GetStash<Destroy>().Set(doomed);
            var survivor = _world.CreateEntity();
            _world.GetStash<Probe>().Set(survivor);
            _world.Commit();

            _runner.FixedTick(1f / 50f);

            Assert.That(doomed.IsDisposed(), Is.True);
            Assert.That(survivor.IsDisposed(), Is.False);
        }

        private struct Probe : IComponent
        {
        }
    }
}

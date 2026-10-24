using System;
using Engine.Configs;
using Engine.DI;
using Engine.Ecs;
using Engine.Messages;
using Engine.Net;
using Engine.Physics;
using Engine.Runtime;
using Features.Network.Providers;
using Features.Network.Systems;
using Scellecs.Morpeh;

namespace Game.Tests.EditMode.Features.Replication
{
    /// <summary>
    /// One process of the real game, headless: the full feature list over its own world, physics
    /// and transport, wired the way Bootstrap wires it. Ticked by hand; Update (views) never runs.
    /// </summary>
    internal sealed class GameNode : IDisposable
    {
        public GameNode(INetTransport transport, IPlayerInputSource input = null)
        {
            World = World.Create();
            Bus = new MessageBus();
            Runner = new Runner(Bus);
            Container = new Container();

            var catalog = ConfigCatalog.FromResources();
            Container.Register(catalog);
            Container.Register(World);
            Container.Register(Bus);
            Container.Register(Runner);
            Physics = Container.Register(new PhysicsWorld(PhysicsWorld.TickRateOf(0.02f)));
            Container.Register(new TimeService()).Install(Runner);
            Container.AddResolver(new ConfigResolver(catalog));
            Container.AddResolver(new StashResolver(World));
            Container.AddResolver(new MessagePipeResolver(Bus));
            Transport = transport;
            Container.Register<INetTransport>(transport);
            var buffer = Container.Register(new LocalInputBuffer());
            Container.Register<IPlayerInputSource>(input ?? buffer);

            global::Meta.Features.Register(Container, Runner);
            Runner.Initialize();
        }

        public World World { get; }
        public MessageBus Bus { get; }
        public Runner Runner { get; }
        public Container Container { get; }
        public PhysicsWorld Physics { get; }
        public INetTransport Transport { get; }

        public void Tick()
        {
            Runner.FixedTick(0.02f);
        }

        public void Dispose()
        {
            Transport.Dispose();
            World.Dispose();
            Physics.Dispose();
        }
    }
}

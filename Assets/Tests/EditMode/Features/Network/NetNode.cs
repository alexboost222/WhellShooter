using System;
using Engine.Configs;
using Engine.DI;
using Engine.Ecs;
using Engine.Messages;
using Engine.Net;
using Engine.Runtime;
using Features.DestroyFeature;
using Features.Network;
using Features.Network.Configs;
using Features.Network.Providers;
using Scellecs.Morpeh;

namespace Game.Tests.EditMode.Features.Network
{
    /// <summary>
    /// One headless process for the network tests: its own world, Runner and container, wired the
    /// way Bootstrap wires them, with the transport wrapped in a <see cref="LinkConditioner"/>.
    /// </summary>
    internal sealed class NetNode : IDisposable
    {
        public NetNode(INetTransport transport, NetConfig config, LinkConditions conditions, int seed,
            IPlayerInputSource input = null)
        {
            World = World.Create();
            var bus = new MessageBus();
            Runner = new Runner(bus);
            Link = new LinkConditioner(transport, conditions, seed);

            var container = new Container();
            var catalog = new ConfigCatalog(new Config[] { config });
            container.Register(catalog);
            container.Register(World);
            container.Register(bus);
            container.Register(Runner);
            container.Register<INetTransport>(Link);
            if (input != null)
            {
                container.Register(input);
            }

            container.AddResolver(new ConfigResolver(catalog));
            container.AddResolver(new StashResolver(World));
            container.AddResolver(new MessagePipeResolver(bus));

            new NetworkFeature().Install(container, Runner);
            new EntitiesDestroyFeature().Install(container, Runner);
            Runner.Initialize();
        }

        public World World { get; }

        public Runner Runner { get; }

        public LinkConditioner Link { get; }

        public void Tick(float deltaTime)
        {
            Runner.FixedTick(deltaTime);
        }

        public void Dispose()
        {
            Link.Dispose();
            World.Dispose();
        }
    }
}

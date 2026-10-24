using Engine.Ecs;
using Engine.Net;
using Engine.Runtime;
using Features.Network.Systems;

namespace Features.Network
{
    /// <summary>
    /// Tick sync and input delivery between the dedicated server and its clients (Docs/05). The
    /// registered <see cref="INetTransport"/> decides the role.
    /// </summary>
    public sealed class NetworkFeature : EcsFeature
    {
        public override void Register()
        {
            AddInitializer(Container.New<NetSessionStorage>());
            var role = Container.Register(new NetRole(Container.Resolve<INetTransport>().IsServer));
            Container.Register(new NetInbox());

            if (role.IsServer)
            {
                AddSystem(RunnerPhase.FixedUpdate, Container.New<ServerTickSystem>());
                AddSystem(RunnerPhase.FixedUpdate, Container.New<ServerReceiveSystem>());
                AddSystem(RunnerPhase.FixedUpdate, Container.New<ServerConsumeInputSystem>());
                AddSystem(RunnerPhase.FixedUpdate, Container.New<ServerSendSystem>());
                AddSystem(RunnerPhase.FixedUpdate, Container.New<ServerStatsSystem>());
            }
            else
            {
                AddSystem(RunnerPhase.FixedUpdate, Container.New<ClientReceiveSystem>());
                AddSystem(RunnerPhase.FixedUpdate, Container.New<ClientTickSystem>());
                AddSystem(RunnerPhase.FixedUpdate, Container.New<ClientSendSystem>());
                AddSystem(RunnerPhase.FixedUpdate, Container.New<ClientStatsSystem>());
            }
        }
    }
}

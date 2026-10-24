using Engine.DI;
using Engine.Net;
using Features.Network.Components;
using Features.Network.Constants;
using Features.Network.Enums;
using Scellecs.Morpeh;

namespace Features.Network.Systems
{
    /// <summary>
    /// Creates the session singleton (<see cref="NetClock"/>, plus <see cref="ClientSync"/> on a
    /// client) and starts the transport.
    /// </summary>
    public sealed class NetSessionStorage : IInitializer
    {
        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<ClientSync> _clientSync;

        [Inject] private INetTransport _transport;

        public World World { get; set; }

        public void OnAwake()
        {
            var session = World.CreateEntity();

            if (_transport.IsServer)
            {
                _netClock.Set(session, new NetClock { Tick = 0 });
            }
            else
            {
                _netClock.Set(session, new NetClock { Tick = NetConstants.NoTick });
                _clientSync.Set(session, new ClientSync
                {
                    State = ClientSyncState.Connecting,
                    ServerTick = NetConstants.NoTick,
                    AckTick = NetConstants.NoTick,
                    FirstTick = NetConstants.NoTick,
                    Barrier = NetConstants.NoTick,
                    LastMissedTick = NetConstants.NoTick,
                    Inputs = new PlayerInput[NetConstants.RingSize],
                });
            }

            _transport.Start();
        }

        // The transport belongs to whoever created it (Bootstrap disposes it on shutdown): the Runner
        // never disposes systems groups, so a Dispose here would not run.
        public void Dispose()
        {
        }
    }
}

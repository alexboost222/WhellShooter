using Engine.DI;
using Engine.Ecs;
using Engine.Net;
using Features.Bots.Components;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Features.Map.Systems;
using Features.Network.Components;
using Features.PlayerFeature.Components;
using Features.Replication.Components;
using Features.Replication.Enums;
using Features.Replication.Utils;
using Features.WeaponsFeature.Components;
using Scellecs.Morpeh;

namespace Features.Replication.Systems
{
    /// <summary>
    /// Server, right after the physics step: one <c>Snapshot</c> per client — this tick's every body,
    /// the arena seed and which body is the client's own (Docs/05 §Snapshot).
    /// </summary>
    public sealed class SnapshotSendSystem : ISystem
    {
        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<ServerClient> _serverClient;
        [Inject] private Stash<NetEntity> _netEntity;
        [Inject] private Stash<NetPlayer> _netPlayer;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<Health> _health;
        [Inject] private Stash<Dead> _dead;
        [Inject] private Stash<DamageTaken> _damageTaken;
        [Inject] private Stash<Team> _team;
        [Inject] private Stash<Weapon> _weapon;
        [Inject] private Stash<Bot> _bot;
        [Inject] private Stash<BotAim> _botAim;

        [Inject] private INetTransport _transport;
        [Inject] private MapStorage _map;

        public World World { get; set; }

        private readonly BodySnapshot[] _bodies = new BodySnapshot[SnapshotPacket.MaxBodies];
        private readonly PacketWriter _writer = new PacketWriter(SnapshotPacket.HeaderSize +
                                                                 SnapshotPacket.MaxBodies * SnapshotPacket.BodySize);
        private Filter _filter;
        private Filter _clients;
        private Filter _players;
        private Filter _clockFilter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<NetEntity>()
                .With<CharacterMotion>()
                .With<Look>()
                .Without<Destroy>()
                .Build();
            _clients = World.Filter
                .With<ServerClient>()
                .Without<Destroy>()
                .Build();
            _players = World.Filter
                .With<NetPlayer>()
                .With<NetEntity>()
                .Without<Destroy>()
                .Build();
            _clockFilter = World.Filter
                .With<NetClock>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var tick = _netClock.Get(_clockFilter.First()).Tick;
            var seed = _map.Layout != null ? _map.Layout.Seed : 0;
            var count = Collect();

            foreach (var entity in _clients)
            {
                var peer = _serverClient.Get(entity).Peer;
                SnapshotPacket.Write(_writer, tick, seed, BodyOf(peer), _bodies, count);
                _transport.Send(peer, _writer.Buffer, _writer.Length);
            }
        }

        private int Collect()
        {
            var count = 0;
            foreach (var entity in _filter)
            {
                if (count == _bodies.Length)
                {
                    break;
                }

                var motion = _characterMotion.Get(entity);
                var look = _look.Get(entity);
                var flags = SnapshotFlags.None;
                if (_bot.Has(entity))
                {
                    flags |= SnapshotFlags.Bot;
                }

                if (motion.Grounded)
                {
                    flags |= SnapshotFlags.Grounded;
                }

                if ((_botAim.Has(entity) && _botAim.Get(entity).Active) ||
                    (_weapon.Has(entity) && _weapon.Get(entity).Aiming))
                {
                    flags |= SnapshotFlags.Aiming;
                }

                var killer = NetEntity.None;
                if (_dead.Has(entity))
                {
                    flags |= SnapshotFlags.Dead;
                    if (World.TryGetEntity(_dead.Get(entity).Killer, out var source) && _netEntity.Has(source))
                    {
                        killer = _netEntity.Get(source).Id;
                    }
                }

                _bodies[count++] = new BodySnapshot
                {
                    Id = _netEntity.Get(entity).Id,
                    Flags = flags,
                    Team = (sbyte)(_team.Has(entity) ? _team.Get(entity).Index : Team.FreeForAll),
                    Feet = motion.Position,
                    Velocity = motion.Velocity,
                    Yaw = look.Yaw,
                    Pitch = look.Pitch,
                    Health = _health.Has(entity) ? _health.Get(entity).Current : 0f,
                    Hits = (ushort)(_damageTaken.Has(entity) ? _damageTaken.Get(entity).Hits : 0u),
                    Shots = (ushort)(_weapon.Has(entity) ? _weapon.Get(entity).Shots : 0u),
                    Killer = killer,
                };
            }

            return count;
        }

        private int BodyOf(int peer)
        {
            foreach (var entity in _players)
            {
                if (_netPlayer.Get(entity).Peer == peer)
                {
                    return _netEntity.Get(entity).Id;
                }
            }

            return NetEntity.None;
        }
    }
}

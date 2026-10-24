using System.Collections.Generic;
using Engine.DI;
using Engine.Diagnostics.Logs;
using Engine.Ecs;
using Features.CharacterFeature.Components;
using Features.Network.Components;
using Features.Network.Constants;
using Features.PlayerFeature.Components;
using Features.WeaponsFeature.Components;
using Scellecs.Morpeh;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// Server: a body (<see cref="NetPlayer"/>) for every client whose input has started, on its
    /// spawn slot; tagged <see cref="Destroy"/>, view and all, once the client is gone (Docs/05).
    /// </summary>
    public sealed class PlayerConnectionSystem : ISystem
    {
        [Inject] private Stash<ServerClient> _serverClient;
        [Inject] private Stash<NetPlayer> _netPlayer;
        [Inject] private Stash<Character> _character;
        [Inject] private Stash<Avatar> _avatar;
        [Inject] private Stash<CharacterIntent> _characterIntent;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<ShotRewind> _shotRewind;
        [Inject] private Stash<CharacterViewLink> _characterViewLink;
        [Inject] private Stash<Destroy> _destroy;

        [Inject] private PlayerSpawnPointStorage _spawnPoints;

        public World World { get; set; }

        private readonly HashSet<int> _peers = new HashSet<int>();
        private readonly HashSet<int> _joined = new HashSet<int>();
        private Filter _filter;
        private Filter _clients;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<NetPlayer>()
                .Without<Destroy>()
                .Build();
            _clients = World.Filter
                .With<ServerClient>()
                .Without<Destroy>()
                .Build();
        }

        public void Dispose()
        {
            _peers.Clear();
            _joined.Clear();
        }

        public void OnUpdate(float deltaTime)
        {
            _peers.Clear();
            foreach (var entity in _clients)
            {
                ref var client = ref _serverClient.Get(entity);
                if (client.FirstTick != NetConstants.NoTick)
                {
                    _peers.Add(client.Peer);
                }
            }

            _joined.Clear();
            foreach (var entity in _filter)
            {
                var peer = _netPlayer.Get(entity).Peer;
                if (_peers.Contains(peer))
                {
                    _joined.Add(peer);
                    continue;
                }

                Leave(entity, peer);
            }

            foreach (var peer in _peers)
            {
                if (!_joined.Contains(peer))
                {
                    Join(peer);
                }
            }
        }

        private void Join(int peer)
        {
            if (!_spawnPoints.TryGetSpawn(peer, out var position, out var yaw))
            {
                return;
            }

            var entity = World.CreateEntity();
            _netPlayer.Set(entity, new NetPlayer { Peer = peer });
            _character.Set(entity);
            _avatar.Set(entity);
            _characterIntent.Set(entity, new CharacterIntent());
            _look.Set(entity, new Look { Yaw = yaw });
            _characterMotion.Set(entity, new CharacterMotion
            {
                Position = position,
                PreviousPosition = position,
            });
            _shotRewind.Set(entity, new ShotRewind());
            Log.Info(LogChannels.Net, $"[Server] player body for client {peer} at {position}.");
        }

        private void Leave(Entity entity, int peer)
        {
            if (_characterViewLink.Has(entity))
            {
                var view = _characterViewLink.Get(entity).View;
                if (view != null)
                {
                    UnityEngine.Object.Destroy(view.gameObject);
                }
            }

            _destroy.Set(entity);
            Log.Info(LogChannels.Net, $"[Server] player body for client {peer} removed.");
        }
    }
}

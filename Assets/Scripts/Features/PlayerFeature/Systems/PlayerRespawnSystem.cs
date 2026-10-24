using Engine.DI;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Features.PlayerFeature.Components;
using Features.PlayerFeature.Configs;
using Scellecs.Morpeh;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// Server: brings a dead player back after <see cref="PlayerConfig.RespawnDelay"/>: a teleport to
    /// its spawn slot and a <see cref="ReviveRequest"/> (Combat refills the health) — the same way
    /// bots come back.
    /// </summary>
    public sealed class PlayerRespawnSystem : ISystem
    {
        [Inject] private Stash<NetPlayer> _netPlayer;
        [Inject] private Stash<PlayerRespawn> _playerRespawn;
        [Inject] private Stash<ReviveRequest> _reviveRequest;
        [Inject] private Stash<CharacterTeleportRequest> _characterTeleportRequest;

        [Inject] private PlayerSpawnPointStorage _spawnPoints;
        [Inject] private PlayerConfig _config;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<NetPlayer>()
                .With<Dead>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                if (!_playerRespawn.Has(entity))
                {
                    _playerRespawn.Set(entity, new PlayerRespawn { Remaining = _config.RespawnDelay });
                    continue;
                }

                ref var respawn = ref _playerRespawn.Get(entity);
                respawn.Remaining -= deltaTime;
                if (respawn.Remaining > 0f ||
                    !_spawnPoints.TryGetSpawn(_netPlayer.Get(entity).Peer, out var position, out var yaw))
                {
                    continue;
                }

                _playerRespawn.Remove(entity);
                _characterTeleportRequest.Set(entity, new CharacterTeleportRequest
                {
                    Position = position,
                    Yaw = yaw,
                });
                _reviveRequest.Set(entity);
            }
        }
    }
}

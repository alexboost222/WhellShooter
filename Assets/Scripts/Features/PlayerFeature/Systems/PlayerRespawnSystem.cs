using Engine.DI;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Features.PlayerFeature.Components;
using Features.PlayerFeature.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// Brings the dead player back after <see cref="PlayerConfig.RespawnDelay"/>: a teleport to the
    /// first spawn point and a <see cref="ReviveRequest"/> (Combat refills the health) — the same
    /// way bots come back.
    /// </summary>
    public sealed class PlayerRespawnSystem : ISystem
    {
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
                .With<Player>()
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
                if (respawn.Remaining > 0f || !_spawnPoints.TryGetFirst(out var point))
                {
                    continue;
                }

                float3 forward = point.forward;
                _playerRespawn.Remove(entity);
                _characterTeleportRequest.Set(entity, new CharacterTeleportRequest
                {
                    Position = point.position,
                    Yaw = math.atan2(forward.x, forward.z),
                });
                _reviveRequest.Set(entity);
            }
        }
    }
}

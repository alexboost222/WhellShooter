using Engine.DI;
using Features.CharacterFeature.Components;
using Features.PlayerFeature.Components;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// Creates the player's body on the first spawn point, when there is none. Simulation state
    /// only — the view arrives by hydration (<c>PlayerViewSpawnSystem</c>).
    /// </summary>
    public sealed class PlayerSpawnSystem : ISystem
    {
        [Inject] private Stash<Player> _player;
        [Inject] private Stash<PlayerLookInput> _playerLookInput;
        [Inject] private Stash<Character> _character;
        [Inject] private Stash<CharacterIntent> _characterIntent;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<CharacterMotion> _characterMotion;

        [Inject] private PlayerSpawnPointStorage _spawnPoints;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Player>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            if (_filter.FirstOrDefault() != null)
            {
                return;
            }

            if (!_spawnPoints.TryGetFirst(out var point))
            {
                return;
            }

            float3 position = point.position;
            float3 forward = point.forward;

            var entity = World.CreateEntity();
            _player.Set(entity);
            _playerLookInput.Set(entity, new PlayerLookInput());
            _character.Set(entity);
            _characterIntent.Set(entity, new CharacterIntent());
            _look.Set(entity, new Look { Yaw = math.atan2(forward.x, forward.z) });
            _characterMotion.Set(entity, new CharacterMotion
            {
                Position = position,
                PreviousPosition = position,
            });
        }
    }
}

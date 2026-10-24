using Engine.DI;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.PlayerFeature.Components;
using Features.PlayerFeature.Configs;
using Scellecs.Morpeh;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// Hydration: the player's body with no view gets one, built at its pose (Docs/03). Also how a
    /// body whose view died with a scene gets a fresh one.
    /// </summary>
    /// <remarks>
    /// The view carries no collider: the body is a phys-r capsule sized from <c>CharacterConfig</c>
    /// by <c>CharacterBodySpawnSystem</c> (Docs/11).
    /// </remarks>
    public sealed class PlayerViewSpawnSystem : ISystem
    {
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<CharacterViewLink> _characterViewLink;

        [Inject] private PlayerConfig _config;
        [Inject] private PlayerViewFactory _factory;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Player>()
                .With<CharacterMotion>()
                .With<Look>()
                .Without<CharacterViewLink>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref var motion = ref _characterMotion.Get(entity);
                var view = _factory.Build(_config.ViewPrefab, motion.Position, _look.Get(entity).Heading);

                // Nothing to blend from: the view starts where the body is.
                motion.PreviousPosition = motion.Position;

                _characterViewLink.Set(entity, new CharacterViewLink { View = view });
            }
        }
    }
}

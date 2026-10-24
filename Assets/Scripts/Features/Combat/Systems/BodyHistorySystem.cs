using Engine.DI;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Combat.Systems
{
    /// <summary>
    /// Server, right after the physics step: every body's feet into its <see cref="BodyHistory"/>,
    /// the history added on first sight.
    /// </summary>
    public sealed class BodyHistorySystem : ISystem
    {
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<BodyHistory> _bodyHistory;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Character>()
                .With<CharacterMotion>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                if (!_bodyHistory.Has(entity))
                {
                    _bodyHistory.Set(entity, new BodyHistory { Feet = new float3[BodyHistory.Capacity] });
                }

                ref var history = ref _bodyHistory.Get(entity);
                history.Feet[history.Cursor] = _characterMotion.Get(entity).Position;
                history.Cursor = (history.Cursor + 1) % BodyHistory.Capacity;
                history.Count = math.min(history.Count + 1, BodyHistory.Capacity);
            }
        }
    }
}

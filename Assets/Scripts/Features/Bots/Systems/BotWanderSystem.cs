using Engine.DI;
using Features.Bots.Components;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Features.Navigation.Systems;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Runs <see cref="BotWander"/>: walk to a random point of the arena, idle a moment, pick
    /// another. Looks where it walks.
    /// </summary>
    public sealed class BotWanderSystem : ISystem
    {
        /// <summary>A new point at least this far away, metres — otherwise wandering is shuffling.</summary>
        private const float MinLeg = 8f;

        [Inject] private Stash<BotWander> _botWander;
        [Inject] private Stash<BotMove> _botMove;
        [Inject] private Stash<BotAim> _botAim;
        [Inject] private Stash<BotRandom> _botRandom;
        [Inject] private Stash<CharacterMotion> _characterMotion;

        [Inject] private NavStorage _nav;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<BotWander>()
                .With<BotMove>()
                .With<BotAim>()
                .With<BotRandom>()
                .With<CharacterMotion>()
                .Without<Dead>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var grid = _nav.Grid;
            if (grid == null || grid.WalkableCells.Count == 0)
            {
                return;
            }

            foreach (var entity in _filter)
            {
                ref var wander = ref _botWander.Get(entity);
                ref var move = ref _botMove.Get(entity);
                _botAim.Get(entity).Active = false;

                if (wander.HasDestination && move.Active && !move.Arrived && !move.Failed)
                {
                    continue;
                }

                // Arrived (or never left): stand a moment, then go on.
                move.Active = false;
                wander.HasDestination = false;
                wander.Wait -= deltaTime;
                if (wander.Wait > 0f)
                {
                    continue;
                }

                ref var random = ref _botRandom.Get(entity).Value;
                var position = _characterMotion.Get(entity).Position;
                var cells = grid.WalkableCells;
                var destination = grid.CellCenter(cells[random.NextInt(0, cells.Count)]);
                for (var attempt = 0; attempt < 8 && math.distance(destination, position) < MinLeg; attempt++)
                {
                    destination = grid.CellCenter(cells[random.NextInt(0, cells.Count)]);
                }

                move = new BotMove { Destination = destination, Active = true };
                wander.HasDestination = true;
                wander.Wait = random.NextFloat(0.4f, 2f);
            }
        }
    }
}

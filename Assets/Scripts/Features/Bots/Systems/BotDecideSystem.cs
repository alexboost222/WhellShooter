using Engine.DI;
using Features.Bots.Components;
using Features.Bots.Enums;
using Features.Combat.Components;
using Scellecs.Morpeh;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Closes a decision round: the best bid becomes the running action, swapping the action state
    /// components through <see cref="BotActionSet"/>.
    /// </summary>
    public sealed class BotDecideSystem : ISystem
    {
        [Inject] private Stash<BotBrain> _botBrain;

        [Inject] private BotActionSet _actions;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Bot>()
                .With<BotBrain>()
                .Without<Dead>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref var brain = ref _botBrain.Get(entity);
                if (!brain.Thinking)
                {
                    continue;
                }

                brain.Thinking = false;
                if (brain.BestAction == BotAction.None || brain.BestAction == brain.Action)
                {
                    continue;
                }

                _actions.Exit(entity, brain.Action);
                _actions.Enter(entity, brain.BestAction);
                brain.Action = brain.BestAction;
                brain.ActionTime = 0f;
            }
        }
    }
}

using Engine.DI;
using Features.Bots.Components;
using Features.Bots.Configs;
using Features.Bots.Enums;
using Features.Combat.Components;
using Scellecs.Morpeh;

namespace Features.Bots.Systems
{
    /// <summary>Bids for <see cref="BotAction.Wander"/>: a small constant — what a bot does when nothing else is worth doing.</summary>
    public sealed class BotWanderScoreSystem : ISystem
    {
        [Inject] private Stash<BotBrain> _botBrain;

        [Inject] private BotsConfig _config;

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
                if (brain.Thinking)
                {
                    brain.Offer(BotAction.Wander, _config.WanderScore);
                }
            }
        }
    }
}

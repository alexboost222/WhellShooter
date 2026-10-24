using Engine.DI;
using Features.Bots.Components;
using Features.Bots.Configs;
using Features.Bots.Enums;
using Features.Combat.Components;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Opens a decision round every <see cref="BotsConfig.ThinkInterval"/>: clears the bids so the
    /// score systems after it can make theirs. Also keeps the action timer.
    /// </summary>
    public sealed class BotThinkSystem : ISystem
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
                brain.ActionTime += deltaTime;
                brain.NextThink -= deltaTime;
                brain.Thinking = brain.NextThink <= 0f;
                if (!brain.Thinking)
                {
                    continue;
                }

                brain.NextThink = math.max(brain.NextThink + _config.ThinkInterval, 0f);
                brain.BestAction = BotAction.None;
                brain.BestScore = 0f;
                brain.Commitment = _config.Commitment;
            }
        }
    }
}

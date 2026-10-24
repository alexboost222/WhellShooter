using Engine.DI;
using Features.Bots.Components;
using Features.Bots.Configs;
using Features.Bots.Enums;
using Features.Bots.Utils;
using Features.Combat.Components;
using Scellecs.Morpeh;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Bids for <see cref="BotAction.Chase"/> when the enemy has just dropped out of sight: fading
    /// over the profile's memory, and zero once a chase reached the spot and found nobody.
    /// </summary>
    public sealed class BotChaseScoreSystem : ISystem
    {
        [Inject] private Stash<Bot> _bot;
        [Inject] private Stash<BotBrain> _botBrain;
        [Inject] private Stash<BotSenses> _botSenses;
        [Inject] private Stash<BotChase> _botChase;

        [Inject] private BotsConfig _config;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Bot>()
                .With<BotBrain>()
                .With<BotSenses>()
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
                var senses = _botSenses.Get(entity);
                if (!brain.Thinking || !senses.HasTarget || senses.Visible)
                {
                    continue;
                }

                if (_botChase.Has(entity) && _botChase.Get(entity).Exhausted)
                {
                    continue;
                }

                var profile = _config.GetProfile(_bot.Get(entity).ProfileId);
                brain.Offer(BotAction.Chase, _config.ChaseScore * BotScores.Fade(senses.SinceSeen, profile.Memory));
            }
        }
    }
}

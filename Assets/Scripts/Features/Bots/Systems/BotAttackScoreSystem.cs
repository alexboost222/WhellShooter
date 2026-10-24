using Engine.DI;
using Features.Bots.Components;
using Features.Bots.Configs;
using Features.Bots.Enums;
using Features.Bots.Utils;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Bids for <see cref="BotAction.Attack"/> while an enemy is in sight: high, shaped by how close
    /// the fight is to the profile's preferred range.
    /// </summary>
    public sealed class BotAttackScoreSystem : ISystem
    {
        [Inject] private Stash<Bot> _bot;
        [Inject] private Stash<BotBrain> _botBrain;
        [Inject] private Stash<BotSenses> _botSenses;
        [Inject] private Stash<CharacterMotion> _characterMotion;

        [Inject] private BotsConfig _config;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Bot>()
                .With<BotBrain>()
                .With<BotSenses>()
                .With<CharacterMotion>()
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
                if (!brain.Thinking || !senses.HasTarget || !senses.Visible)
                {
                    continue;
                }

                var profile = _config.GetProfile(_bot.Get(entity).ProfileId);
                var distance = math.distance(_characterMotion.Get(entity).Position, senses.TargetPosition);
                brain.Offer(BotAction.Attack, _config.AttackScore * BotScores.Range(distance, profile.PreferredRange, 0.6f));
            }
        }
    }
}

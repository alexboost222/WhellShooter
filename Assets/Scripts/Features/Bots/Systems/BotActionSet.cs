using Engine.DI;
using Features.Bots.Components;
using Features.Bots.Enums;
using Features.CharacterFeature.Components;
using Scellecs.Morpeh;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Enters and leaves <see cref="BotAction"/>s: adds the action's state component with fresh
    /// values, removes it, and cleans up what the action owned. The one place that knows which
    /// component belongs to which action.
    /// </summary>
    public sealed class BotActionSet : IInitializer
    {
        [Inject] private Stash<BotWander> _botWander;
        [Inject] private Stash<BotAttack> _botAttack;
        [Inject] private Stash<BotChase> _botChase;
        [Inject] private Stash<BotMove> _botMove;
        [Inject] private Stash<BotAim> _botAim;
        [Inject] private Stash<CharacterIntent> _characterIntent;

        public World World { get; set; }

        public void OnAwake()
        {
        }

        public void Dispose()
        {
        }

        public void Enter(Entity entity, BotAction action)
        {
            // Every action starts from a standstill and its own idea of where to go and look.
            if (_botMove.Has(entity))
            {
                _botMove.Set(entity, default);
            }

            if (_botAim.Has(entity))
            {
                ref var aim = ref _botAim.Get(entity);
                aim.Active = false;
            }

            switch (action)
            {
                case BotAction.Wander:
                    _botWander.Set(entity, default);
                    break;
                case BotAction.Attack:
                    _botAttack.Set(entity, new BotAttack { NextStrafe = 0f, StrafeSide = 1f });
                    break;
                case BotAction.Chase:
                    _botChase.Set(entity, default);
                    break;
            }
        }

        public void Exit(Entity entity, BotAction action)
        {
            switch (action)
            {
                case BotAction.Wander:
                    _botWander.Remove(entity);
                    break;
                case BotAction.Attack:
                    _botAttack.Remove(entity);
                    // The trigger belongs to the attack; let go of it on the way out.
                    if (_characterIntent.Has(entity))
                    {
                        ref var intent = ref _characterIntent.Get(entity);
                        intent.Fire = false;
                        intent.FirePressed = false;
                    }

                    break;
                case BotAction.Chase:
                    _botChase.Remove(entity);
                    break;
            }
        }
    }
}

using Engine.Ecs;
using Engine.Runtime;
using Features.Bots.Systems;
using Features.Network.Systems;

namespace Features.Bots
{
    /// <summary>
    /// AI-driven bodies (Docs/08 §Bots). A bot is the player's body with a brain instead of a
    /// person: it writes the same <c>CharacterIntent</c> and <c>Look</c>, so it moves with the same
    /// motor and shoots with the same weapon.
    /// </summary>
    /// <remarks>
    /// Listed after Map, Navigation and the player, before Combat and the body: the brain decides on
    /// this tick's world, and the motor and weapon act on that decision in the same tick.
    /// <para>
    /// FixedUpdate, in order: spawn → respawn → senses → think → bids → decide → executors → path →
    /// steer → aim. Update: view hydration, animator. A client installs the views only.
    /// </para>
    /// </remarks>
    public sealed class BotsFeature : EcsFeature
    {
        public override void Register()
        {
            AddInitializer(Container.NewAndRegister<BotActionSet>());
            AddInitializer(Container.NewAndRegister<BotSpawnSelector>());
            Container.Register(new BotViewFactory(Container));

            // Views on both sides: on a client they draw the replicated bots and players (Docs/05).
            AddSystem(RunnerPhase.Update, Container.New<BotViewSpawnSystem>());
            AddSystem(RunnerPhase.Update, Container.New<BotAnimatorSystem>());

            // The brains run on the server only.
            if (Container.Resolve<NetRole>().IsClient)
            {
                return;
            }

            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotSpawnSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotRespawnSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotSensesSystem>());

            // Utility AI: open the round, every behaviour bids, the best bid wins.
            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotThinkSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotWanderScoreSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotAttackScoreSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotChaseScoreSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotDecideSystem>());

            // The running behaviour acts.
            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotWanderSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotAttackSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotChaseSystem>());

            // Locomotion and looking, shared by every behaviour.
            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotPathSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotSteerSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<BotAimSystem>());
        }
    }
}

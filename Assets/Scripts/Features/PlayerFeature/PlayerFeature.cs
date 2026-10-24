using Engine.Ecs;
using Engine.Messages;
using Engine.Runtime;
using Features.PlayerFeature.Messages;
using Features.PlayerFeature.Systems;

namespace Features.PlayerFeature
{
    /// <summary>
    /// The local player as a driver of a body (Docs/08 §Player): spawns it on a
    /// <c>PlayerSpawnPoint</c>, builds its view, and turns devices into its intent and look.
    /// Moving the body is <c>CharacterFeature</c>'s, which is listed after this one.
    /// </summary>
    public sealed class PlayerFeature : EcsFeature
    {
        public override void Register()
        {
            AddInitializer(Container.NewAndRegister<PlayerSpawnPointStorage>());
            AddInitializer(Container.NewAndRegister<PlayerInputService>());
            AddInitializer(Container.NewAndRegister<PlayerSettings>());
            // The pause screen's Resume is drained by the input system, in Update.
            Container.Register(Container.Resolve<MessageBus>().Queue<ResumePlay>(MessagePhase.Update));
            Container.Register(new PlayerViewFactory(Container));

            AddSystem(RunnerPhase.FixedUpdate, Container.New<PlayerSpawnSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<PlayerRespawnSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<PlayerSettingsSystem>());

            AddSystem(RunnerPhase.Update, Container.New<PlayerViewSpawnSystem>());
            AddSystem(RunnerPhase.Update, Container.New<PlayerInputSystem>());
            AddSystem(RunnerPhase.Update, Container.New<PlayerLookSystem>());
        }
    }
}

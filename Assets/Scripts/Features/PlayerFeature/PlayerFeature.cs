using Engine.Ecs;
using Engine.Runtime;
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
            Container.Register(new PlayerViewFactory(Container));

            AddSystem(RunnerPhase.FixedUpdate, Container.New<PlayerSpawnSystem>());

            AddSystem(RunnerPhase.Update, Container.New<PlayerViewSpawnSystem>());
            AddSystem(RunnerPhase.Update, Container.New<PlayerInputSystem>());
            AddSystem(RunnerPhase.Update, Container.New<PlayerLookSystem>());
        }
    }
}

using Engine.Ecs;
using Engine.Messages;
using Engine.Runtime;
using Features.Network.Systems;
using Features.PlayerFeature.Messages;
using Features.PlayerFeature.Systems;

namespace Features.PlayerFeature
{
    /// <summary>
    /// Players as drivers of bodies (Docs/08 §Player, Docs/05 §Roles). Server: a body per connected
    /// client on its <c>PlayerSpawnPoint</c> slot, driven by its consumed input, respawned. Client:
    /// devices into the input it sends and into its local body's intent and look, and its view — the
    /// body itself is created by replication. Moving the body is <c>CharacterFeature</c>'s, which is
    /// listed after this one.
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

            AddSystem(RunnerPhase.FixedUpdate, Container.New<PlayerSettingsSystem>());

            if (Container.Resolve<NetRole>().IsServer)
            {
                AddSystem(RunnerPhase.FixedUpdate, Container.New<PlayerConnectionSystem>());
                AddSystem(RunnerPhase.FixedUpdate, Container.New<PlayerNetInputSystem>());
                AddSystem(RunnerPhase.FixedUpdate, Container.New<PlayerRespawnSystem>());
                return;
            }

            AddSystem(RunnerPhase.FixedUpdate, Container.New<PlayerTickInputSystem>());

            AddSystem(RunnerPhase.Update, Container.New<PlayerViewSpawnSystem>());
            AddSystem(RunnerPhase.Update, Container.New<PlayerInputSystem>());
            AddSystem(RunnerPhase.Update, Container.New<PlayerLookSystem>());
        }
    }
}

using Engine.Ecs;
using Engine.Runtime;
using Features.CharacterFeature.Systems;

namespace Features.CharacterFeature
{
    /// <summary>
    /// Bodies: the shared contract the player and bots both drive (Docs/08 §Character). Whoever
    /// drives a body writes <c>CharacterIntent</c> and <c>Look</c> and builds its view; this feature
    /// moves it and places its head.
    /// </summary>
    /// <remarks>
    /// <b>Listed after the drivers</b> (player, bots), so intent and look written this tick or frame
    /// are what the motor and the head act on in the same tick or frame.
    /// </remarks>
    public sealed class CharacterFeature : EcsFeature
    {
        public override void Register()
        {
            AddSystem(RunnerPhase.FixedUpdate, Container.New<CharacterBodySpawnSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<CharacterTeleportSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<CharacterMotorSystem>());

            // phys-r moves the capsule in the physics step (Docs/11); read it back the same tick.
            AddSystem(RunnerPhase.AfterPhysics, Container.New<CharacterMotionSystem>());

            AddSystem(RunnerPhase.Update, Container.New<CharacterViewSweepSystem>());
            AddSystem(RunnerPhase.Update, Container.New<CharacterHeadSystem>());
        }
    }
}

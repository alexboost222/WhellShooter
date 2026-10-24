using Engine.Ecs;
using Engine.Runtime;
using Features.KillCam.Systems;

namespace Features.KillCam
{
    /// <summary>
    /// The kill cam (Docs/08 §Kill cam): records the last seconds of every body after each physics
    /// step and, when the local player dies, replays them through the killer's eyes. View only;
    /// <b>listed after the viewmodel and the tracers</b> — it writes the camera last and reads the
    /// Update-phase <c>ShotTraced</c> queue the tracers register. Registers <see cref="KillCamStorage"/>.
    /// </summary>
    public sealed class KillCamFeature : EcsFeature
    {
        public override void Register()
        {
            AddInitializer(Container.NewAndRegister<KillCamStorage>());

            AddSystem(RunnerPhase.AfterPhysics, Container.New<KillCamRecordSystem>());
            AddSystem(RunnerPhase.Update, Container.New<KillCamSystem>());
        }
    }
}

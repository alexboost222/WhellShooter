using Engine.Ecs;
using Engine.Runtime;
using Features.ViewmodelFeature.Systems;

namespace Features.ViewmodelFeature
{
    /// <summary>
    /// Procedural first-person animation (Docs/08 §Viewmodel): sway, bob, breathing, recoil, aiming,
    /// sprint and landing, all from springs and oscillators driven by the simulation's state. No
    /// animation clips, no Animator. View only — it reads the world and writes Transforms.
    /// </summary>
    /// <remarks>
    /// Listed after <c>CharacterFeature</c>: the head has to be placed this frame before the rig
    /// under it is posed. The order of the systems below is the layering order and is load-bearing.
    /// </remarks>
    public sealed class ViewmodelFeature : EcsFeature
    {
        public override void Register()
        {
            AddSystem(RunnerPhase.Update, Container.New<ViewmodelSweepSystem>());
            AddSystem(RunnerPhase.Update, Container.New<ViewmodelSpawnSystem>());
            AddSystem(RunnerPhase.Update, Container.New<ViewmodelBlendSystem>());
            AddSystem(RunnerPhase.Update, Container.New<ViewmodelMotionSystem>());
            AddSystem(RunnerPhase.Update, Container.New<ViewmodelRecoilSystem>());
            AddSystem(RunnerPhase.Update, Container.New<ViewmodelSpringSystem>());
            AddSystem(RunnerPhase.Update, Container.New<ViewmodelPoseSystem>());
            AddSystem(RunnerPhase.Update, Container.New<ViewmodelApplySystem>());
        }
    }
}

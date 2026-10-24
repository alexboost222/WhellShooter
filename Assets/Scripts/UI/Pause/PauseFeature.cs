using Engine.Ecs;
using Engine.Runtime;
using UI.Pause.Systems;

namespace UI.Pause
{
    /// <summary>
    /// The pause / settings screen (Docs/08 §Pause): Escape frees the cursor and opens it; mouse
    /// sensitivity and Resume. Raw UI Toolkit; writes only by publishing.
    /// </summary>
    public sealed class PauseFeature : EcsFeature
    {
        public override void Register()
        {
            AddSystem(RunnerPhase.Update, Container.New<PauseSystem>());
        }
    }
}

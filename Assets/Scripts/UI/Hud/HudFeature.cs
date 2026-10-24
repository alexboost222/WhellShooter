using Engine.Ecs;
using Engine.Runtime;
using UI.Hud.Systems;

namespace UI.Hud
{
    /// <summary>
    /// The in-game HUD (Docs/08 §HUD): health, crosshair, hit marker, damage flash, death screen.
    /// Raw UI Toolkit, read-only towards the world. Listed after <c>TracersFeature</c>, whose
    /// Update-phase <c>ShotTraced</c> queue it reads.
    /// </summary>
    public sealed class HudFeature : EcsFeature
    {
        public override void Register()
        {
            AddSystem(RunnerPhase.Update, Container.New<HudSystem>());
        }
    }
}

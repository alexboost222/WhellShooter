using Engine.Ecs;
using Engine.Messages;
using Engine.Runtime;
using Features.Combat.Messages;
using Features.Tracers.Systems;

namespace Features.Tracers
{
    /// <summary>
    /// Bullet tracers (Docs/08 §Tracers): every resolved shot drawn as a streak. View only.
    /// Registers the Update-phase <c>ShotTraced</c> queue, which the HUD's hit marker reads too.
    /// </summary>
    public sealed class TracersFeature : EcsFeature
    {
        public override void Register()
        {
            Container.Register(Container.Resolve<MessageBus>().Queue<ShotTraced>(MessagePhase.Update));

            AddSystem(RunnerPhase.Update, Container.New<TracerSystem>());
        }
    }
}

using Engine.Ecs;
using Engine.Runtime;
using Features.DestroyFeature.Systems;

namespace Features.DestroyFeature
{
    /// <summary>
    /// The end of the tick: everything tagged <see cref="Engine.Ecs.Destroy"/> is disposed here.
    /// <b>Must stay last in Meta's feature list</b> — that ordering is the entire contract, and the
    /// Runner special-cases nothing to enforce it.
    /// </summary>
    public sealed class EntitiesDestroyFeature : EcsFeature
    {
        public override void Register()
        {
            AddSystem(RunnerPhase.FixedUpdate, Container.New<EntitiesDestroySystem>());
        }
    }
}

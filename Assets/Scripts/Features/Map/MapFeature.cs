using Engine.Ecs;
using Engine.Runtime;
using Features.Map.Systems;

namespace Features.Map
{
    /// <summary>
    /// The procedural arena (Docs/08): generation in the simulation, the GameObjects in the view.
    /// Registers <see cref="MapStorage"/> for features after it.
    /// </summary>
    public sealed class MapFeature : EcsFeature
    {
        public override void Register()
        {
            AddInitializer(Container.NewAndRegister<MapStorage>());

            AddSystem(RunnerPhase.FixedUpdate, Container.New<MapGenerateSystem>());
            AddSystem(RunnerPhase.Update, Container.New<MapViewSystem>());
        }
    }
}

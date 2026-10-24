using Engine.Ecs;
using Engine.Runtime;
using Features.Navigation.Systems;

namespace Features.Navigation
{
    /// <summary>
    /// Walkability, paths and line of sight over the arena grid (Docs/08 §Navigation). Must come
    /// after <c>MapFeature</c>; registers <see cref="NavStorage"/> for features after it.
    /// </summary>
    public sealed class NavigationFeature : EcsFeature
    {
        public override void Register()
        {
            AddInitializer(Container.NewAndRegister<NavStorage>());

            AddSystem(RunnerPhase.FixedUpdate, Container.New<NavGridBuildSystem>());
        }
    }
}

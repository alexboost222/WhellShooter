using Engine.Ecs;
using Engine.Physics;
using Features.Bodies.Systems;

namespace Features.Bodies
{
    /// <summary>
    /// The ECS bridge to phys-r (Docs/11). Installs the Runner's physics hook: release → create →
    /// teleport → character input, the world step, then read-back. Registers <see cref="BodiesStorage"/>.
    /// </summary>
    /// <remarks>
    /// All of its work runs inside the hook — after every FixedUpdate step, including
    /// <c>EntitiesDestroyFeature</c> — so its place in <c>Features.cs</c> doesn't change timing.
    /// </remarks>
    public sealed class BodiesFeature : EcsFeature
    {
        public override void Register()
        {
            AddInitializer(Container.NewAndRegister<BodiesStorage>());

            var before = CreateStep("PhysicsBefore",
                Container.New<PhysicsReleaseSystem>(),
                Container.New<PhysicsCreateSystem>(),
                Container.New<CharacterTeleportSystem>(),
                Container.New<CharacterCorrectionSystem>(),
                Container.New<CharacterInputSystem>());

            var after = CreateStep("PhysicsAfter",
                Container.New<CharacterReadbackSystem>(),
                Container.New<BodyReadbackSystem>());

            Runner.PhysicsHook = new PhysicsStep(Container.Resolve<PhysicsWorld>(), before, after);
        }
    }
}

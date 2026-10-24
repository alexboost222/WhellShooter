using Engine.Ecs;
using Engine.Physics;
using Engine.Runtime;
using Features.Combat.Systems;

namespace Features.Combat
{
    /// <summary>
    /// Health, teams, hits and death (Docs/08 §Combat). Listed after the drivers (player, bots) and
    /// before the body and the weapon: the shots fired last tick land first, then the dead are
    /// silenced before the motor reads their intent.
    /// </summary>
    public sealed class CombatFeature : EcsFeature
    {
        public override void Register()
        {
            Container.Register<IShotOccluder>(new WorldShotOccluder(Container.Resolve<PhysicsWorld>()));

            AddSystem(RunnerPhase.FixedUpdate, Container.New<HealthEquipSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<ReviveSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<HitscanSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<DamageApplySystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<DeadIntentSystem>());
        }
    }
}

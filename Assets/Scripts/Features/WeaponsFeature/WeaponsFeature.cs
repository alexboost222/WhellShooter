using Engine.Ecs;
using Engine.Runtime;
using Features.WeaponsFeature.Systems;

namespace Features.WeaponsFeature
{
    /// <summary>
    /// What a body holds and when it fires (Docs/08 §Weapons). Shots leave as
    /// <c>ShotRequest</c>s for Combat; how a shot looks is the viewmodel's.
    /// </summary>
    /// <remarks>Listed after <c>CharacterFeature</c>: firing reads this tick's sprint state.</remarks>
    public sealed class WeaponsFeature : EcsFeature
    {
        public override void Register()
        {
            AddSystem(RunnerPhase.FixedUpdate, Container.New<WeaponEquipSystem>());
            AddSystem(RunnerPhase.FixedUpdate, Container.New<WeaponFireSystem>());
        }
    }
}

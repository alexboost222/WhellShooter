using Scellecs.Morpeh;

namespace Features.Combat.Components
{
    /// <summary>
    /// Hit points. Every body gets one (<c>HealthEquipSystem</c>); only <c>DamageApplySystem</c>
    /// lowers it and only <c>ReviveSystem</c> refills it.
    /// </summary>
    public struct Health : IComponent
    {
        public float Current;
        public float Max;

        public float Fraction => Max > 0f ? Current / Max : 0f;
    }
}

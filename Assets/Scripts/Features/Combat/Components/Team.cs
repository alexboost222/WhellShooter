using Scellecs.Morpeh;

namespace Features.Combat.Components
{
    /// <summary>
    /// Which side a body fights for. Set by whoever spawns the body; a body without one fights for
    /// itself (see <see cref="Utils.CombatRules.AreHostile"/>).
    /// </summary>
    public struct Team : IComponent
    {
        /// <summary>0, 1, … or <see cref="FreeForAll"/> — the same numbering as the map's spawn points.</summary>
        public int Index;

        public const int FreeForAll = -1;
    }
}

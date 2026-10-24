using Features.Combat.Components;

namespace Features.Combat.Utils
{
    /// <summary>Who may hurt whom. One rule, used by the hitscan, the damage step and the bots' senses.</summary>
    public static class CombatRules
    {
        /// <summary>
        /// Free-for-all on either side means hostile; otherwise different teams are. A body never
        /// counts as hostile to itself — callers check identity first.
        /// </summary>
        public static bool AreHostile(int teamA, int teamB)
        {
            if (teamA == Team.FreeForAll || teamB == Team.FreeForAll)
            {
                return true;
            }

            return teamA != teamB;
        }
    }
}

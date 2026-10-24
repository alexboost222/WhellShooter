using Unity.Mathematics;

namespace Features.Bots.Utils
{
    /// <summary>
    /// The response curves the score systems shape their bids with. Pure functions, 0..1 in and out,
    /// so a behaviour's whole decision logic can be read — and tested — in one place.
    /// </summary>
    public static class BotScores
    {
        /// <summary>1 at zero, falling linearly to 0 at <paramref name="limit"/>.</summary>
        public static float Fade(float value, float limit)
        {
            return limit > 0f ? math.saturate(1f - value / limit) : 0f;
        }

        /// <summary>
        /// How good a distance is for fighting: 1 around the preferred range, easing off when much
        /// closer or farther, never below <paramref name="floor"/> — a visible enemy is always worth
        /// fighting.
        /// </summary>
        public static float Range(float distance, float preferred, float floor)
        {
            if (preferred <= 0f)
            {
                return 1f;
            }

            var ratio = distance / preferred;
            var off = ratio < 1f ? 1f - ratio : (ratio - 1f) * 0.5f;
            return math.max(floor, 1f - off * off);
        }
    }
}

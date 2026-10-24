using System;

namespace Features.Network.Utils
{
    public static class SlackMath
    {
        /// <summary><paramref name="leadMs"/> in whole ticks of <paramref name="tickSeconds"/>, rounded down.</summary>
        public static int MaxLeadTicks(float leadMs, float tickSeconds)
        {
            return Math.Max(1, (int)Math.Floor(leadMs / 1000.0 / tickSeconds + 1e-6));
        }

        /// <summary>
        /// Ticks of slack that make a miss rarer than <paramref name="missChance"/> per tick on a link
        /// losing <paramref name="lossRate"/> of packets (Docs/05).
        /// </summary>
        /// <remarks>
        /// With slack <c>s</c> an input travels in <c>s + 1</c> packets before its deadline, so it
        /// misses only if all of them are lost: <c>loss^(s+1) ≤ missChance</c>.
        /// </remarks>
        public static int NeedSlack(float lossRate, float missChance, int min, int max)
        {
            if (lossRate <= 0f)
            {
                return min;
            }

            if (lossRate >= 1f)
            {
                return max;
            }

            // The epsilon keeps float inputs like 0.01f from rounding an exact 3 up to 4.
            var copies = Math.Ceiling(Math.Log(missChance) / Math.Log(lossRate) - 1e-4);
            var slack = (int)copies - 1;
            return Math.Max(min, Math.Min(max, slack));
        }
    }
}

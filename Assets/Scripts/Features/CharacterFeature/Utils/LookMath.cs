using Unity.Mathematics;

namespace Features.CharacterFeature.Utils
{
    /// <summary>Angle arithmetic for <c>Look</c>, in radians.</summary>
    public static class LookMath
    {
        /// <summary>Wraps into [−π, π).</summary>
        public static float WrapAngle(float radians)
        {
            return radians - 2f * math.PI * math.floor((radians + math.PI) / (2f * math.PI));
        }

        /// <summary>The shortest signed turn from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static float DeltaAngle(float from, float to)
        {
            return WrapAngle(to - from);
        }
    }
}

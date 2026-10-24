using Unity.Mathematics;

namespace Features.Network.Utils
{
    /// <summary>Absolute look on the wire (Docs/05 §Input): yaw as a ushort turn, pitch in 1e-4 rad.</summary>
    public static class LookCodec
    {
        private const float PitchScale = 10000f;

        public static ushort EncodeYaw(float yaw)
        {
            var turn = (yaw + math.PI) / (2f * math.PI);
            turn -= math.floor(turn);
            return (ushort)((int)math.round(turn * 65536f) & 0xFFFF);
        }

        /// <summary>Wrapped to [−π, π).</summary>
        public static float DecodeYaw(ushort yaw)
        {
            return yaw / 65536f * 2f * math.PI - math.PI;
        }

        public static short EncodePitch(float pitch)
        {
            return (short)math.clamp(math.round(pitch * PitchScale), short.MinValue, short.MaxValue);
        }

        public static float DecodePitch(short pitch)
        {
            return pitch / PitchScale;
        }
    }
}

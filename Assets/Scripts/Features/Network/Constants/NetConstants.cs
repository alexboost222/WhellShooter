namespace Features.Network.Constants
{
    public static class NetConstants
    {
        /// <summary>"No tick yet" in every tick field and on the wire.</summary>
        public const int NoTick = -1;

        /// <summary>Input ring size on both ends — 5.12 s at 50 Hz. Power of two.</summary>
        public const int RingSize = 256;

        public const int RingMask = RingSize - 1;
    }
}

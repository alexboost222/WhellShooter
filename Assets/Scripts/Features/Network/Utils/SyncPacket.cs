namespace Features.Network.Utils
{
    /// <summary>Server → client, every server tick (Docs/05).</summary>
    public struct SyncPacket
    {
        /// <summary><see cref="LossHalfPercent"/> before the server has a full loss window.</summary>
        public const byte LossUnknown = 255;

        public int ServerTick;
        public uint EchoFrame;

        /// <summary>Server ticks <see cref="EchoFrame"/> waited on the server before this packet.</summary>
        public byte EchoAge;

        public int AckTick;

        /// <summary>Newest input tick the slack sample is about; NoTick for no sample.</summary>
        public int SampleTick;
        public sbyte Slack;

        /// <summary>Client → server loss, 0.5 % units; <see cref="LossUnknown"/> until measured.</summary>
        public byte LossHalfPercent;

        public int LastMissedTick;
    }
}

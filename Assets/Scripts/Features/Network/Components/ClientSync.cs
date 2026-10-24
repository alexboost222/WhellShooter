using Features.Network.Enums;
using Scellecs.Morpeh;

namespace Features.Network.Components
{
    /// <summary>Client side singleton, beside <see cref="NetClock"/>: tick sync and unacked input (Docs/05).</summary>
    public struct ClientSync : IComponent
    {
        public ClientSyncState State;

        /// <summary>Local fixed-step counter; grows on holds too. Echoed by the server for RTT.</summary>
        public uint Frame;

        // Newest server tick heard and the frame it was heard on.
        public int ServerTick;
        public uint ServerTickFrame;

        /// <summary>Inputs up to here are on the server; never above <see cref="NetClock.Tick"/>.</summary>
        public int AckTick;

        public int RttSamples;
        public int WorstRtt;
        public int Rtt;

        public int FirstTick;

        /// <summary>Ring keyed by tick & NetConstants.RingMask; (AckTick, Tick] is unacked.</summary>
        public PlayerInput[] Inputs;

        // Slack window. Only samples for ticks >= Barrier count: older inputs were produced
        // under the lead before the last adjustment.
        public int Barrier;
        public int WindowMin;
        public int WindowMax;
        public int WindowSum;
        public int WindowCount;

        /// <summary>Max − min slack of the last full window: the jitter a young window has not seen yet.</summary>
        public int JitterSpread;

        /// <summary>Slack the client aims to keep as the window minimum, from the server's loss rate.</summary>
        public int NeedSlack;
        public float LossRate;
        public int LastMissedTick;

        // Decided by the receive system, executed by the tick system in the same step.
        public int PendingJump;
        public int PendingHolds;

        public int Jumps;
        public int JumpedTicks;
        public int Holds;
        public int LastSlack;
    }
}

using Scellecs.Morpeh;

namespace Features.Network.Components
{
    /// <summary>Server side, one entity per connected client: its input buffer and sync state (Docs/05).</summary>
    public struct ServerClient : IComponent
    {
        /// <summary>Transport peer id.</summary>
        public int Peer;

        // Ring keyed by tick & NetConstants.RingMask. InputTicks says which tick a slot holds.
        public PlayerInput[] Inputs;
        public int[] InputTicks;

        /// <summary>Server tick the slot's input first arrived on.</summary>
        public int[] ArrivalTicks;

        /// <summary>The client's first input tick; NoTick until its first input packet.</summary>
        public int FirstTick;

        /// <summary>Next tick to consume.</summary>
        public int NextTick;

        /// <summary>Highest tick with every input up to it received — the ack.</summary>
        public int ReceivedThrough;

        /// <summary>Newest input tick seen at all; only packets that raise it are slack samples.</summary>
        public int NewestTick;

        // Echo for the client's RTT: newest frame and the server tick it arrived on.
        public uint LastFrame;
        public int LastFrameTick;
        public bool HasFrame;

        // This tick's slack report: lowest (newest tick − server tick) among packets that brought a
        // new newest tick. SampleTick is NoTick when no such packet arrived.
        public int SampleTick;
        public int SampleSlack;

        public int LastMissedTick;

        // Loss on the client → server link from gaps in frame numbers, per LossWindow packets.
        // LossRate is -1 until the first window is full.
        public int LossExpected;
        public int LossLost;
        public float LossRate;

        /// <summary>Repeated for the simulation when an input misses its tick.</summary>
        public PlayerInput LastInput;
    }
}

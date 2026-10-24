using Scellecs.Morpeh;

namespace Features.Network.Components
{
    /// <summary>Server side, beside <see cref="ServerClient"/>: input delivery counters.</summary>
    public struct ServerClientStats : IComponent
    {
        // Totals since connect.
        public int Consumed;
        public int Missed;
        public int Late;

        /// <summary>Inputs dropped for being more than MaxInputLeadMs ahead of the server tick.</summary>
        public int Rejected;

        // Since the last stats log.
        public int IntervalRejected;
        public int IntervalConsumed;
        public int IntervalMissed;
        public int IntervalLate;
        public int IntervalPackets;
        public int IntervalSlackMin;
        public int IntervalSlackMax;
    }
}

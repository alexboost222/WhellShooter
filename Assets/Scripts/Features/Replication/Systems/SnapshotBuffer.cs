using Features.Network.Constants;
using Features.Replication.Utils;
using Scellecs.Morpeh;

namespace Features.Replication.Systems
{
    /// <summary>
    /// Client: the last <see cref="Capacity"/> ticks of snapshots and the tick remote bodies are
    /// drawn at (Docs/05 §Remote bodies). Filled by <c>SnapshotReceiveSystem</c>, read by the body
    /// systems after it.
    /// </summary>
    public sealed class SnapshotBuffer : IInitializer
    {
        public const int Capacity = 64;
        private const int Mask = Capacity - 1;

        private readonly SnapshotFrame[] _frames = new SnapshotFrame[Capacity];
        private readonly SnapshotFrame _scratch = new SnapshotFrame();

        public World World { get; set; }

        /// <summary>The newest snapshot received, or null before the first.</summary>
        public SnapshotFrame Newest { get; private set; }

        /// <summary>Server tick remote bodies are drawn at; NoTick before the first snapshot.</summary>
        public int RenderTick { get; set; } = NetConstants.NoTick;

        /// <summary>A frame to decode into; hand it to <see cref="Commit"/> if it decoded.</summary>
        public SnapshotFrame Scratch => _scratch;

        public void OnAwake()
        {
            for (var i = 0; i < Capacity; i++)
            {
                _frames[i] = new SnapshotFrame();
            }
        }

        public void Dispose()
        {
            Newest = null;
            RenderTick = NetConstants.NoTick;
        }

        /// <summary>
        /// Keeps <see cref="Scratch"/> under its tick. A duplicate or one older than the ring is
        /// dropped. Returns whether it was kept.
        /// </summary>
        public bool Commit()
        {
            var tick = _scratch.Tick;
            if (tick < 0 || (Newest != null && tick <= Newest.Tick - Capacity))
            {
                return false;
            }

            var slot = _frames[tick & Mask];
            if (slot.Tick == tick)
            {
                return false;
            }

            Copy(_scratch, slot);
            if (Newest == null || tick > Newest.Tick)
            {
                Newest = slot;
            }

            return true;
        }

        public bool TryGet(int tick, out SnapshotFrame frame)
        {
            frame = tick >= 0 ? _frames[tick & Mask] : null;
            return frame != null && frame.Tick == tick;
        }

        private static void Copy(SnapshotFrame from, SnapshotFrame to)
        {
            to.Tick = from.Tick;
            to.Seed = from.Seed;
            to.You = from.You;
            to.Count = from.Count;
            System.Array.Copy(from.Bodies, to.Bodies, from.Count);
        }
    }
}

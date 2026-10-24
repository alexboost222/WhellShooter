using Engine.Diagnostics;

namespace Engine.Runtime
{
    /// <summary>
    /// Turns frame time into a whole number of fixed ticks: each frame adds its delta, and every
    /// full <see cref="TickSeconds"/> in the accumulator is one tick to run this frame — 0, 1 or more.
    /// </summary>
    /// <remarks>
    /// <b>Capped at <see cref="MaxTicksPerFrame"/>.</b> If a frame owes more (a hitch, a breakpoint,
    /// a machine slower than the tick), the excess is dropped rather than carried: carrying it makes
    /// the next frame longer still, which owes more ticks — the death spiral. Dropped time is
    /// counted in <see cref="DroppedTicks"/>; the net tick sync absorbs it (a client jumps, a server's
    /// clients hold).
    /// </remarks>
    public sealed class TickAccumulator
    {
        private double _accumulator;

        public TickAccumulator(float tickSeconds, int maxTicksPerFrame)
        {
            Guard.ThrowIf(tickSeconds <= 0f, "Tick length must be positive.");
            Guard.ThrowIf(maxTicksPerFrame < 1, "At least one tick per frame must be allowed.");

            TickSeconds = tickSeconds;
            MaxTicksPerFrame = maxTicksPerFrame;
        }

        public float TickSeconds { get; }

        public int MaxTicksPerFrame { get; }

        /// <summary>Ticks discarded by the cap since construction.</summary>
        public long DroppedTicks { get; private set; }

        /// <summary>Time owed to the next tick, 0 ≤ x &lt; <see cref="TickSeconds"/> after <see cref="Advance"/>.</summary>
        public double Accumulated => _accumulator;

        /// <summary>Adds a frame's delta and returns how many ticks to run this frame.</summary>
        public int Advance(float frameDelta)
        {
            if (frameDelta > 0f)
            {
                _accumulator += frameDelta;
            }

            var ticks = (int)(_accumulator / TickSeconds);
            _accumulator -= ticks * (double)TickSeconds;

            if (ticks > MaxTicksPerFrame)
            {
                DroppedTicks += ticks - MaxTicksPerFrame;
                ticks = MaxTicksPerFrame;
            }

            return ticks;
        }
    }
}

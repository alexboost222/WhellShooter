using Engine.Diagnostics;
using Unity.Mathematics;

namespace Engine.Runtime
{
    /// <summary>
    /// What a system may know about time. Injected; systems never read <c>UnityEngine.Time</c>
    /// (Docs/06 §Ticks &amp; time).
    /// </summary>
    /// <remarks>
    /// <b>Fed by two Runner steps, not by Unity.</b> <see cref="Install"/> puts one step first in
    /// FixedUpdate and one first in Update, so every system in a phase sees the same numbers and the
    /// whole thing runs in a headless test exactly as it does in play. The Runner itself still keeps
    /// no clock — this is the clock, and it is one service rather than a field on the Runner so that
    /// what a system depends on is visible in its <c>[Inject]</c> list.
    /// <para>
    /// <b>What it is for today is <see cref="InterpolationAlpha"/>:</b> how far the frame being drawn
    /// sits between the last two fixed ticks, which is what lets a view draw a body that moves on the
    /// fixed tick without stepping. Systems still take their delta from <c>OnUpdate</c>; that is the
    /// phase's delta and the only one they should integrate with.
    /// </para>
    /// </remarks>
    public sealed class TimeService
    {
        private float _sinceFixedTick;

        /// <summary>The fixed step the last tick ran with. Zero before the first one.</summary>
        public float FixedDeltaTime { get; private set; }

        /// <summary>Fixed ticks run this session.</summary>
        public ulong FixedTicks { get; private set; }

        /// <summary>The delta of the frame being drawn. Zero before the first one.</summary>
        public float FrameDeltaTime { get; private set; }

        /// <summary>
        /// 0 = the frame is exactly at the last fixed tick, 1 = a whole step after it. Draw a
        /// fixed-tick body at <c>lerp(previous, current, alpha)</c>. 1 before the first fixed tick,
        /// so a view with nothing to blend from shows the current pose.
        /// </summary>
        public float InterpolationAlpha
        {
            get
            {
                if (FixedDeltaTime <= 0f)
                {
                    return 1f;
                }

                return math.saturate(_sinceFixedTick / FixedDeltaTime);
            }
        }

        /// <summary>
        /// Adds the two feeding steps. Call once, from the composition root, <b>before any feature
        /// registers</b> — being first in each phase is what makes the numbers consistent.
        /// </summary>
        public void Install(Runner runner)
        {
            Guard.ThrowIfNull(runner, nameof(runner));

            runner.Add(RunnerPhase.FixedUpdate, RunnerStep.Create("Time.FixedTick", AdvanceFixed));
            runner.Add(RunnerPhase.Update, RunnerStep.Create("Time.Frame", AdvanceFrame));
        }

        /// <summary>One fixed tick of <paramref name="deltaTime"/> has started.</summary>
        public void AdvanceFixed(float deltaTime)
        {
            FixedDeltaTime = deltaTime;
            FixedTicks++;

            // Unity runs a frame's fixed ticks before its Update, so subtracting here and adding in
            // AdvanceFrame leaves exactly (frame time − last fixed tick time) when Update reads it.
            _sinceFixedTick -= deltaTime;
        }

        /// <summary>A frame of <paramref name="deltaTime"/> has started.</summary>
        public void AdvanceFrame(float deltaTime)
        {
            FrameDeltaTime = deltaTime;

            // Clamped so a hitch (Unity caps how many fixed ticks one frame may run) can never leave
            // a debt that would park alpha at 0 or 1 for seconds afterwards.
            _sinceFixedTick = math.clamp(_sinceFixedTick + deltaTime, 0f, math.max(FixedDeltaTime, 0f));
        }
    }
}

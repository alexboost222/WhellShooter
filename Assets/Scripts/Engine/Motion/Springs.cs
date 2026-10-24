using Unity.Mathematics;

namespace Engine.Motion
{
    /// <summary>
    /// Frame-rate-independent motion helpers: an exact damped spring and exponential smoothing.
    /// </summary>
    /// <remarks>
    /// <b>Exact, not integrated.</b> <see cref="Step"/> evaluates the closed-form solution of
    /// <c>x'' = −ω²(x − target) − 2ζω x'</c> over the whole delta, so one step of 0.1 s lands where
    /// ten steps of 0.01 s do, and a stiff spring cannot blow up on a hitch. An Euler step would make
    /// the feel depend on the frame rate, which on mobile is whatever the device felt like today.
    /// <para>
    /// Parameterised the way a designer thinks: <b>frequency</b> in Hz (how fast it swings) and
    /// <b>damping ratio</b> ζ (below 1 overshoots and rings, 1 settles fastest without overshoot,
    /// above 1 creeps).
    /// </para>
    /// </remarks>
    public static class Springs
    {
        private const float CriticalBand = 1e-3f;

        /// <summary>Advances <paramref name="spring"/> towards <paramref name="target"/> by <paramref name="deltaTime"/>.</summary>
        public static void Step(ref Spring3 spring, float3 target, float frequency, float dampingRatio, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (frequency <= 0f)
            {
                // No stiffness: nothing pulls it anywhere, so it is already where it should be.
                spring.Value = target;
                spring.Velocity = float3.zero;
                return;
            }

            var omega = 2f * math.PI * frequency;
            var zeta = math.max(dampingRatio, 0f);
            var y0 = spring.Value - target;
            var v0 = spring.Velocity;
            float3 y;
            float3 v;

            if (math.abs(zeta - 1f) < CriticalBand)
            {
                var decay = math.exp(-omega * deltaTime);
                var c2 = v0 + omega * y0;
                y = (y0 + c2 * deltaTime) * decay;
                v = (c2 - omega * (y0 + c2 * deltaTime)) * decay;
            }
            else if (zeta < 1f)
            {
                var omegaD = omega * math.sqrt(1f - zeta * zeta);
                var decay = math.exp(-zeta * omega * deltaTime);
                var cos = math.cos(omegaD * deltaTime);
                var sin = math.sin(omegaD * deltaTime);
                var c2 = (v0 + zeta * omega * y0) / omegaD;
                y = decay * (y0 * cos + c2 * sin);
                v = decay * ((c2 * omegaD - zeta * omega * y0) * cos - (y0 * omegaD + zeta * omega * c2) * sin);
            }
            else
            {
                var root = math.sqrt(zeta * zeta - 1f);
                var r1 = -omega * (zeta - root);
                var r2 = -omega * (zeta + root);
                var c2 = (v0 - r1 * y0) / (r2 - r1);
                var c1 = y0 - c2;
                var e1 = math.exp(r1 * deltaTime);
                var e2 = math.exp(r2 * deltaTime);
                y = c1 * e1 + c2 * e2;
                v = c1 * r1 * e1 + c2 * r2 * e2;
            }

            spring.Value = target + y;
            spring.Velocity = v;
        }

        /// <summary>
        /// Moves <paramref name="current"/> towards <paramref name="target"/>, covering the fraction
        /// <c>1 − e^(−sharpness·dt)</c> of the gap. Frame-rate independent; higher is snappier.
        /// </summary>
        public static float Damp(float current, float target, float sharpness, float deltaTime)
        {
            if (sharpness <= 0f)
            {
                return target;
            }

            return math.lerp(target, current, math.exp(-sharpness * math.max(deltaTime, 0f)));
        }

        /// <inheritdoc cref="Damp(float,float,float,float)"/>
        public static float3 Damp(float3 current, float3 target, float sharpness, float deltaTime)
        {
            if (sharpness <= 0f)
            {
                return target;
            }

            return math.lerp(target, current, math.exp(-sharpness * math.max(deltaTime, 0f)));
        }

        /// <summary>Moves <paramref name="current"/> towards <paramref name="target"/> by at most <paramref name="maxDelta"/>.</summary>
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            var gap = target - current;
            if (math.abs(gap) <= maxDelta)
            {
                return target;
            }

            return current + math.sign(gap) * maxDelta;
        }

        /// <inheritdoc cref="MoveTowards(float,float,float)"/>
        public static float3 MoveTowards(float3 current, float3 target, float maxDelta)
        {
            var gap = target - current;
            var distance = math.length(gap);
            if (distance <= maxDelta || distance < 1e-6f)
            {
                return target;
            }

            return current + gap / distance * maxDelta;
        }
    }
}

using Features.ViewmodelFeature.Configs;
using Features.WeaponsFeature.Configs;
using Unity.Mathematics;

namespace Features.ViewmodelFeature.Utils
{
    /// <summary>
    /// The procedural animation's formulas, pure, so each one is tested on its own. Rotations are
    /// Euler offsets in radians (x pitch, +down; y yaw, +right; z roll, +top-left), positions metres
    /// (x right, y up, z forward), all in camera space.
    /// </summary>
    public static class ViewmodelMath
    {
        private const float TwoPi = 2f * math.PI;

        /// <summary>Smoothstep: blends start and finish gently.</summary>
        public static float Ease(float t)
        {
            return math.smoothstep(0f, 1f, math.saturate(t));
        }

        /// <summary>Wraps a phase into [0, 2π) so it never loses precision over a long session.</summary>
        public static float WrapPhase(float phase)
        {
            return phase - TwoPi * math.floor(phase / TwoPi);
        }

        /// <summary>
        /// Turns a kick authored as a distance or angle into the spring velocity that produces a
        /// comparable jump. A spring of angular frequency ω kicked with v swings out by roughly v/ω.
        /// </summary>
        public static float3 KickVelocity(float3 kick, float frequency)
        {
            return kick * (TwoPi * math.max(frequency, 0f));
        }

        /// <summary>
        /// Where the motion spring should sit: lag behind the turn rate (degrees/second; x yaw,
        /// y pitch) and behind the body's velocity in its own frame (x right, y up, z forward, m/s).
        /// </summary>
        public static void MotionTargets(float2 lookRate, float3 localVelocity, ViewmodelConfig config,
            out float3 position, out float3 rotation)
        {
            var yawRate = lookRate.x;
            var pitchRate = lookRate.y;

            // Turning right leaves the weapon pointing left and trailing left; looking up leaves it
            // pointing down and low.
            var maxRotation = config.MaxSwayRotation;
            var rotationDeg = new float3(
                math.clamp(-pitchRate * config.LookSwayRotation, -maxRotation, maxRotation),
                math.clamp(-yawRate * config.LookSwayRotation, -maxRotation, maxRotation),
                math.clamp(yawRate * config.LookSwayRoll, -maxRotation, maxRotation)
                - localVelocity.x * config.StrafeRoll);

            var maxPosition = config.MaxSwayPosition;
            var fall = math.clamp(-localVelocity.y * config.FallLag, -config.MaxFallLag, config.MaxFallLag);
            position = new float3(
                math.clamp(-yawRate * config.LookSwayPosition, -maxPosition, maxPosition) - localVelocity.x * config.MoveLag,
                math.clamp(pitchRate * config.LookSwayPosition, -maxPosition, maxPosition) + fall,
                -localVelocity.z * config.MoveLag);

            rotation = math.radians(rotationDeg);
        }

        /// <summary>
        /// Walk/run bob at <paramref name="phase"/> (π per footstep): a figure-of-eight — side to side
        /// once per two steps, down at each footfall — scaled by <paramref name="amplitude"/>.
        /// </summary>
        public static void Bob(float phase, float amplitude, ViewmodelConfig config,
            out float3 position, out float3 rotation)
        {
            var side = math.sin(phase);

            // cos(2φ) is +1 between footfalls and −1 on them, when the weapon is out to the side.
            var lift = math.cos(2f * phase);

            var positionAmp = config.BobPosition;
            var rotationAmp = config.BobRotation;

            position = new float3(side * positionAmp.x, lift * positionAmp.y, 0f) * amplitude;
            rotation = math.radians(new float3(
                -lift * rotationAmp.x,
                side * rotationAmp.y,
                -side * rotationAmp.z)) * amplitude;
        }

        /// <summary>Idle breathing at <paramref name="phase"/>: a slow rise and a slower drift.</summary>
        public static void Breath(float phase, float amplitude, ViewmodelConfig config,
            out float3 position, out float3 rotation)
        {
            var rise = math.sin(phase);
            var drift = math.sin(phase * 0.5f);

            position = new float3(drift * 0.5f, rise, 0f) * (config.BreathPosition * amplitude);
            rotation = math.radians(new float3(-rise, drift * 0.5f, 0f) * (config.BreathRotation * amplitude));
        }

        /// <summary>
        /// One shot's kick as a distance (metres) and an angle (radians), the random part decided by
        /// <paramref name="shotIndex"/> alone — the same shot always kicks the same way, and no
        /// global random is touched.
        /// </summary>
        public static void RecoilKick(WeaponViewmodel weapon, uint shotIndex, float scale,
            out float3 position, out float3 rotation, out float3 camera)
        {
            var random = Random.CreateFromIndex(shotIndex);
            var spread = random.NextFloat2(new float2(-1f), new float2(1f));
            var cameraYaw = random.NextFloat(-1f, 1f);

            position = (float3)weapon.KickPosition * scale;
            rotation = math.radians(new float3(
                weapon.KickRotation.x,
                weapon.KickRotation.y + spread.x * weapon.KickRandom.x,
                weapon.KickRotation.z + spread.y * weapon.KickRandom.y)) * scale;
            camera = math.radians(new float3(-weapon.CameraKick, cameraYaw * weapon.CameraKickRandom, 0f)) * scale;
        }
    }
}

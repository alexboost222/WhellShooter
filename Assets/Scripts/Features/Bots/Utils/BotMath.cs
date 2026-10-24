using Unity.Mathematics;

namespace Features.Bots.Utils
{
    /// <summary>
    /// Angles and frames for driving a body. Look convention as the body's: radians, +yaw turns
    /// right (yaw 0 faces +Z), +pitch looks <b>down</b>.
    /// </summary>
    public static class BotMath
    {
        /// <summary>Yaw that faces <paramref name="direction"/> (only its XZ part counts).</summary>
        public static float YawOf(float3 direction)
        {
            return math.atan2(direction.x, direction.z);
        }

        /// <summary>Pitch that faces <paramref name="direction"/>; positive when it points down.</summary>
        public static float PitchOf(float3 direction)
        {
            return -math.atan2(direction.y, math.length(direction.xz));
        }

        /// <summary><paramref name="radians"/> wrapped to [−π, π).</summary>
        public static float Wrap(float radians)
        {
            return radians - 2f * math.PI * math.floor((radians + math.PI) / (2f * math.PI));
        }

        /// <summary>Shortest signed turn from one angle to another.</summary>
        public static float DeltaAngle(float from, float to)
        {
            return Wrap(to - from);
        }

        /// <summary>
        /// A world-space walking direction as the body's local move input (x strafe right, y forward)
        /// for a body facing <paramref name="yaw"/>.
        /// </summary>
        public static float2 ToLocalMove(float3 worldDirection, float yaw)
        {
            var forward = new float2(math.sin(yaw), math.cos(yaw));
            var right = new float2(math.cos(yaw), -math.sin(yaw));
            var flat = worldDirection.xz;
            var move = new float2(math.dot(flat, right), math.dot(flat, forward));
            var length = math.length(move);
            return length > 1f ? move / length : move;
        }

        /// <summary>
        /// Turns <paramref name="current"/> towards <paramref name="target"/>: closes a share of the
        /// gap set by <paramref name="sharpness"/>, never faster than <paramref name="maxSpeed"/> rad/s.
        /// </summary>
        public static float TurnTowards(float current, float target, float sharpness, float maxSpeed, float deltaTime)
        {
            var gap = DeltaAngle(current, target);
            var step = gap * (1f - math.exp(-sharpness * deltaTime));
            var limit = maxSpeed * deltaTime;
            step = math.clamp(step, -limit, limit);

            // The exponential never quite arrives; snap the last sliver so "on target" can be true.
            if (math.abs(gap - step) < 1e-4f)
            {
                step = gap;
            }

            return Wrap(current + step);
        }

        /// <summary>Angle between where a body looks and a direction, radians.</summary>
        public static float AngleBetween(float yaw, float pitch, float targetYaw, float targetPitch)
        {
            return math.length(new float2(DeltaAngle(yaw, targetYaw), targetPitch - pitch));
        }
    }
}

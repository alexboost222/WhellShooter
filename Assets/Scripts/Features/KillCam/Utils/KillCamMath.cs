using System.Collections.Generic;
using Features.KillCam.Systems;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.KillCam.Utils
{
    /// <summary>The kill cam's sampling and framing as pure functions (Docs/08 §Kill cam).</summary>
    public static class KillCamMath
    {
        public const float Rise = 4f;

        /// <summary>
        /// <paramref name="id"/> between two recorded frames, <paramref name="t"/> ∈ [0, 1]: feet and
        /// velocity lerped, look slerped the short way; flags from the later frame. False if the body
        /// is in neither.
        /// </summary>
        public static bool TrySample(List<BodySample> from, List<BodySample> to, EntityId id, float t,
            out BodySample sample)
        {
            var hasA = TryFind(from, id, out var a);
            var hasB = TryFind(to, id, out var b);
            if (!hasA && !hasB)
            {
                sample = default;
                return false;
            }

            if (!hasA)
            {
                sample = b;
                return true;
            }

            if (!hasB)
            {
                sample = a;
                return true;
            }

            sample = b;
            sample.Position = math.lerp(a.Position, b.Position, t);
            sample.Velocity = math.lerp(a.Velocity, b.Velocity, t);
            sample.Yaw = LerpAngle(a.Yaw, b.Yaw, t);
            sample.Pitch = math.lerp(a.Pitch, b.Pitch, t);
            return true;
        }

        public static bool TryFind(List<BodySample> frame, EntityId id, out BodySample sample)
        {
            if (frame != null)
            {
                for (var i = 0; i < frame.Count; i++)
                {
                    if (frame[i].Id == id)
                    {
                        sample = frame[i];
                        return true;
                    }
                }
            }

            sample = default;
            return false;
        }

        /// <summary>The short way round, so a look crossing ±π doesn't spin.</summary>
        public static float LerpAngle(float a, float b, float t)
        {
            var delta = b - a;
            delta -= math.floor((delta + math.PI) / (2f * math.PI)) * 2f * math.PI;
            return a + delta * t;
        }

        /// <summary>The eyes of a body: Unity's look convention (+pitch looks down).</summary>
        public static quaternion EyeRotation(float yaw, float pitch)
        {
            return quaternion.EulerZXY(new float3(pitch, yaw, 0f));
        }

        /// <summary>No killer to replay: straight up from the spot, looking down at it.</summary>
        public static float3 Overhead(float3 feet)
        {
            return feet + new float3(0f, Rise, 0f);
        }

        /// <summary>A look rotation from <paramref name="from"/> at <paramref name="target"/>, upright.</summary>
        public static quaternion LookAt(float3 from, float3 target)
        {
            var forward = math.normalizesafe(target - from, new float3(0f, 0f, 1f));
            var up = math.abs(forward.y) > 0.999f ? new float3(0f, 0f, 1f) : new float3(0f, 1f, 0f);
            return quaternion.LookRotationSafe(forward, up);
        }
    }
}

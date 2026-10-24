using Unity.Mathematics;

namespace Features.Combat.Utils
{
    /// <summary>
    /// Ray against an upright capsule — what a hitscan shot tests every body with. Pure maths, so
    /// hits are decided the same headless as in play.
    /// </summary>
    public static class CapsuleRay
    {
        /// <summary>
        /// Distance along the ray to where it enters the capsule standing on <paramref name="feet"/>
        /// (axis +Y, total <paramref name="height"/>, <paramref name="radius"/>), or false. A ray
        /// starting inside counts as a hit at distance 0.
        /// </summary>
        /// <param name="direction">Unit length.</param>
        public static bool Intersect(float3 origin, float3 direction, float3 feet, float height, float radius,
            out float distance)
        {
            var a = feet + new float3(0f, radius, 0f);
            var b = feet + new float3(0f, math.max(height - radius, radius), 0f);
            return IntersectSegmentCapsule(origin, direction, a, b, radius, out distance);
        }

        /// <summary>Ray vs capsule between <paramref name="a"/> and <paramref name="b"/> (Íñigo Quílez's closed form).</summary>
        private static bool IntersectSegmentCapsule(float3 origin, float3 direction, float3 a, float3 b, float radius,
            out float distance)
        {
            distance = 0f;
            var ba = b - a;
            var oa = origin - a;
            var baba = math.dot(ba, ba);
            var bard = math.dot(ba, direction);
            var baoa = math.dot(ba, oa);
            var rdoa = math.dot(direction, oa);
            var oaoa = math.dot(oa, oa);

            // Inside already: a hit at the muzzle.
            var along = baba > 0f ? math.saturate(baoa / baba) : 0f;
            if (math.lengthsq(oa - ba * along) <= radius * radius)
            {
                return true;
            }

            var qa = baba - bard * bard;
            var qb = baba * rdoa - baoa * bard;
            var qc = baba * oaoa - baoa * baoa - radius * radius * baba;
            var h = qb * qb - qa * qc;
            if (qa > 1e-8f && h >= 0f)
            {
                var t = (-qb - math.sqrt(h)) / qa;
                var y = baoa + t * bard;
                if (y > 0f && y < baba && t >= 0f)
                {
                    distance = t;
                    return true;
                }
            }

            // The caps.
            var best = float.MaxValue;
            if (Sphere(origin, direction, a, radius, out var ta))
            {
                best = math.min(best, ta);
            }

            if (Sphere(origin, direction, b, radius, out var tb))
            {
                best = math.min(best, tb);
            }

            if (best < float.MaxValue)
            {
                distance = best;
                return true;
            }

            return false;
        }

        private static bool Sphere(float3 origin, float3 direction, float3 center, float radius, out float distance)
        {
            var oc = origin - center;
            var b = math.dot(oc, direction);
            var c = math.dot(oc, oc) - radius * radius;
            var h = b * b - c;
            distance = 0f;
            if (h < 0f)
            {
                return false;
            }

            var t = -b - math.sqrt(h);
            if (t < 0f)
            {
                return false;
            }

            distance = t;
            return true;
        }
    }
}

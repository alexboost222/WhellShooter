using Unity.Mathematics;

namespace Features.Combat.Systems
{
    /// <summary>
    /// The static world as a shot sees it: how far a ray gets before it hits geometry. Bodies are
    /// not part of it — the hitscan tests those itself.
    /// </summary>
    /// <remarks>
    /// An interface so the hitscan runs headless: tests register an occluder that knows a few
    /// planes, play registers <see cref="WorldShotOccluder"/>.
    /// </remarks>
    public interface IShotOccluder
    {
        /// <summary>Distance to the first world hit within <paramref name="maxDistance"/>, or false.</summary>
        bool Raycast(float3 origin, float3 direction, float maxDistance, out float distance);
    }
}

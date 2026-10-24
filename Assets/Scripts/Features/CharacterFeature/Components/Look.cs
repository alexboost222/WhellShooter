using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.CharacterFeature.Components
{
    /// <summary>
    /// Where the body is facing, in radians — the authority, written directly by its driver. The
    /// player's is updated every frame, never on the tick: look latency is the one thing a
    /// first-person game cannot have.
    /// </summary>
    /// <remarks>
    /// Unity's convention: positive <see cref="Pitch"/> looks <b>down</b>, positive <see cref="Yaw"/>
    /// turns right, yaw wrapped to [−π, π). <see cref="Rotation"/> is what
    /// <c>Quaternion.Euler(pitch, yaw, 0)</c> would give.
    /// </remarks>
    public struct Look : IComponent
    {
        public float Yaw;
        public float Pitch;

        public quaternion Rotation => quaternion.EulerZXY(new float3(Pitch, Yaw, 0f));

        /// <summary>The yaw-only rotation movement is steered by.</summary>
        public quaternion Heading => quaternion.RotateY(Yaw);

        /// <summary>Unit vector the eyes point along.</summary>
        public float3 Forward => math.mul(Rotation, new float3(0f, 0f, 1f));
    }
}

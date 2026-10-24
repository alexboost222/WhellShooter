using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bodies.Components
{
    /// <summary>A dynamic or kinematic <see cref="PhysicsBody"/>'s pose after the last physics step.</summary>
    public struct BodyPose : IComponent
    {
        public float3 Position;
        public quaternion Rotation;

        /// <summary>Pose one step earlier — for render interpolation.</summary>
        public float3 PreviousPosition;

        public quaternion PreviousRotation;
        public bool Sleeping;
    }
}

using Features.Bodies.Enums;
using PhysR;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bodies.Components
{
    /// <summary>
    /// Ask for a physics body on this entity. Consumed in the next physics step, which replaces it
    /// with <see cref="PhysicsBody"/> (and <see cref="BodyPose"/> unless static).
    /// </summary>
    public struct BodyCreateRequest : IComponent
    {
        public BodyKind Kind;
        public BodyShape Shape;

        /// <summary>Box: half extents. Sphere: x = radius. Capsule (Y axis): x = radius, y = half the inner segment.</summary>
        public float3 Size;

        public float3 Position;

        /// <summary>Default (all zero) is read as identity.</summary>
        public quaternion Rotation;

        /// <summary>Dynamic bodies only; 0 = phys-r's default.</summary>
        public float Mass;
    }
}

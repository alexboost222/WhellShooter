using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bodies.Components
{
    /// <summary>Move a <see cref="PhysicsCharacter"/> instantly (respawn). Consumed right before the next step.</summary>
    public struct PhysicsTeleportRequest : IComponent
    {
        public float3 Feet;
    }
}

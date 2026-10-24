using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bodies.Components
{
    /// <summary>
    /// Shift a <see cref="PhysicsCharacter"/> by a prediction error (Docs/05 §Prediction): feet and
    /// velocity both move by the deltas, interpolation is not snapped. Consumed right before the
    /// next step.
    /// </summary>
    public struct PhysicsCharacterCorrection : IComponent
    {
        public float3 FeetDelta;
        public float3 VelocityDelta;
    }
}

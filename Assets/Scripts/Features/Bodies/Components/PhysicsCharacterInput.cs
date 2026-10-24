using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bodies.Components
{
    /// <summary>
    /// This tick's movement for a <see cref="PhysicsCharacter"/>, written by the gameplay motor in
    /// FixedUpdate and pushed into phys-r right before the step. Missing = stand still.
    /// </summary>
    /// <remarks>The physics-level input; the gameplay contract (intent, look) belongs to the character feature.</remarks>
    public struct PhysicsCharacterInput : IComponent
    {
        /// <summary>x = strafe right, y = forward, each −1..1, in the frame of <see cref="Yaw"/>.</summary>
        public float2 Move;

        /// <summary>Facing, radians about +Y; 0 looks down +Z.</summary>
        public float Yaw;

        /// <summary>
        /// Caps this tick's top speed as a fraction of walk / sprint (aiming: 2.8 / 4.5 ≈ 0.62).
        /// 0 = no cap. Only slows down: values above 1 act as 1.
        /// </summary>
        public float MaxSpeedScale;

        public bool Jump;
        public bool Sprint;
        public bool Crouch;
    }
}

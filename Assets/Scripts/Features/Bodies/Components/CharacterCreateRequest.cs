using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bodies.Components
{
    /// <summary>
    /// Ask for a kinematic character capsule on this entity. Consumed in the next physics step,
    /// which replaces it with <see cref="PhysicsCharacter"/> and <see cref="PhysicsCharacterState"/>.
    /// </summary>
    /// <remarks>
    /// Every value left at 0 takes phys-r's default: 0.4 m × 1.8 m capsule, step 0.35 m, walk 6 /
    /// sprint 9 / crouch 3 m/s, ground accel 10, air accel 2, gravity 20 m/s², jump 7 m/s.
    /// </remarks>
    public struct CharacterCreateRequest : IComponent
    {
        /// <summary>Bottom of the capsule.</summary>
        public float3 Feet;

        public float Radius;

        /// <summary>Standing height, hemispheres included.</summary>
        public float Height;

        public float StepHeight;

        public float WalkSpeed;
        public float SprintSpeed;
        public float CrouchSpeed;

        /// <summary>Quake-style acceleration on the ground and in the air (1/s).</summary>
        public float GroundAccel;

        public float AirAccel;

        /// <summary>Downward acceleration, m/s² (positive).</summary>
        public float Gravity;

        public float JumpSpeed;

        /// <summary>Apex height in metres; when set, overrides <see cref="JumpSpeed"/> with √(2·g·h).</summary>
        public float JumpHeight;
    }
}

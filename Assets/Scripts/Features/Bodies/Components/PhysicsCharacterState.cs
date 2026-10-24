using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bodies.Components
{
    /// <summary>
    /// A <see cref="PhysicsCharacter"/>'s state after the last physics step. Written only by
    /// Bodies; the character feature turns it into its gameplay motion.
    /// </summary>
    public struct PhysicsCharacterState : IComponent
    {
        /// <summary>Bottom of the capsule.</summary>
        public float3 Feet;

        /// <summary>Feet one step earlier — for render interpolation.</summary>
        public float3 PreviousFeet;

        public float3 Velocity;
        public bool Grounded;
        public bool Crouched;

        /// <summary>Touched ground this step (see <see cref="LandingSpeed"/>).</summary>
        public bool Landed;

        /// <summary>Jumped this step.</summary>
        public bool Jumped;

        /// <summary>Current capsule height (crouch aware).</summary>
        public float Height;

        /// <summary>Downward speed on the landing step (fall damage), else 0.</summary>
        public float LandingSpeed;
    }
}

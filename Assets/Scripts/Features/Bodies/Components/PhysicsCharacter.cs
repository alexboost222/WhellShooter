using Scellecs.Morpeh;

namespace Features.Bodies.Components
{
    /// <summary>
    /// Link to a native phys-r character. Present ⇔ the character exists; remove it (or the
    /// entity) and the next physics step destroys it.
    /// </summary>
    public struct PhysicsCharacter : IComponent
    {
        public ulong Handle;

        /// <summary>The character's capsule body — what a raycast reports when it hits this character.</summary>
        public ulong Body;
    }
}

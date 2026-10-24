using Scellecs.Morpeh;

namespace Features.Bodies.Components
{
    /// <summary>
    /// Link to a native phys-r body. Present ⇔ the body exists; remove it (or the entity) and the
    /// next physics step destroys the body.
    /// </summary>
    public struct PhysicsBody : IComponent
    {
        /// <summary>The native handle (index + generation) — an id, validated by phys-r on every use.</summary>
        public ulong Handle;
    }
}

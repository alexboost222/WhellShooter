using Scellecs.Morpeh;

namespace Features.Combat.Components
{
    /// <summary>
    /// Request on a body: back to full health and no longer <see cref="Dead"/>. Consumed
    /// (read-then-remove) by <c>ReviveSystem</c>. Whoever respawns the body adds it, together with a
    /// teleport.
    /// </summary>
    public struct ReviveRequest : IComponent
    {
    }
}

using Scellecs.Morpeh;

namespace Engine.Ecs
{
    /// <summary>
    /// "Dispose this entity at the end of the tick." Anything may add it; the game's
    /// entity-destroy feature, listed last, is the only thing that acts on it (Docs/03).
    /// </summary>
    /// <remarks>
    /// <b>Nothing disposes an entity mid-tick.</b> A system that disposed as it went would pull
    /// the ground out from under every system after it in the same tick — and from under the
    /// filters they are iterating. Tagging defers that to one known point.
    /// <para>
    /// The tag lives in Engine while the feature that consumes it lives in <c>Features/</c>, so
    /// Engine code can condemn an entity without referencing game code.
    /// </para>
    /// </remarks>
    public struct Destroy : IComponent
    {
    }
}

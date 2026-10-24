using Scellecs.Morpeh;

namespace Features.PlayerFeature.Components
{
    /// <summary>
    /// "This entity is the local player's body." Client only: there is one once a snapshot names
    /// it (Docs/05 §Prediction). The server's player bodies are <see cref="NetPlayer"/>s.
    /// </summary>
    public struct Player : IComponent
    {
    }
}

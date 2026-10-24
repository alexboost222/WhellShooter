using Scellecs.Morpeh;

namespace Features.CharacterFeature.Components
{
    /// <summary>
    /// "This entity is a body" — the player's or a bot's. Whoever drives it writes
    /// <see cref="CharacterIntent"/> and <see cref="Look"/>; the motor does the rest (Docs/08
    /// §Character).
    /// </summary>
    public struct Character : IComponent
    {
    }
}

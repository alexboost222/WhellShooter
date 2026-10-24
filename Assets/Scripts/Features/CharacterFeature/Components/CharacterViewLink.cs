using Features.CharacterFeature.Providers;
using Scellecs.Morpeh;

namespace Features.CharacterFeature.Components
{
    /// <summary>
    /// Local-only link to the body's view object. Present means live (Docs/03):
    /// <c>CharacterViewSweepSystem</c> drops it the frame the object dies. Added by whichever
    /// feature builds the view — the player's, a bot's.
    /// </summary>
    public struct CharacterViewLink : IComponent
    {
        public CharacterView View;
    }
}

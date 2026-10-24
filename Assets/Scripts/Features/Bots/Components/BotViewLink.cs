using Features.Bots.Providers;
using Scellecs.Morpeh;

namespace Features.Bots.Components
{
    /// <summary>
    /// Local-only link to the bot's visual (its <see cref="BotView"/>). Present means live:
    /// <c>BotAnimatorSystem</c> drops it the frame the object is gone.
    /// </summary>
    public struct BotViewLink : IComponent
    {
        public BotView View;
    }
}

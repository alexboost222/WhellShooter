using Scellecs.Morpeh;

namespace Features.Network.Components
{
    /// <summary>
    /// Server side, beside <see cref="ServerClient"/>: the input the simulation uses this tick.
    /// Rewritten every tick once the client has started.
    /// </summary>
    public struct ConsumedInput : IComponent
    {
        public int Tick;
        public PlayerInput Input;

        /// <summary>The client's input for <see cref="Tick"/> was not there; <see cref="Input"/> repeats the last one.</summary>
        public bool Missed;
    }
}

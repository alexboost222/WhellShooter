using Scellecs.Morpeh;

namespace Features.Network.Components
{
    /// <summary>
    /// Singleton. The simulation tick (Docs/05). Server: the authoritative tick being simulated.
    /// Client: the newest tick it has produced input for, running ahead of the server. Never
    /// decreases.
    /// </summary>
    public struct NetClock : IComponent
    {
        public int Tick;
    }
}

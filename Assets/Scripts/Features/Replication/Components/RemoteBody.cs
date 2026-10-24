using Scellecs.Morpeh;

namespace Features.Replication.Components
{
    /// <summary>
    /// Client: a body drawn from snapshots, in the past (Docs/05 §Remote bodies) — another player or
    /// a bot. Never a <c>Character</c>: nothing simulates it here.
    /// </summary>
    public struct RemoteBody : IComponent
    {
        /// <summary>The snapshot's shot counter last turned into tracers.</summary>
        public ushort Shots;
    }
}

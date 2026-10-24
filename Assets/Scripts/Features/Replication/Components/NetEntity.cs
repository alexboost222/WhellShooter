using Scellecs.Morpeh;

namespace Features.Replication.Components
{
    /// <summary>
    /// A replicated body's id, the same on the server and every client (Docs/05 §Snapshot). Given by
    /// the server to every <c>Character</c>, counting up from 1; 0 is "none" on the wire.
    /// </summary>
    public struct NetEntity : IComponent
    {
        public const int None = 0;

        public int Id;
    }
}

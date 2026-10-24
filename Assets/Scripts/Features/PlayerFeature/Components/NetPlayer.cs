using Scellecs.Morpeh;

namespace Features.PlayerFeature.Components
{
    /// <summary>
    /// Server: "this body is a connected client's" — driven by that client's consumed input, gone
    /// when it leaves (Docs/05 §Replication).
    /// </summary>
    public struct NetPlayer : IComponent
    {
        /// <summary>Transport peer id, as in <c>ServerClient.Peer</c>.</summary>
        public int Peer;
    }
}

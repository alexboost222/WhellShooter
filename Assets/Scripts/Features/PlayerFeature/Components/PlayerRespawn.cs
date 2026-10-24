using Scellecs.Morpeh;

namespace Features.PlayerFeature.Components
{
    /// <summary>On the dead player: seconds until it comes back. Added and consumed by <c>PlayerRespawnSystem</c>.</summary>
    public struct PlayerRespawn : IComponent
    {
        public float Remaining;
    }
}

using Scellecs.Morpeh;

namespace Features.WeaponsFeature.Components
{
    /// <summary>
    /// Server: how far in the past this body's driver sees the others, in ticks — a remote player's
    /// view delay (Docs/05 §Lag compensation). Copied into every <see cref="ShotRequest"/> it fires.
    /// </summary>
    public struct ShotRewind : IComponent
    {
        public int Ticks;
    }
}

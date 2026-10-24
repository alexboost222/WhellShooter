using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.PlayerFeature.Components
{
    /// <summary>
    /// Pointer movement not yet spent on <c>Look</c>, in pixels. Written by input, emptied by
    /// <c>PlayerLookSystem</c> in the same frame.
    /// </summary>
    public struct PlayerLookInput : IComponent
    {
        public float2 Delta;
    }
}

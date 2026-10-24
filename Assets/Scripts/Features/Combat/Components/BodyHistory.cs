using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Combat.Components
{
    /// <summary>
    /// Server: a body's feet after each of the last <see cref="Capacity"/> physics steps — what a
    /// lag-compensated shot is tested against (Docs/05 §Lag compensation).
    /// </summary>
    public struct BodyHistory : IComponent
    {
        public const int Capacity = 32;

        /// <summary>Ring; the newest entry is at <c>Cursor − 1</c>.</summary>
        public float3[] Feet;
        public int Cursor;
        public int Count;
    }
}

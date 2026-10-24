using Features.Map.Enums;
using Unity.Mathematics;

namespace Features.Map.Utils
{
    /// <summary>One thing to place in the arena. See <see cref="MapPieceKind"/> for the convention.</summary>
    public struct MapPiece
    {
        public MapPieceKind Kind;

        /// <summary>Any non-negative number; the palette takes it modulo its prefab count.</summary>
        public int Variant;

        public float3 Position;

        /// <summary>Degrees around Y.</summary>
        public float Yaw;

        public float Scale;

        public override string ToString()
        {
            return $"{Kind}#{Variant} {Position} yaw {Yaw}";
        }
    }
}

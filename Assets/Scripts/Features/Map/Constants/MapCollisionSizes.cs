namespace Features.Map.Constants
{
    /// <summary>
    /// Nominal collision sizes of the Synty PolygonGeneric pieces, in metres (Docs/08, Docs/11).
    /// Collision comes from these numbers and the layout, never from prefab meshes, so it is the
    /// same headless, in the editor and on every peer.
    /// </summary>
    public static class MapCollisionSizes
    {
        /// <summary>Wall, border wall, parapet and cover-wall thickness.</summary>
        public const float WallThickness = 0.22f;

        public const float DoorWidth = 1.2f;
        public const float DoorHeight = 2.2f;

        public const float WindowWidth = 1.2f;
        public const float WindowSill = 1f;
        public const float WindowTop = 2.2f;

        public const float PillarWidth = 0.42f;

        public const float ParapetHeight = 1.5f;
        public const float RoofThickness = 0.2f;

        /// <summary>Stairs climb in steps no higher than the character's step height (0.35 m).</summary>
        public const int StairSteps = 9;

        public const float CoverWallHeight = 1.5f;
        public const float CoverLowSize = 1f;
        public const float CoverHighSize = 1.7f;
        public const float InteriorPropSize = 1f;

        /// <summary>The ground slab under the arena and its outskirts.</summary>
        public const float GroundThickness = 1f;
    }
}

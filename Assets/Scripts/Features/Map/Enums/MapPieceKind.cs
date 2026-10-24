namespace Features.Map.Enums
{
    /// <summary>
    /// What a <see cref="Utils.MapPiece"/> is. <c>MapConfig</c> maps each kind to a list of prefabs;
    /// the layout never names a prefab.
    /// </summary>
    /// <remarks>
    /// Placement convention, shared by the generator and the builder: <c>Position</c> is the centre
    /// of the piece's footprint at its base height, and yaw 0 means a wall runs along +X and stairs
    /// climb towards +Z.
    /// <para>
    /// Values are explicit because <c>MapConfig</c> serializes them: never renumber. The ground is
    /// not a piece — the builder lays one plane (<c>MapConfig.GroundMaterial</c>).
    /// </para>
    /// </remarks>
    public enum MapPieceKind
    {
        Floor = 1,
        Roof = 2,
        Wall = 3,
        WallWindow = 4,
        WallDoor = 5,
        BorderWall = 6,
        Parapet = 7,
        Pillar = 8,
        Stairs = 9,
        CoverLow = 10,
        CoverHigh = 11,
        CoverWall = 12,
        InteriorProp = 13,
        Tree = 14,
        Rock = 15,
        Bush = 16,
        GroundDetail = 17,
        Backdrop = 18,
        Ceiling = 19,
    }
}

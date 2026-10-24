namespace Features.Map.Enums
{
    /// <summary>What occupies one cell of the arena grid at ground level.</summary>
    public enum MapCellKind : byte
    {
        /// <summary>Open ground, walkable.</summary>
        Open = 0,

        /// <summary>Inside a building, walkable.</summary>
        Interior = 1,

        /// <summary>A cover piece or prop stands here — not walkable.</summary>
        Blocked = 2,

        /// <summary>Roof stairs stand here — not walkable at ground level.</summary>
        Stairs = 3,
    }
}

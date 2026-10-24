namespace Features.Map.Enums
{
    /// <summary>What stands on the boundary between two neighbouring cells.</summary>
    public enum MapEdgeKind : byte
    {
        None = 0,
        Wall = 1,
        Window = 2,
        Door = 3,
        Border = 4,
    }
}

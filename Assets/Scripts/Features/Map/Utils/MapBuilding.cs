using Unity.Mathematics;

namespace Features.Map.Utils
{
    /// <summary>A building's footprint in cells, and where its roof stairs stand if it has any.</summary>
    public struct MapBuilding
    {
        /// <summary>The −X/−Z cell of the footprint.</summary>
        public int2 Min;

        public int2 Size;

        public bool HasRoofAccess;

        /// <summary>The cell the stairs stand on (outside the footprint). Valid when <see cref="HasRoofAccess"/>.</summary>
        public int2 StairsCell;

        /// <summary>Unit step from the stairs cell towards the building.</summary>
        public int2 StairsDirection;

        public int2 Max => Min + Size - 1;

        public bool Contains(int2 cell)
        {
            return math.all(cell >= Min) && math.all(cell <= Max);
        }
    }
}

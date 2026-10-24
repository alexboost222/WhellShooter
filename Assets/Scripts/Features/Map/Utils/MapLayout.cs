using System.Collections.Generic;
using Features.Map.Enums;
using Unity.Mathematics;

namespace Features.Map.Utils
{
    /// <summary>
    /// A generated arena: the cell grid, the walls on the edges between cells, the pieces to place
    /// and the spawn points. Pure data — built by <see cref="MapGenerator"/>, drawn by
    /// <see cref="MapBuilder"/>.
    /// </summary>
    /// <remarks>
    /// The grid is centred on the world origin. Cell (0, 0) is the −X/−Z corner. Edges are stored
    /// in two arrays: <c>EdgeX(x, z)</c> is the edge along X on the −Z side of cell (x, z), with
    /// z in [0, Depth]; <c>EdgeZ(x, z)</c> is the edge along Z on the −X side of cell (x, z), with
    /// x in [0, Width].
    /// </remarks>
    public sealed class MapLayout
    {
        private readonly MapCellKind[] _cells;
        private readonly int[] _buildings;
        private readonly MapEdgeKind[] _edgesX;
        private readonly MapEdgeKind[] _edgesZ;

        public MapLayout(int seed, int width, int depth, float cellSize)
        {
            Seed = seed;
            Width = width;
            Depth = depth;
            CellSize = cellSize;
            Origin = new float3(-width * cellSize * 0.5f, 0f, -depth * cellSize * 0.5f);

            _cells = new MapCellKind[width * depth];
            _buildings = new int[width * depth];
            _edgesX = new MapEdgeKind[width * (depth + 1)];
            _edgesZ = new MapEdgeKind[(width + 1) * depth];

            for (var i = 0; i < _buildings.Length; i++)
            {
                _buildings[i] = NoBuilding;
            }
        }

        public const int NoBuilding = -1;

        public int Seed { get; }
        public int Width { get; }
        public int Depth { get; }
        public float CellSize { get; }

        /// <summary>World position of the −X/−Z corner of cell (0, 0).</summary>
        public float3 Origin { get; }

        public List<MapPiece> Pieces { get; } = new List<MapPiece>();
        public List<MapSpawn> Spawns { get; } = new List<MapSpawn>();
        public List<MapBuilding> Buildings { get; } = new List<MapBuilding>();

        public bool InBounds(int2 cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Depth;
        }

        public MapCellKind GetCell(int2 cell)
        {
            return _cells[Index(cell)];
        }

        public void SetCell(int2 cell, MapCellKind kind)
        {
            _cells[Index(cell)] = kind;
        }

        /// <summary>The building a cell belongs to, or <see cref="NoBuilding"/>.</summary>
        public int GetBuilding(int2 cell)
        {
            return _buildings[Index(cell)];
        }

        public void SetBuilding(int2 cell, int building)
        {
            _buildings[Index(cell)] = building;
        }

        public MapEdgeKind GetEdgeX(int x, int z)
        {
            return _edgesX[z * Width + x];
        }

        public void SetEdgeX(int x, int z, MapEdgeKind kind)
        {
            _edgesX[z * Width + x] = kind;
        }

        public MapEdgeKind GetEdgeZ(int x, int z)
        {
            return _edgesZ[z * (Width + 1) + x];
        }

        public void SetEdgeZ(int x, int z, MapEdgeKind kind)
        {
            _edgesZ[z * (Width + 1) + x] = kind;
        }

        /// <summary>The edge crossed when stepping from <paramref name="cell"/> one cell in <paramref name="step"/>.</summary>
        public MapEdgeKind GetEdge(int2 cell, int2 step)
        {
            if (step.x == 1)
            {
                return GetEdgeZ(cell.x + 1, cell.y);
            }

            if (step.x == -1)
            {
                return GetEdgeZ(cell.x, cell.y);
            }

            if (step.y == 1)
            {
                return GetEdgeX(cell.x, cell.y + 1);
            }

            return GetEdgeX(cell.x, cell.y);
        }

        public void SetEdge(int2 cell, int2 step, MapEdgeKind kind)
        {
            if (step.x == 1)
            {
                SetEdgeZ(cell.x + 1, cell.y, kind);
            }
            else if (step.x == -1)
            {
                SetEdgeZ(cell.x, cell.y, kind);
            }
            else if (step.y == 1)
            {
                SetEdgeX(cell.x, cell.y + 1, kind);
            }
            else
            {
                SetEdgeX(cell.x, cell.y, kind);
            }
        }

        /// <summary>World position of a cell's centre at ground level.</summary>
        public float3 CellCenter(int2 cell)
        {
            return Origin + new float3((cell.x + 0.5f) * CellSize, 0f, (cell.y + 0.5f) * CellSize);
        }

        /// <summary>World position of a grid corner (x in [0, Width], z in [0, Depth]).</summary>
        public float3 Corner(int x, int z)
        {
            return Origin + new float3(x * CellSize, 0f, z * CellSize);
        }

        public static bool IsWalkable(MapCellKind kind)
        {
            return kind == MapCellKind.Open || kind == MapCellKind.Interior;
        }

        public static bool IsPassable(MapEdgeKind kind)
        {
            return kind == MapEdgeKind.None || kind == MapEdgeKind.Door;
        }

        private int Index(int2 cell)
        {
            return cell.y * Width + cell.x;
        }
    }
}

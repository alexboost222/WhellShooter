using System;
using UnityEngine;

namespace Features.Map.Utils
{
    /// <summary>
    /// The generator's tuning. Serialized inside <c>MapConfig</c>; the defaults are the numbers in
    /// Docs/08 — change one, change the doc.
    /// </summary>
    [Serializable]
    public struct MapSettings
    {
        [Tooltip("Arena size in cells along X.")]
        [Range(12, 64)] public int Width;

        [Tooltip("Arena size in cells along Z. Team spawn zones sit at both Z ends.")]
        [Range(12, 64)] public int Depth;

        [Tooltip("Metres per cell — the module of the building kit.")]
        public float CellSize;

        [Tooltip("Height of one storey (wall height, roof level).")]
        public float StoreyHeight;

        [Tooltip("Storeys of border wall.")]
        [Range(1, 4)] public int BorderStoreys;

        [Tooltip("Rows of building-free cells at each Z end where a team spawns.")]
        [Range(1, 6)] public int SpawnZoneDepth;

        [Tooltip("Spawn points per team.")]
        [Range(1, 8)] public int TeamSpawns;

        [Tooltip("Free-for-all spawn points, spread by farthest-point sampling.")]
        [Range(0, 32)] public int FreeSpawns;

        public int MinBuildings;
        public int MaxBuildings;

        [Tooltip("Building footprint side, cells.")]
        public int MinBuildingSize;
        public int MaxBuildingSize;

        [Tooltip("Free cells kept between two buildings, and between a building and the border.")]
        [Range(1, 6)] public int BuildingGap;

        [Range(0f, 1f)] public float WindowChance;
        [Range(0f, 1f)] public float ExtraDoorChance;
        [Range(0f, 1f)] public float RoofAccessChance;

        [Tooltip("Share of open cells that get a cover piece (before spacing and connectivity rejections).")]
        [Range(0f, 0.5f)] public float CoverDensity;

        [Tooltip("Share of cover that is low (shoot over it) rather than high (breaks line of sight).")]
        [Range(0f, 1f)] public float LowCoverShare;

        [Tooltip("Chance for an interior cell along a wall to get a prop.")]
        [Range(0f, 1f)] public float InteriorPropChance;

        [Tooltip("Grass, flowers and pebbles per open cell (visual only).")]
        [Range(0f, 3f)] public float DetailDensity;

        [Tooltip("Rings of cells outside the border wall filled with trees and rocks.")]
        [Range(0, 8)] public int OutskirtsRings;

        public static MapSettings Default => new MapSettings
        {
            Width = 26,
            Depth = 26,
            CellSize = 2.5f,
            StoreyHeight = 3f,
            BorderStoreys = 2,
            SpawnZoneDepth = 3,
            TeamSpawns = 4,
            FreeSpawns = 12,
            MinBuildings = 4,
            MaxBuildings = 7,
            MinBuildingSize = 3,
            MaxBuildingSize = 5,
            BuildingGap = 2,
            WindowChance = 0.35f,
            ExtraDoorChance = 0.25f,
            RoofAccessChance = 0.6f,
            CoverDensity = 0.16f,
            LowCoverShare = 0.65f,
            InteriorPropChance = 0.3f,
            DetailDensity = 1.2f,
            OutskirtsRings = 4,
        };

        /// <summary>Throws on settings the generator cannot honour, naming the field.</summary>
        public void Validate()
        {
            if (CellSize <= 0f)
            {
                throw new ArgumentException($"{nameof(CellSize)} must be positive, got {CellSize}.");
            }

            if (StoreyHeight <= 0f)
            {
                throw new ArgumentException($"{nameof(StoreyHeight)} must be positive, got {StoreyHeight}.");
            }

            if (Width < 12 || Depth < 12)
            {
                throw new ArgumentException($"The arena must be at least 12×12 cells, got {Width}×{Depth}.");
            }

            if (MinBuildingSize < 2 || MaxBuildingSize < MinBuildingSize)
            {
                throw new ArgumentException(
                    $"Building size range [{MinBuildingSize}, {MaxBuildingSize}] is invalid (min ≥ 2, max ≥ min).");
            }

            if (MinBuildings < 0 || MaxBuildings < MinBuildings)
            {
                throw new ArgumentException($"Building count range [{MinBuildings}, {MaxBuildings}] is invalid.");
            }

            if (SpawnZoneDepth * 2 + 2 >= Depth)
            {
                throw new ArgumentException(
                    $"{nameof(SpawnZoneDepth)} {SpawnZoneDepth} leaves no room between the zones in depth {Depth}.");
            }
        }
    }
}

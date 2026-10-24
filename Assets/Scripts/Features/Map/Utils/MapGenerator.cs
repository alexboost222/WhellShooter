using System.Collections.Generic;
using Features.Map.Enums;
using Unity.Mathematics;

namespace Features.Map.Utils
{
    /// <summary>
    /// Seed + <see cref="MapSettings"/> → <see cref="MapLayout"/>. Pure and deterministic: the only
    /// randomness is a <see cref="Random"/> seeded from the argument (Docs/08).
    /// </summary>
    /// <remarks>
    /// Two passes. <b>Placement</b> decides the grid — border, buildings and their doors, roof stairs,
    /// spawns, props and cover — and rejects anything that would break connectivity. <b>Emission</b>
    /// then turns the grid into <see cref="MapPiece"/>s; it never changes the grid, so everything the
    /// tests check is decided before a single piece exists.
    /// </remarks>
    public static class MapGenerator
    {
        /// <summary>Interior floors sit this far above the ground plane, so the two never z-fight.</summary>
        public const float FloorLift = 0.02f;


        private static readonly int2[] Steps =
        {
            new int2(1, 0),
            new int2(-1, 0),
            new int2(0, 1),
            new int2(0, -1),
        };

        public static MapLayout Generate(int seed, MapSettings settings)
        {
            settings.Validate();

            var random = CreateRandom(seed);
            var layout = new MapLayout(seed, settings.Width, settings.Depth, settings.CellSize);
            var keepClear = new bool[settings.Width * settings.Depth];

            PlaceBorder(layout);
            PlaceBuildings(layout, settings, keepClear, ref random);
            PlaceRoofStairs(layout, settings, keepClear, ref random);
            PlaceTeamSpawns(layout, settings, keepClear);
            PlaceInteriorProps(layout, settings, keepClear, ref random);
            PlaceCover(layout, settings, keepClear, ref random);
            PlaceFreeSpawns(layout, settings, ref random);

            EmitWalls(layout, settings, ref random);
            EmitBuildings(layout, settings, ref random);
            EmitOutskirts(layout, settings, ref random);
            EmitGroundDetail(layout, settings, ref random);

            return layout;
        }

        /// <summary>
        /// Whether every walkable ground cell can reach every other through passable edges.
        /// Public because the tests assert it on the finished layout too.
        /// </summary>
        public static bool IsConnected(MapLayout layout)
        {
            var total = 0;
            var start = new int2(-1, -1);
            for (var z = 0; z < layout.Depth; z++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    if (!MapLayout.IsWalkable(layout.GetCell(new int2(x, z))))
                    {
                        continue;
                    }

                    total++;
                    if (start.x < 0)
                    {
                        start = new int2(x, z);
                    }
                }
            }

            if (total == 0)
            {
                return true;
            }

            return Flood(layout, start, null) == total;
        }

        /// <summary>
        /// Breadth-first walk over walkable cells from <paramref name="start"/>; returns how many it
        /// reached and, when <paramref name="distances"/> is given, fills it with step counts (−1 = unreached).
        /// </summary>
        public static int Flood(MapLayout layout, int2 start, int[] distances)
        {
            var visited = new bool[layout.Width * layout.Depth];
            var queue = new Queue<int2>();
            queue.Enqueue(start);
            visited[start.y * layout.Width + start.x] = true;

            if (distances != null)
            {
                for (var i = 0; i < distances.Length; i++)
                {
                    distances[i] = -1;
                }

                distances[start.y * layout.Width + start.x] = 0;
            }

            var reached = 0;
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                reached++;

                for (var i = 0; i < Steps.Length; i++)
                {
                    var next = cell + Steps[i];
                    if (!layout.InBounds(next))
                    {
                        continue;
                    }

                    var index = next.y * layout.Width + next.x;
                    if (visited[index])
                    {
                        continue;
                    }

                    if (!MapLayout.IsWalkable(layout.GetCell(next)) || !MapLayout.IsPassable(layout.GetEdge(cell, Steps[i])))
                    {
                        continue;
                    }

                    visited[index] = true;
                    if (distances != null)
                    {
                        distances[index] = distances[cell.y * layout.Width + cell.x] + 1;
                    }

                    queue.Enqueue(next);
                }
            }

            return reached;
        }

        // ---------------------------------------------------------------- placement

        private static void PlaceBorder(MapLayout layout)
        {
            for (var x = 0; x < layout.Width; x++)
            {
                layout.SetEdgeX(x, 0, MapEdgeKind.Border);
                layout.SetEdgeX(x, layout.Depth, MapEdgeKind.Border);
            }

            for (var z = 0; z < layout.Depth; z++)
            {
                layout.SetEdgeZ(0, z, MapEdgeKind.Border);
                layout.SetEdgeZ(layout.Width, z, MapEdgeKind.Border);
            }
        }

        private static void PlaceBuildings(MapLayout layout, MapSettings settings, bool[] keepClear, ref Random random)
        {
            var target = random.NextInt(settings.MinBuildings, settings.MaxBuildings + 1);
            var gap = settings.BuildingGap;

            // Buildings stay out of both spawn zones plus one row, so each team starts in the open.
            var zLow = settings.SpawnZoneDepth + 1;
            var zHigh = settings.Depth - settings.SpawnZoneDepth - 1;

            for (var attempt = 0; attempt < target * 40 && layout.Buildings.Count < target; attempt++)
            {
                var size = new int2(
                    random.NextInt(settings.MinBuildingSize, settings.MaxBuildingSize + 1),
                    random.NextInt(settings.MinBuildingSize, settings.MaxBuildingSize + 1));

                var xMax = settings.Width - gap - size.x;
                var zMax = zHigh - size.y;
                if (xMax < gap || zMax < zLow)
                {
                    continue;
                }

                var min = new int2(random.NextInt(gap, xMax + 1), random.NextInt(zLow, zMax + 1));
                if (!IsAreaFree(layout, min - gap, min + size - 1 + gap))
                {
                    continue;
                }

                var building = new MapBuilding { Min = min, Size = size };
                var id = layout.Buildings.Count;
                layout.Buildings.Add(building);

                for (var z = min.y; z < min.y + size.y; z++)
                {
                    for (var x = min.x; x < min.x + size.x; x++)
                    {
                        layout.SetCell(new int2(x, z), MapCellKind.Interior);
                        layout.SetBuilding(new int2(x, z), id);
                    }
                }

                PlaceBuildingWalls(layout, settings, building, keepClear, ref random);
            }
        }

        private static bool IsAreaFree(MapLayout layout, int2 min, int2 max)
        {
            for (var z = min.y; z <= max.y; z++)
            {
                for (var x = min.x; x <= max.x; x++)
                {
                    var cell = new int2(x, z);
                    if (!layout.InBounds(cell))
                    {
                        continue;
                    }

                    if (layout.GetBuilding(cell) != MapLayout.NoBuilding)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void PlaceBuildingWalls(MapLayout layout, MapSettings settings, MapBuilding building,
            bool[] keepClear, ref Random random)
        {
            // Every perimeter segment starts as a wall.
            var sides = new List<WallSegment>[4];
            for (var side = 0; side < 4; side++)
            {
                sides[side] = PerimeterSide(building, side);
                foreach (var segment in sides[side])
                {
                    layout.SetEdge(segment.Inside, segment.Outward, MapEdgeKind.Wall);
                }
            }

            // At least two doors on different sides, so a building is never a dead end.
            var first = random.NextInt(0, 4);
            var second = (first + random.NextInt(1, 4)) % 4;
            for (var side = 0; side < 4; side++)
            {
                if (side == first || side == second || random.NextFloat() < settings.ExtraDoorChance)
                {
                    var segments = sides[side];
                    // Away from the corners when the side is long enough: a door at a corner reads
                    // as a hole in the wall and leaves no wall to fight from.
                    var index = segments.Count >= 3 ? random.NextInt(1, segments.Count - 1) : random.NextInt(0, segments.Count);
                    var door = segments[index];
                    layout.SetEdge(door.Inside, door.Outward, MapEdgeKind.Door);
                    MarkClear(layout, keepClear, door.Inside);
                    MarkClear(layout, keepClear, door.Inside + door.Outward);
                }
            }

            for (var side = 0; side < 4; side++)
            {
                foreach (var segment in sides[side])
                {
                    if (layout.GetEdge(segment.Inside, segment.Outward) == MapEdgeKind.Wall &&
                        random.NextFloat() < settings.WindowChance)
                    {
                        layout.SetEdge(segment.Inside, segment.Outward, MapEdgeKind.Window);
                    }
                }
            }
        }

        private static void PlaceRoofStairs(MapLayout layout, MapSettings settings, bool[] keepClear, ref Random random)
        {
            for (var id = 0; id < layout.Buildings.Count; id++)
            {
                var building = layout.Buildings[id];
                if (random.NextFloat() >= settings.RoofAccessChance)
                {
                    continue;
                }

                // Gather every segment the stairs could stand against, then try them in random order.
                var candidates = new List<WallSegment>();
                for (var side = 0; side < 4; side++)
                {
                    candidates.AddRange(PerimeterSide(building, side));
                }

                Shuffle(candidates, ref random);

                foreach (var segment in candidates)
                {
                    var stairs = segment.Inside + segment.Outward;
                    var approach = stairs + segment.Outward;
                    if (!layout.InBounds(approach) || layout.GetEdge(segment.Inside, segment.Outward) == MapEdgeKind.Door)
                    {
                        continue;
                    }

                    if (layout.GetCell(stairs) != MapCellKind.Open || layout.GetCell(approach) != MapCellKind.Open)
                    {
                        continue;
                    }

                    if (IsClear(layout, keepClear, stairs) || IsClear(layout, keepClear, approach))
                    {
                        continue;
                    }

                    layout.SetCell(stairs, MapCellKind.Stairs);
                    if (!IsConnected(layout))
                    {
                        layout.SetCell(stairs, MapCellKind.Open);
                        continue;
                    }

                    // The stairs lean on that wall, so it can't be a window.
                    layout.SetEdge(segment.Inside, segment.Outward, MapEdgeKind.Wall);
                    MarkClear(layout, keepClear, approach);

                    building.HasRoofAccess = true;
                    building.StairsCell = stairs;
                    building.StairsDirection = -segment.Outward;
                    layout.Buildings[id] = building;
                    break;
                }
            }
        }

        private static void PlaceTeamSpawns(MapLayout layout, MapSettings settings, bool[] keepClear)
        {
            for (var team = 0; team < 2; team++)
            {
                // One row in from the border wall, spread evenly along X, facing the other team.
                var z = team == 0 ? 1 : settings.Depth - 2;
                var yaw = team == 0 ? 0f : 180f;

                for (var i = 0; i < settings.TeamSpawns; i++)
                {
                    var x = (int)math.round((i + 1) * settings.Width / (float)(settings.TeamSpawns + 1) - 0.5f);
                    var cell = new int2(math.clamp(x, 0, settings.Width - 1), z);

                    layout.Spawns.Add(new MapSpawn
                    {
                        Cell = cell,
                        Position = layout.CellCenter(cell),
                        Yaw = yaw,
                        Team = team,
                    });

                    MarkClear(layout, keepClear, cell);
                }
            }
        }

        private static void PlaceInteriorProps(MapLayout layout, MapSettings settings, bool[] keepClear, ref Random random)
        {
            for (var z = 0; z < layout.Depth; z++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    var cell = new int2(x, z);
                    if (layout.GetCell(cell) != MapCellKind.Interior || IsClear(layout, keepClear, cell))
                    {
                        continue;
                    }

                    // Against a solid wall only, so props line rooms instead of floating mid-floor.
                    var wall = new int2(0, 0);
                    for (var i = 0; i < Steps.Length; i++)
                    {
                        if (layout.GetEdge(cell, Steps[i]) == MapEdgeKind.Wall)
                        {
                            wall = Steps[i];
                            break;
                        }
                    }

                    if (math.all(wall == 0) || random.NextFloat() >= settings.InteriorPropChance)
                    {
                        continue;
                    }

                    if (!TryBlock(layout, cell))
                    {
                        continue;
                    }

                    var position = layout.CellCenter(cell) + new float3(wall.x, 0f, wall.y) * (layout.CellSize * 0.3f);
                    layout.Pieces.Add(new MapPiece
                    {
                        Kind = MapPieceKind.InteriorProp,
                        Variant = random.NextInt(0, int.MaxValue),
                        Position = position,
                        Yaw = YawOf(-wall),
                        Scale = 1f,
                    });
                }
            }
        }

        private static void PlaceCover(MapLayout layout, MapSettings settings, bool[] keepClear, ref Random random)
        {
            var candidates = new List<int2>();
            for (var z = 0; z < layout.Depth; z++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    var cell = new int2(x, z);
                    if (layout.GetCell(cell) == MapCellKind.Open && !IsClear(layout, keepClear, cell))
                    {
                        candidates.Add(cell);
                    }
                }
            }

            Shuffle(candidates, ref random);
            var target = (int)math.round(candidates.Count * settings.CoverDensity);
            var placed = 0;

            foreach (var cell in candidates)
            {
                if (placed >= target)
                {
                    break;
                }

                if (HasBlockedNeighbour(layout, cell) || !TryBlock(layout, cell))
                {
                    continue;
                }

                placed++;
                var center = layout.CellCenter(cell);
                var low = random.NextFloat() < settings.LowCoverShare;

                if (low && random.NextBool())
                {
                    // A waist-high wall: snapped to the grid so it lines up with the lanes.
                    layout.Pieces.Add(new MapPiece
                    {
                        Kind = MapPieceKind.CoverWall,
                        Variant = random.NextInt(0, int.MaxValue),
                        Position = center,
                        Yaw = random.NextBool() ? 0f : 90f,
                        Scale = 1f,
                    });
                    continue;
                }

                var jitter = layout.CellSize * 0.15f;
                layout.Pieces.Add(new MapPiece
                {
                    Kind = low ? MapPieceKind.CoverLow : MapPieceKind.CoverHigh,
                    Variant = random.NextInt(0, int.MaxValue),
                    Position = center + new float3(random.NextFloat(-jitter, jitter), 0f, random.NextFloat(-jitter, jitter)),
                    Yaw = random.NextFloat(0f, 360f),
                    Scale = 1f,
                });
            }
        }

        private static void PlaceFreeSpawns(MapLayout layout, MapSettings settings, ref Random random)
        {
            if (settings.FreeSpawns == 0)
            {
                return;
            }

            // Anywhere walkable outside the team zones.
            var candidates = new List<int2>();
            for (var z = settings.SpawnZoneDepth; z < settings.Depth - settings.SpawnZoneDepth; z++)
            {
                for (var x = 0; x < settings.Width; x++)
                {
                    var cell = new int2(x, z);
                    if (MapLayout.IsWalkable(layout.GetCell(cell)))
                    {
                        candidates.Add(cell);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            // Farthest-point sampling: each new spawn is the cell farthest from every spawn so far.
            var chosen = new List<int2> { candidates[random.NextInt(0, candidates.Count)] };
            while (chosen.Count < settings.FreeSpawns && chosen.Count < candidates.Count)
            {
                var best = candidates[0];
                var bestDistance = -1f;
                foreach (var candidate in candidates)
                {
                    var nearest = float.MaxValue;
                    foreach (var spawn in chosen)
                    {
                        nearest = math.min(nearest, math.distancesq(candidate, spawn));
                    }

                    if (nearest > bestDistance)
                    {
                        bestDistance = nearest;
                        best = candidate;
                    }
                }

                chosen.Add(best);
            }

            var middle = layout.CellCenter(new int2(settings.Width / 2, settings.Depth / 2));
            foreach (var cell in chosen)
            {
                var position = layout.CellCenter(cell);
                var toMiddle = middle - position;
                layout.Spawns.Add(new MapSpawn
                {
                    Cell = cell,
                    Position = position,
                    // Facing the middle of the map, where the fight is.
                    Yaw = math.degrees(math.atan2(toMiddle.x, toMiddle.z)),
                    Team = -1,
                });
            }
        }

        private static bool TryBlock(MapLayout layout, int2 cell)
        {
            var previous = layout.GetCell(cell);
            layout.SetCell(cell, MapCellKind.Blocked);
            if (IsConnected(layout))
            {
                return true;
            }

            layout.SetCell(cell, previous);
            return false;
        }

        private static bool HasBlockedNeighbour(MapLayout layout, int2 cell)
        {
            for (var dz = -1; dz <= 1; dz++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    var next = cell + new int2(dx, dz);
                    if ((dx != 0 || dz != 0) && layout.InBounds(next) && layout.GetCell(next) == MapCellKind.Blocked)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // ---------------------------------------------------------------- emission

        private static void EmitWalls(MapLayout layout, MapSettings settings, ref Random random)
        {
            var half = layout.CellSize * 0.5f;

            for (var z = 0; z <= layout.Depth; z++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    EmitEdge(layout, settings, layout.GetEdgeX(x, z), layout.Corner(x, z) + new float3(half, 0f, 0f), 0f,
                        ref random);
                }
            }

            for (var z = 0; z < layout.Depth; z++)
            {
                for (var x = 0; x <= layout.Width; x++)
                {
                    EmitEdge(layout, settings, layout.GetEdgeZ(x, z), layout.Corner(x, z) + new float3(0f, 0f, half), 90f,
                        ref random);
                }
            }

            // Pillars where walls turn a corner — the modular kit leaves a seam there otherwise.
            for (var z = 0; z <= layout.Depth; z++)
            {
                for (var x = 0; x <= layout.Width; x++)
                {
                    if (!IsWallCorner(layout, x, z, out var border))
                    {
                        continue;
                    }

                    var storeys = border ? settings.BorderStoreys : 1;
                    for (var storey = 0; storey < storeys; storey++)
                    {
                        layout.Pieces.Add(new MapPiece
                        {
                            Kind = MapPieceKind.Pillar,
                            Variant = 0,
                            Position = layout.Corner(x, z) + new float3(0f, storey * settings.StoreyHeight, 0f),
                            Yaw = 0f,
                            Scale = 1f,
                        });
                    }
                }
            }
        }

        private static void EmitEdge(MapLayout layout, MapSettings settings, MapEdgeKind edge, float3 position, float yaw,
            ref Random random)
        {
            switch (edge)
            {
                case MapEdgeKind.None:
                    return;
                case MapEdgeKind.Border:
                    for (var storey = 0; storey < settings.BorderStoreys; storey++)
                    {
                        layout.Pieces.Add(new MapPiece
                        {
                            Kind = MapPieceKind.BorderWall,
                            Variant = random.NextInt(0, int.MaxValue),
                            Position = position + new float3(0f, storey * settings.StoreyHeight, 0f),
                            Yaw = yaw,
                            Scale = 1f,
                        });
                    }

                    return;
            }

            layout.Pieces.Add(new MapPiece
            {
                Kind = edge == MapEdgeKind.Door ? MapPieceKind.WallDoor :
                    edge == MapEdgeKind.Window ? MapPieceKind.WallWindow : MapPieceKind.Wall,
                Variant = random.NextInt(0, int.MaxValue),
                Position = position,
                Yaw = yaw,
                Scale = 1f,
            });
        }

        private static bool IsWallCorner(MapLayout layout, int x, int z, out bool border)
        {
            // The four edges meeting at this grid corner.
            var alongX = 0;
            var alongZ = 0;
            border = false;

            if (x > 0 && Count(layout.GetEdgeX(x - 1, z), ref border))
            {
                alongX++;
            }

            if (x < layout.Width && Count(layout.GetEdgeX(x, z), ref border))
            {
                alongX++;
            }

            if (z > 0 && Count(layout.GetEdgeZ(x, z - 1), ref border))
            {
                alongZ++;
            }

            if (z < layout.Depth && Count(layout.GetEdgeZ(x, z), ref border))
            {
                alongZ++;
            }

            return alongX > 0 && alongZ > 0;
        }

        private static bool Count(MapEdgeKind edge, ref bool border)
        {
            if (edge == MapEdgeKind.Border)
            {
                border = true;
            }

            return edge != MapEdgeKind.None;
        }

        private static void EmitBuildings(MapLayout layout, MapSettings settings, ref Random random)
        {
            var roofHeight = settings.StoreyHeight;
            var half = layout.CellSize * 0.5f;

            foreach (var building in layout.Buildings)
            {
                for (var z = building.Min.y; z <= building.Max.y; z++)
                {
                    for (var x = building.Min.x; x <= building.Max.x; x++)
                    {
                        var center = layout.CellCenter(new int2(x, z));
                        layout.Pieces.Add(new MapPiece
                        {
                            Kind = MapPieceKind.Floor,
                            Variant = random.NextInt(0, int.MaxValue),
                            Position = center + new float3(0f, FloorLift, 0f),
                            Yaw = 0f,
                            Scale = 1f,
                        });
                        layout.Pieces.Add(new MapPiece
                        {
                            Kind = MapPieceKind.Roof,
                            Variant = random.NextInt(0, int.MaxValue),
                            Position = center + new float3(0f, roofHeight, 0f),
                            Yaw = 0f,
                            Scale = 1f,
                        });
                        layout.Pieces.Add(new MapPiece
                        {
                            Kind = MapPieceKind.Ceiling,
                            Variant = random.NextInt(0, int.MaxValue),
                            Position = center + new float3(0f, roofHeight - 0.1f, 0f),
                            Yaw = 0f,
                            Scale = 1f,
                        });
                    }
                }

                // A parapet round the roof, open only where the stairs arrive.
                for (var side = 0; side < 4; side++)
                {
                    foreach (var segment in PerimeterSide(building, side))
                    {
                        var stairs = segment.Inside + segment.Outward;
                        if (building.HasRoofAccess && math.all(stairs == building.StairsCell))
                        {
                            continue;
                        }

                        var edgeCenter = layout.CellCenter(segment.Inside) +
                                         new float3(segment.Outward.x, 0f, segment.Outward.y) * half;
                        layout.Pieces.Add(new MapPiece
                        {
                            Kind = MapPieceKind.Parapet,
                            Variant = random.NextInt(0, int.MaxValue),
                            Position = edgeCenter + new float3(0f, roofHeight, 0f),
                            Yaw = segment.Outward.x != 0 ? 90f : 0f,
                            Scale = 1f,
                        });
                    }
                }

                if (building.HasRoofAccess)
                {
                    layout.Pieces.Add(new MapPiece
                    {
                        Kind = MapPieceKind.Stairs,
                        Variant = 0,
                        Position = layout.CellCenter(building.StairsCell),
                        Yaw = YawOf(building.StairsDirection),
                        Scale = 1f,
                    });
                }
            }
        }

        private static void EmitOutskirts(MapLayout layout, MapSettings settings, ref Random random)
        {
            var rings = settings.OutskirtsRings;
            var jitter = layout.CellSize * 0.4f;

            for (var z = -rings; z < settings.Depth + rings; z++)
            {
                for (var x = -rings; x < settings.Width + rings; x++)
                {
                    if (layout.InBounds(new int2(x, z)))
                    {
                        continue;
                    }

                    // The ring hugging the wall stays bushes and rocks only: a tree there would
                    // poke its crown over the wall into the arena.
                    var ring = math.max(math.max(-x, x - settings.Width + 1), math.max(-z, z - settings.Depth + 1));
                    var roll = random.NextFloat();
                    MapPieceKind kind;
                    if (ring > 1 && roll < 0.45f)
                    {
                        kind = MapPieceKind.Tree;
                    }
                    else if (roll < 0.6f)
                    {
                        kind = MapPieceKind.Bush;
                    }
                    else if (roll < 0.75f)
                    {
                        kind = MapPieceKind.Rock;
                    }
                    else
                    {
                        continue;
                    }

                    var center = layout.Origin + new float3((x + 0.5f) * layout.CellSize, 0f, (z + 0.5f) * layout.CellSize);
                    layout.Pieces.Add(new MapPiece
                    {
                        Kind = kind,
                        Variant = random.NextInt(0, int.MaxValue),
                        Position = center + new float3(random.NextFloat(-jitter, jitter), 0f, random.NextFloat(-jitter, jitter)),
                        Yaw = random.NextFloat(0f, 360f),
                        Scale = random.NextFloat(0.85f, 1.25f),
                    });
                }
            }

            // Mountains on the horizon, a ring of them facing the arena.
            var radius = math.max(settings.Width, settings.Depth) * layout.CellSize * 0.5f + rings * layout.CellSize + 45f;
            const int backdrops = 8;
            for (var i = 0; i < backdrops; i++)
            {
                var angle = (i + random.NextFloat(-0.15f, 0.15f)) * (360f / backdrops);
                var radians = math.radians(angle);
                layout.Pieces.Add(new MapPiece
                {
                    Kind = MapPieceKind.Backdrop,
                    Variant = random.NextInt(0, int.MaxValue),
                    Position = new float3(math.sin(radians), 0f, math.cos(radians)) * radius,
                    Yaw = angle,
                    Scale = random.NextFloat(0.9f, 1.3f),
                });
            }
        }

        private static void EmitGroundDetail(MapLayout layout, MapSettings settings, ref Random random)
        {
            var spread = layout.CellSize * 0.35f;
            for (var z = 0; z < layout.Depth; z++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    var cell = new int2(x, z);
                    // Outdoors only: a building cell holding a prop is Blocked, not Interior.
                    if (layout.GetBuilding(cell) != MapLayout.NoBuilding || layout.GetCell(cell) == MapCellKind.Stairs)
                    {
                        continue;
                    }

                    // Whole part plus a chance at one more, so a density of 1.2 means "1, sometimes 2".
                    var count = (int)settings.DetailDensity;
                    if (random.NextFloat() < settings.DetailDensity - count)
                    {
                        count++;
                    }

                    var center = layout.CellCenter(cell);
                    for (var i = 0; i < count; i++)
                    {
                        layout.Pieces.Add(new MapPiece
                        {
                            Kind = MapPieceKind.GroundDetail,
                            Variant = random.NextInt(0, int.MaxValue),
                            Position = center + new float3(random.NextFloat(-spread, spread), 0f, random.NextFloat(-spread, spread)),
                            Yaw = random.NextFloat(0f, 360f),
                            Scale = random.NextFloat(0.8f, 1.2f),
                        });
                    }
                }
            }
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>
        /// The perimeter segments of one side of a building: 0 = +X, 1 = −X, 2 = +Z, 3 = −Z.
        /// </summary>
        private static List<WallSegment> PerimeterSide(MapBuilding building, int side)
        {
            var segments = new List<WallSegment>();
            var outward = Steps[side];

            if (outward.x != 0)
            {
                var x = outward.x > 0 ? building.Max.x : building.Min.x;
                for (var z = building.Min.y; z <= building.Max.y; z++)
                {
                    segments.Add(new WallSegment { Inside = new int2(x, z), Outward = outward });
                }
            }
            else
            {
                var z = outward.y > 0 ? building.Max.y : building.Min.y;
                for (var x = building.Min.x; x <= building.Max.x; x++)
                {
                    segments.Add(new WallSegment { Inside = new int2(x, z), Outward = outward });
                }
            }

            return segments;
        }

        /// <summary>Yaw that turns +Z into <paramref name="direction"/>.</summary>
        private static float YawOf(int2 direction)
        {
            return math.degrees(math.atan2(direction.x, direction.y));
        }

        private static void MarkClear(MapLayout layout, bool[] keepClear, int2 cell)
        {
            if (layout.InBounds(cell))
            {
                keepClear[cell.y * layout.Width + cell.x] = true;
            }
        }

        private static bool IsClear(MapLayout layout, bool[] keepClear, int2 cell)
        {
            return keepClear[cell.y * layout.Width + cell.x];
        }

        private static void Shuffle<T>(List<T> list, ref Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.NextInt(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static Random CreateRandom(int seed)
        {
            // Random rejects a zero state, and neighbouring seeds should not give neighbouring maps.
            var state = math.hash(new int2(seed, 0x5EED));
            return new Random(state == 0 ? 1u : state);
        }

        private struct WallSegment
        {
            /// <summary>The building cell on the inner side of the segment.</summary>
            public int2 Inside;

            /// <summary>Unit step from inside to outside.</summary>
            public int2 Outward;
        }
    }
}

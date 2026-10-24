using System.Collections.Generic;
using Features.Map.Enums;
using Features.Map.Utils;
using Unity.Mathematics;

namespace Features.Navigation.Utils
{
    /// <summary>
    /// What an agent needs to know about the arena: where it can stand, which steps between cells it
    /// can take, and what it can see through. Built once per <see cref="MapLayout"/>; pure C#, so
    /// everything on top of it runs headless (Docs/08 §Navigation).
    /// </summary>
    /// <remarks>
    /// <b>Two different questions, two different rules.</b> Walking is blocked by every wall, window
    /// and cover piece; sight is blocked only by solid walls and high cover — windows, doorways and
    /// low cover are exactly the things you shoot through or over.
    /// <para>
    /// Ground level only: roofs are not part of the grid yet (Docs/09).
    /// </para>
    /// </remarks>
    public sealed class NavGrid
    {
        private static readonly int2[] Steps =
        {
            new int2(1, 0),
            new int2(-1, 0),
            new int2(0, 1),
            new int2(0, -1),
        };

        private readonly MapLayout _layout;
        private readonly bool[] _sightBlocked;
        private readonly List<int2> _walkableCells = new List<int2>();

        public NavGrid(MapLayout layout)
        {
            _layout = layout;
            _sightBlocked = new bool[layout.Width * layout.Depth];

            for (var z = 0; z < layout.Depth; z++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    var cell = new int2(x, z);
                    if (MapLayout.IsWalkable(layout.GetCell(cell)))
                    {
                        _walkableCells.Add(cell);
                    }
                }
            }

            // High cover is what breaks line of sight; it stands on a Blocked cell, jittered within it.
            foreach (var piece in layout.Pieces)
            {
                if (piece.Kind != MapPieceKind.CoverHigh)
                {
                    continue;
                }

                var cell = CellOf(piece.Position);
                if (InBounds(cell))
                {
                    _sightBlocked[Index(cell)] = true;
                }
            }
        }

        public int Width => _layout.Width;
        public int Depth => _layout.Depth;
        public float CellSize => _layout.CellSize;
        public int Seed => _layout.Seed;

        /// <summary>Every walkable ground cell, row by row — stable order, for seeded picks.</summary>
        public IReadOnlyList<int2> WalkableCells => _walkableCells;

        public bool InBounds(int2 cell)
        {
            return _layout.InBounds(cell);
        }

        public bool IsWalkable(int2 cell)
        {
            return InBounds(cell) && MapLayout.IsWalkable(_layout.GetCell(cell));
        }

        /// <summary>Whether an agent on <paramref name="cell"/> can step one cell in an axis direction.</summary>
        public bool CanStep(int2 cell, int2 step)
        {
            var next = cell + step;
            return IsWalkable(cell) && IsWalkable(next) && MapLayout.IsPassable(_layout.GetEdge(cell, step));
        }

        /// <summary>Whether sight passes from <paramref name="cell"/> into its axis neighbour.</summary>
        public bool CanSee(int2 cell, int2 step)
        {
            var next = cell + step;
            if (!InBounds(next) || _sightBlocked[Index(next)])
            {
                return false;
            }

            var edge = _layout.GetEdge(cell, step);
            return edge != MapEdgeKind.Wall && edge != MapEdgeKind.Border;
        }

        public int2 CellOf(float3 position)
        {
            var local = (position.xz - _layout.Origin.xz) / _layout.CellSize;
            return (int2)math.floor(local);
        }

        public float3 CellCenter(int2 cell)
        {
            return _layout.CellCenter(cell);
        }

        public int Index(int2 cell)
        {
            return cell.y * _layout.Width + cell.x;
        }

        public int2 CellAt(int index)
        {
            return new int2(index % _layout.Width, index / _layout.Width);
        }

        /// <summary>
        /// Whether a body of <paramref name="radius"/> can walk the straight segment between two
        /// points without leaving walkable ground or crossing a wall. Tests the centre line and both
        /// flanks, so a path never shaves a corner the body would catch on.
        /// </summary>
        public bool CanWalkStraight(float3 from, float3 to, float radius)
        {
            if (!Trace(from, to, false))
            {
                return false;
            }

            var along = to.xz - from.xz;
            var length = math.length(along);
            if (radius <= 0f || length < 1e-4f)
            {
                return true;
            }

            var side = new float2(-along.y, along.x) / length * radius;
            var offset = new float3(side.x, 0f, side.y);
            return Trace(from + offset, to + offset, false) && Trace(from - offset, to - offset, false);
        }

        /// <summary>
        /// Whether sight passes between two points at ground level: no solid wall, no high cover in
        /// between. The cells of both ends themselves never block.
        /// </summary>
        public bool HasLineOfSight(float3 from, float3 to)
        {
            return Trace(from, to, true);
        }

        /// <summary>
        /// The walkable cell nearest <paramref name="cell"/> by rings of growing radius, scanned in a
        /// fixed order so the answer is deterministic.
        /// </summary>
        public bool TryNearestWalkable(int2 cell, int maxRadius, out int2 found)
        {
            if (IsWalkable(cell))
            {
                found = cell;
                return true;
            }

            for (var radius = 1; radius <= maxRadius; radius++)
            {
                var best = new int2(-1, -1);
                var bestDistance = float.MaxValue;
                for (var dz = -radius; dz <= radius; dz++)
                {
                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        if (math.max(math.abs(dx), math.abs(dz)) != radius)
                        {
                            continue;
                        }

                        var candidate = cell + new int2(dx, dz);
                        var distance = math.lengthsq(new float2(dx, dz));
                        if (IsWalkable(candidate) && distance < bestDistance)
                        {
                            best = candidate;
                            bestDistance = distance;
                        }
                    }
                }

                if (bestDistance < float.MaxValue)
                {
                    found = best;
                    return true;
                }
            }

            found = cell;
            return false;
        }

        /// <summary>
        /// Walks the cells a segment crosses (Amanatides–Woo) and checks every crossing. Passing
        /// exactly through a grid corner counts only if both ways round it are open.
        /// </summary>
        private bool Trace(float3 from, float3 to, bool sight)
        {
            var a = (from.xz - _layout.Origin.xz) / _layout.CellSize;
            var b = (to.xz - _layout.Origin.xz) / _layout.CellSize;
            var cell = (int2)math.floor(a);
            var end = (int2)math.floor(b);

            if (!sight && !IsWalkable(cell))
            {
                return false;
            }

            if (math.all(cell == end))
            {
                return true;
            }

            var delta = b - a;
            var step = new int2((int)math.sign(delta.x), (int)math.sign(delta.y));
            var tDelta = new float2(
                step.x != 0 ? math.abs(1f / delta.x) : float.MaxValue,
                step.y != 0 ? math.abs(1f / delta.y) : float.MaxValue);
            var tMax = new float2(
                step.x > 0 ? (cell.x + 1 - a.x) * tDelta.x : step.x < 0 ? (a.x - cell.x) * tDelta.x : float.MaxValue,
                step.y > 0 ? (cell.y + 1 - a.y) * tDelta.y : step.y < 0 ? (a.y - cell.y) * tDelta.y : float.MaxValue);

            var stepX = new int2(step.x, 0);
            var stepZ = new int2(0, step.y);
            var limit = math.abs(end.x - cell.x) + math.abs(end.y - cell.y) + 2;
            const float epsilon = 1e-5f;

            for (var i = 0; i < limit && !math.all(cell == end); i++)
            {
                if (tMax.x < tMax.y - epsilon)
                {
                    if (!Cross(cell, stepX, sight))
                    {
                        return false;
                    }

                    cell += stepX;
                    tMax.x += tDelta.x;
                }
                else if (tMax.y < tMax.x - epsilon)
                {
                    if (!Cross(cell, stepZ, sight))
                    {
                        return false;
                    }

                    cell += stepZ;
                    tMax.y += tDelta.y;
                }
                else
                {
                    var viaX = Cross(cell, stepX, sight) && Cross(cell + stepX, stepZ, sight);
                    var viaZ = Cross(cell, stepZ, sight) && Cross(cell + stepZ, stepX, sight);
                    if (!viaX || !viaZ)
                    {
                        return false;
                    }

                    cell += step;
                    tMax += tDelta;
                }
            }

            return math.all(cell == end);
        }

        private bool Cross(int2 cell, int2 step, bool sight)
        {
            return sight ? CanSee(cell, step) : CanStep(cell, step);
        }

        /// <summary>The four axis steps, for searches built on top of the grid.</summary>
        public static int2 Step(int index)
        {
            return Steps[index];
        }
    }
}

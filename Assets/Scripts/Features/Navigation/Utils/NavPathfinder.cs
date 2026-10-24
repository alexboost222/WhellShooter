using System.Collections.Generic;
using Unity.Mathematics;

namespace Features.Navigation.Utils
{
    /// <summary>
    /// A* over a <see cref="NavGrid"/>, then string-pulling: the result is a short list of corners a
    /// body can walk in straight lines, not a staircase of cell centres.
    /// </summary>
    /// <remarks>
    /// Eight-connected; a diagonal step is allowed only when both ways round it are open, so a path
    /// never squeezes between two blocked cells touching at a corner. Ties break on the cell index,
    /// so the same query always gives the same path.
    /// <para>
    /// One instance owns its search buffers and reuses them: a query allocates nothing once warm.
    /// Not thread-safe — one per storage.
    /// </para>
    /// </remarks>
    public sealed class NavPathfinder
    {
        private const float Diagonal = 1.41421356f;

        /// <summary>How far a start or goal off walkable ground is snapped back onto it, in cells.</summary>
        private const int SnapRadius = 3;

        private readonly NavGrid _grid;
        private readonly float[] _cost;
        private readonly int[] _parent;
        private readonly int[] _stamp;
        private readonly bool[] _closed;
        private readonly List<int> _heap = new List<int>();
        private readonly List<float> _heapScore = new List<float>();
        private readonly List<int> _cells = new List<int>();
        private int _search;

        public NavPathfinder(NavGrid grid)
        {
            _grid = grid;
            var size = grid.Width * grid.Depth;
            _cost = new float[size];
            _parent = new int[size];
            _stamp = new int[size];
            _closed = new bool[size];
        }

        /// <summary>
        /// Fills <paramref name="corners"/> with the way from <paramref name="from"/> to
        /// <paramref name="to"/>: the start, the turns, the goal. False (and empty) when the goal
        /// can't be reached.
        /// </summary>
        /// <remarks>
        /// A goal off walkable ground (inside cover, behind the border) is moved to the nearest
        /// walkable cell's centre; the start likewise, which is what gets a body unstuck after it was
        /// pushed into a corner.
        /// </remarks>
        public bool TryFindPath(float3 from, float3 to, float radius, List<float3> corners)
        {
            corners.Clear();

            if (!_grid.TryNearestWalkable(_grid.CellOf(from), SnapRadius, out var start) ||
                !_grid.TryNearestWalkable(_grid.CellOf(to), SnapRadius, out var goal))
            {
                return false;
            }

            if (!math.all(start == _grid.CellOf(from)))
            {
                from = _grid.CellCenter(start);
            }

            if (!math.all(goal == _grid.CellOf(to)))
            {
                to = _grid.CellCenter(goal);
            }

            if (!Search(start, goal))
            {
                return false;
            }

            Smooth(from, to, radius, corners);
            return true;
        }

        private bool Search(int2 start, int2 goal)
        {
            _search++;
            _heap.Clear();
            _heapScore.Clear();

            var startIndex = _grid.Index(start);
            var goalIndex = _grid.Index(goal);
            Open(startIndex, 0f, -1, goal);

            while (_heap.Count > 0)
            {
                var current = Pop();
                if (_closed[current])
                {
                    continue;
                }

                _closed[current] = true;
                if (current == goalIndex)
                {
                    Reconstruct(goalIndex);
                    return true;
                }

                var cell = _grid.CellAt(current);
                var cost = _cost[current];

                for (var i = 0; i < 4; i++)
                {
                    var step = NavGrid.Step(i);
                    if (_grid.CanStep(cell, step))
                    {
                        Relax(_grid.Index(cell + step), cost + 1f, current, goal);
                    }
                }

                for (var dz = -1; dz <= 1; dz += 2)
                {
                    for (var dx = -1; dx <= 1; dx += 2)
                    {
                        var x = new int2(dx, 0);
                        var z = new int2(0, dz);
                        if (_grid.CanStep(cell, x) && _grid.CanStep(cell + x, z) &&
                            _grid.CanStep(cell, z) && _grid.CanStep(cell + z, x))
                        {
                            Relax(_grid.Index(cell + x + z), cost + Diagonal, current, goal);
                        }
                    }
                }
            }

            return false;
        }

        private void Relax(int index, float cost, int parent, int2 goal)
        {
            if (_stamp[index] == _search && (_closed[index] || cost >= _cost[index]))
            {
                return;
            }

            Open(index, cost, parent, goal);
        }

        private void Open(int index, float cost, int parent, int2 goal)
        {
            if (_stamp[index] != _search)
            {
                _stamp[index] = _search;
                _closed[index] = false;
            }

            _cost[index] = cost;
            _parent[index] = parent;
            Push(index, cost + Heuristic(_grid.CellAt(index), goal));
        }

        /// <summary>Octile distance — exact on an open eight-connected grid, so A* stays optimal.</summary>
        private static float Heuristic(int2 cell, int2 goal)
        {
            var d = math.abs(goal - cell);
            return math.max(d.x, d.y) + (Diagonal - 1f) * math.min(d.x, d.y);
        }

        private void Reconstruct(int goalIndex)
        {
            _cells.Clear();
            for (var index = goalIndex; index >= 0; index = _parent[index])
            {
                _cells.Add(index);
            }

            _cells.Reverse();
        }

        /// <summary>
        /// String-pulling: from each corner, run to the farthest path point still reachable in a
        /// straight line, and turn there.
        /// </summary>
        private void Smooth(float3 from, float3 to, float radius, List<float3> corners)
        {
            corners.Add(from);
            var anchor = from;
            var last = _cells.Count - 1;

            for (var i = 1; i <= last; i++)
            {
                var next = i == last ? to : _grid.CellCenter(_grid.CellAt(_cells[i]));
                if (_grid.CanWalkStraight(anchor, next, radius))
                {
                    continue;
                }

                // i - 1 was the last point reachable from the anchor: turn there. When that is the
                // anchor itself (an off-centre start whose flank clips a wall), follow the grid one
                // cell instead — neighbouring centres on the path always connect.
                var turn = i - 1 == 0 ? from : _grid.CellCenter(_grid.CellAt(_cells[i - 1]));
                if (math.all(turn == anchor))
                {
                    turn = next;
                }

                corners.Add(turn);
                anchor = turn;
            }

            if (corners.Count == 1 || !math.all(corners[corners.Count - 1] == to))
            {
                corners.Add(to);
            }
        }

        // ---------------------------------------------------------------- binary heap

        private void Push(int index, float score)
        {
            _heap.Add(index);
            _heapScore.Add(score);
            var child = _heap.Count - 1;
            while (child > 0)
            {
                var parent = (child - 1) / 2;
                if (!Less(child, parent))
                {
                    break;
                }

                Swap(child, parent);
                child = parent;
            }
        }

        private int Pop()
        {
            var top = _heap[0];
            var lastIndex = _heap.Count - 1;
            Swap(0, lastIndex);
            _heap.RemoveAt(lastIndex);
            _heapScore.RemoveAt(lastIndex);

            var parent = 0;
            while (true)
            {
                var left = parent * 2 + 1;
                var right = left + 1;
                var smallest = parent;
                if (left < _heap.Count && Less(left, smallest))
                {
                    smallest = left;
                }

                if (right < _heap.Count && Less(right, smallest))
                {
                    smallest = right;
                }

                if (smallest == parent)
                {
                    break;
                }

                Swap(parent, smallest);
                parent = smallest;
            }

            return top;
        }

        private bool Less(int a, int b)
        {
            if (_heapScore[a] != _heapScore[b])
            {
                return _heapScore[a] < _heapScore[b];
            }

            return _heap[a] < _heap[b];
        }

        private void Swap(int a, int b)
        {
            (_heap[a], _heap[b]) = (_heap[b], _heap[a]);
            (_heapScore[a], _heapScore[b]) = (_heapScore[b], _heapScore[a]);
        }
    }
}

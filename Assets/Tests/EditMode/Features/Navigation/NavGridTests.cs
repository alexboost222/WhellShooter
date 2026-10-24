using System.Collections.Generic;
using Features.Map.Enums;
using Features.Map.Utils;
using Features.Navigation.Utils;
using NUnit.Framework;
using Unity.Mathematics;

namespace Game.Tests.EditMode.Features.Navigation
{
    /// <summary>
    /// The grid's two questions — can I walk there, can I see there — and the pathfinder on top,
    /// against hand-built layouts small enough to reason about, then against generated arenas.
    /// </summary>
    public sealed class NavGridTests
    {
        private const float Radius = 0.35f;

        [Test]
        public void OpenGround_PathIsOneStraightLine()
        {
            var layout = OpenLayout(12, 12);
            var grid = new NavGrid(layout);
            var corners = new List<float3>();

            var from = layout.CellCenter(new int2(1, 1));
            var to = layout.CellCenter(new int2(10, 7));
            Assert.That(new NavPathfinder(grid).TryFindPath(from, to, Radius, corners), Is.True);

            Assert.That(corners.Count, Is.EqualTo(2));
            Assert.That(corners[0], Is.EqualTo(from));
            Assert.That(corners[1], Is.EqualTo(to));
        }

        [Test]
        public void WallWithADoor_PathGoesThroughTheDoor()
        {
            var layout = OpenLayout(12, 12);
            // A wall across the whole map between rows 5 and 6, with one door at x = 9.
            for (var x = 0; x < 12; x++)
            {
                layout.SetEdge(new int2(x, 5), new int2(0, 1), x == 9 ? MapEdgeKind.Door : MapEdgeKind.Wall);
            }

            var grid = new NavGrid(layout);
            var corners = new List<float3>();
            var from = layout.CellCenter(new int2(1, 1));
            var to = layout.CellCenter(new int2(1, 10));

            Assert.That(new NavPathfinder(grid).TryFindPath(from, to, Radius, corners), Is.True);
            AssertWalkable(grid, corners);

            var door = layout.CellCenter(new int2(9, 5));
            var nearDoor = false;
            foreach (var corner in corners)
            {
                nearDoor |= math.distance(corner.xz, door.xz) < layout.CellSize * 1.5f;
            }

            Assert.That(nearDoor, Is.True, "the path should turn at the door");
        }

        [Test]
        public void SealedOffGoal_IsUnreachable()
        {
            var layout = OpenLayout(12, 12);
            for (var x = 0; x < 12; x++)
            {
                layout.SetEdge(new int2(x, 5), new int2(0, 1), MapEdgeKind.Wall);
            }

            var corners = new List<float3>();
            var found = new NavPathfinder(new NavGrid(layout)).TryFindPath(
                layout.CellCenter(new int2(1, 1)), layout.CellCenter(new int2(1, 10)), Radius, corners);

            Assert.That(found, Is.False);
            Assert.That(corners, Is.Empty);
        }

        [Test]
        public void TwoBlockedCellsTouchingAtACorner_AreNotSqueezedThrough()
        {
            var layout = OpenLayout(12, 12);
            layout.SetCell(new int2(5, 6), MapCellKind.Blocked);
            layout.SetCell(new int2(6, 5), MapCellKind.Blocked);
            var grid = new NavGrid(layout);

            Assert.That(grid.CanWalkStraight(layout.CellCenter(new int2(5, 5)), layout.CellCenter(new int2(6, 6)), 0f),
                Is.False);

            var corners = new List<float3>();
            Assert.That(new NavPathfinder(grid).TryFindPath(
                layout.CellCenter(new int2(5, 5)), layout.CellCenter(new int2(6, 6)), Radius, corners), Is.True);
            Assert.That(corners.Count, Is.GreaterThan(2), "must go round, not through the pinch");
            AssertWalkable(grid, corners);
        }

        [Test]
        public void GoalInsideCover_SnapsToNearestWalkableCell()
        {
            var layout = OpenLayout(12, 12);
            layout.SetCell(new int2(6, 6), MapCellKind.Blocked);
            var corners = new List<float3>();

            Assert.That(new NavPathfinder(new NavGrid(layout)).TryFindPath(
                layout.CellCenter(new int2(1, 1)), layout.CellCenter(new int2(6, 6)), Radius, corners), Is.True);

            var end = corners[corners.Count - 1];
            Assert.That(new NavGrid(layout).IsWalkable(new NavGrid(layout).CellOf(end)), Is.True);
        }

        [Test]
        public void Sight_WallBlocks_WindowAndDoorDoNot()
        {
            var layout = OpenLayout(12, 12);
            layout.SetEdge(new int2(3, 5), new int2(0, 1), MapEdgeKind.Wall);
            layout.SetEdge(new int2(5, 5), new int2(0, 1), MapEdgeKind.Window);
            layout.SetEdge(new int2(7, 5), new int2(0, 1), MapEdgeKind.Door);
            var grid = new NavGrid(layout);

            Assert.That(grid.HasLineOfSight(layout.CellCenter(new int2(3, 4)), layout.CellCenter(new int2(3, 7))), Is.False);
            Assert.That(grid.HasLineOfSight(layout.CellCenter(new int2(5, 4)), layout.CellCenter(new int2(5, 7))), Is.True);
            Assert.That(grid.HasLineOfSight(layout.CellCenter(new int2(7, 4)), layout.CellCenter(new int2(7, 7))), Is.True);

            // The window is see-through but not walk-through.
            Assert.That(grid.CanWalkStraight(layout.CellCenter(new int2(5, 4)), layout.CellCenter(new int2(5, 7)), 0f),
                Is.False);
        }

        [Test]
        public void Sight_HighCoverBlocks_LowCoverDoesNot()
        {
            var layout = OpenLayout(12, 12);
            layout.SetCell(new int2(4, 6), MapCellKind.Blocked);
            layout.Pieces.Add(new MapPiece { Kind = MapPieceKind.CoverHigh, Position = layout.CellCenter(new int2(4, 6)) });
            layout.SetCell(new int2(8, 6), MapCellKind.Blocked);
            layout.Pieces.Add(new MapPiece { Kind = MapPieceKind.CoverLow, Position = layout.CellCenter(new int2(8, 6)) });
            var grid = new NavGrid(layout);

            Assert.That(grid.HasLineOfSight(layout.CellCenter(new int2(4, 3)), layout.CellCenter(new int2(4, 9))), Is.False);
            Assert.That(grid.HasLineOfSight(layout.CellCenter(new int2(8, 3)), layout.CellCenter(new int2(8, 9))), Is.True);
        }

        [Test]
        public void Radius_KeepsThePathOffBlockedCorners()
        {
            var layout = OpenLayout(12, 12);
            layout.SetCell(new int2(5, 5), MapCellKind.Blocked);
            var grid = new NavGrid(layout);

            // A line grazing the blocked cell's corner: fine for a point, not for a body.
            var cell = layout.CellSize;
            var from = layout.Corner(5, 5) + new float3(-0.2f, 0f, -cell * 2f);
            var to = layout.Corner(5, 5) + new float3(-0.2f, 0f, cell * 2f);
            Assert.That(grid.CanWalkStraight(from, to, 0f), Is.True);
            Assert.That(grid.CanWalkStraight(from, to, Radius), Is.False);
        }

        [Test]
        public void GeneratedArenas_EveryPathIsWalkable_AndDeterministic()
        {
            for (var seed = 1; seed <= 12; seed++)
            {
                var layout = MapGenerator.Generate(seed, MapSettings.Default);
                var grid = new NavGrid(layout);
                var pathfinder = new NavPathfinder(grid);
                var random = new Random((uint)seed);
                var corners = new List<float3>();
                var again = new List<float3>();

                for (var i = 0; i < 30; i++)
                {
                    var cells = grid.WalkableCells;
                    var from = grid.CellCenter(cells[random.NextInt(0, cells.Count)]);
                    var to = grid.CellCenter(cells[random.NextInt(0, cells.Count)]);

                    // The generator guarantees connectivity, so every query must succeed.
                    Assert.That(pathfinder.TryFindPath(from, to, Radius, corners), Is.True, $"seed {seed}: {from} → {to}");
                    AssertWalkable(grid, corners);

                    pathfinder.TryFindPath(from, to, Radius, again);
                    Assert.That(again, Is.EqualTo(corners), $"seed {seed}: same query, different path");
                }
            }
        }

        private static void AssertWalkable(NavGrid grid, List<float3> corners)
        {
            for (var i = 1; i < corners.Count; i++)
            {
                Assert.That(grid.CanWalkStraight(corners[i - 1], corners[i], 0f), Is.True,
                    $"segment {corners[i - 1]} → {corners[i]} crosses something");
            }
        }

        private static MapLayout OpenLayout(int width, int depth)
        {
            var layout = new MapLayout(1, width, depth, 2.5f);
            for (var x = 0; x < width; x++)
            {
                layout.SetEdgeX(x, 0, MapEdgeKind.Border);
                layout.SetEdgeX(x, depth, MapEdgeKind.Border);
            }

            for (var z = 0; z < depth; z++)
            {
                layout.SetEdgeZ(0, z, MapEdgeKind.Border);
                layout.SetEdgeZ(width, z, MapEdgeKind.Border);
            }

            return layout;
        }
    }
}

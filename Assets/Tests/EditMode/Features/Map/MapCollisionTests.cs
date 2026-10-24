using System.Collections.Generic;
using Features.Map.Constants;
using Features.Map.Enums;
using Features.Map.Utils;
using NUnit.Framework;
using Unity.Mathematics;

namespace Game.Tests.EditMode.Features.Map
{
    /// <summary>MapCollision as pure data: layout in, boxes out (Docs/11).</summary>
    public sealed class MapCollisionTests
    {
        private static readonly int[] Seeds = { 1, 7, 42, 1234 };

        [TestCaseSource(nameof(Seeds))]
        public void SameLayout_SameBoxes(int seed)
        {
            var a = Build(seed, out _);
            var b = Build(seed, out _);

            Assert.That(b.Count, Is.EqualTo(a.Count));
            for (var i = 0; i < a.Count; i++)
            {
                Assert.That(b[i].Center, Is.EqualTo(a[i].Center));
                Assert.That(b[i].HalfExtents, Is.EqualTo(a[i].HalfExtents));
                Assert.That(b[i].Yaw, Is.EqualTo(a[i].Yaw));
            }
        }

        [TestCaseSource(nameof(Seeds))]
        public void TheGroundIsUnderEveryCell_WithItsTopAtZero(int seed)
        {
            var boxes = Build(seed, out var layout);
            var ground = boxes[0];

            Assert.That(ground.Center.y + ground.HalfExtents.y, Is.EqualTo(0f).Within(1e-5f));
            for (var z = 0; z < layout.Depth; z++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    var below = layout.CellCenter(new int2(x, z)) - new float3(0f, 0.1f, 0f);
                    Assert.That(Contains(ground, below), Is.True, $"cell {x},{z}");
                }
            }
        }

        [TestCaseSource(nameof(Seeds))]
        public void Doors_LeaveTheirOpeningFree_AndWallsAroundThemSolid(int seed)
        {
            var boxes = Build(seed, out var layout);
            var doors = 0;
            foreach (var piece in layout.Pieces)
            {
                if (piece.Kind != MapPieceKind.WallDoor)
                {
                    continue;
                }

                doors++;
                Assert.That(Solid(boxes, piece.Position + new float3(0f, 1f, 0f)), Is.False, $"door {piece}");
                Assert.That(Solid(boxes, piece.Position + new float3(0f, 2f, 0f)), Is.False, $"door {piece}");
                Assert.That(Solid(boxes, piece.Position + new float3(0f, 2.6f, 0f)), Is.True, $"lintel {piece}");
                Assert.That(Solid(boxes, piece.Position + Along(piece, 1f) + new float3(0f, 1f, 0f)), Is.True,
                    $"post {piece}");
            }

            Assert.That(doors, Is.GreaterThan(0));
        }

        [TestCaseSource(nameof(Seeds))]
        public void Windows_AreSolidBelowTheSill_AndOpenAboveIt(int seed)
        {
            var boxes = Build(seed, out var layout);
            foreach (var piece in layout.Pieces)
            {
                if (piece.Kind != MapPieceKind.WallWindow)
                {
                    continue;
                }

                Assert.That(Solid(boxes, piece.Position + new float3(0f, 0.5f, 0f)), Is.True, $"sill {piece}");
                Assert.That(Solid(boxes, piece.Position + new float3(0f, 1.6f, 0f)), Is.False, $"window {piece}");
            }
        }

        [TestCaseSource(nameof(Seeds))]
        public void PlainWalls_AreSolidFullHeight(int seed)
        {
            var boxes = Build(seed, out var layout);
            foreach (var piece in layout.Pieces)
            {
                if (piece.Kind != MapPieceKind.Wall)
                {
                    continue;
                }

                Assert.That(Solid(boxes, piece.Position + new float3(0f, 0.3f, 0f)), Is.True, $"{piece}");
                Assert.That(Solid(boxes, piece.Position + new float3(0f, 2.8f, 0f)), Is.True, $"{piece}");
            }
        }

        [TestCaseSource(nameof(Seeds))]
        public void Stairs_ClimbInSteps_ACharacterCanStepUp(int seed)
        {
            var boxes = Build(seed, out var layout);
            var settings = MapSettings.Default;
            foreach (var piece in layout.Pieces)
            {
                if (piece.Kind != MapPieceKind.Stairs)
                {
                    continue;
                }

                var previous = 0f;
                var depth = layout.CellSize / MapCollisionSizes.StairSteps;
                for (var i = 0; i < MapCollisionSizes.StairSteps; i++)
                {
                    // Walk the stairs' centre line from the bottom: the solid top under each tread.
                    var forward = -layout.CellSize * 0.5f + (i + 0.5f) * depth;
                    var top = TopAt(boxes, piece.Position + Forward(piece, forward));
                    Assert.That(top - previous, Is.LessThanOrEqualTo(0.35f), $"step {i} of {piece}");
                    previous = top;
                }

                Assert.That(previous, Is.EqualTo(settings.StoreyHeight).Within(1e-4f), "the last step is the roof");
            }
        }

        // --- helpers --------------------------------------------------------------------------

        private static List<MapCollisionBox> Build(int seed, out MapLayout layout)
        {
            layout = MapGenerator.Generate(seed, MapSettings.Default);
            return MapCollision.Build(layout, MapSettings.Default);
        }

        private static float3 Along(MapPiece piece, float distance)
        {
            return math.mul(quaternion.RotateY(math.radians(piece.Yaw)), new float3(distance, 0f, 0f));
        }

        private static float3 Forward(MapPiece piece, float distance)
        {
            return math.mul(quaternion.RotateY(math.radians(piece.Yaw)), new float3(0f, 0f, distance));
        }

        private static bool Solid(List<MapCollisionBox> boxes, float3 point)
        {
            for (var i = 0; i < boxes.Count; i++)
            {
                if (Contains(boxes[i], point))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Highest box top at <paramref name="point"/>'s x/z, scanning down from 10 m.</summary>
        private static float TopAt(List<MapCollisionBox> boxes, float3 point)
        {
            var top = float.MinValue;
            for (var i = 0; i < boxes.Count; i++)
            {
                var probe = new float3(point.x, boxes[i].Center.y, point.z);
                if (Contains(boxes[i], probe))
                {
                    top = math.max(top, boxes[i].Center.y + boxes[i].HalfExtents.y);
                }
            }

            return top;
        }

        private static bool Contains(MapCollisionBox box, float3 point)
        {
            var local = math.mul(math.inverse(quaternion.RotateY(box.Yaw)), point - box.Center);
            return math.all(math.abs(local) <= box.HalfExtents + 1e-4f);
        }
    }
}

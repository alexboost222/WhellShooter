using System;
using System.Collections.Generic;
using System.Linq;
using Features.Map.Enums;
using Features.Map.Utils;
using NUnit.Framework;
using Unity.Mathematics;

namespace Game.Tests.EditMode.Features.Map
{
    /// <summary>The generator's guarantees from Docs/08, checked over many seeds.</summary>
    public sealed class MapGeneratorTests
    {
        private static readonly int[] Seeds = Enumerable.Range(1, 40).Concat(new[] { 0, -7, int.MaxValue, 12345 }).ToArray();

        private static readonly int2[] Steps =
        {
            new int2(1, 0),
            new int2(-1, 0),
            new int2(0, 1),
            new int2(0, -1),
        };

        [Test]
        public void SameSeed_GivesTheSameLayout()
        {
            var a = MapGenerator.Generate(42, MapSettings.Default);
            var b = MapGenerator.Generate(42, MapSettings.Default);

            Assert.That(b.Pieces.Count, Is.EqualTo(a.Pieces.Count));
            for (var i = 0; i < a.Pieces.Count; i++)
            {
                Assert.That(b.Pieces[i].Kind, Is.EqualTo(a.Pieces[i].Kind), $"piece {i}");
                Assert.That(b.Pieces[i].Variant, Is.EqualTo(a.Pieces[i].Variant), $"piece {i}");
                Assert.That(b.Pieces[i].Position, Is.EqualTo(a.Pieces[i].Position), $"piece {i}");
                Assert.That(b.Pieces[i].Yaw, Is.EqualTo(a.Pieces[i].Yaw), $"piece {i}");
            }

            Assert.That(b.Spawns.Select(s => s.Cell), Is.EqualTo(a.Spawns.Select(s => s.Cell)));
        }

        [Test]
        public void DifferentSeeds_GiveDifferentLayouts()
        {
            var a = MapGenerator.Generate(1, MapSettings.Default);
            var b = MapGenerator.Generate(2, MapSettings.Default);

            var sameBuildings = a.Buildings.Count == b.Buildings.Count &&
                                a.Buildings.Zip(b.Buildings, (x, y) => x.Min.Equals(y.Min) && x.Size.Equals(y.Size)).All(same => same);
            Assert.That(sameBuildings, Is.False);
        }

        [TestCaseSource(nameof(Seeds))]
        public void EveryWalkableCell_IsReachable(int seed)
        {
            var layout = MapGenerator.Generate(seed, MapSettings.Default);

            Assert.That(MapGenerator.IsConnected(layout), Is.True);
        }

        [TestCaseSource(nameof(Seeds))]
        public void BuildingCount_IsWithinSettings(int seed)
        {
            var settings = MapSettings.Default;
            var layout = MapGenerator.Generate(seed, settings);

            Assert.That(layout.Buildings.Count, Is.InRange(settings.MinBuildings, settings.MaxBuildings));
        }

        [TestCaseSource(nameof(Seeds))]
        public void EveryBuilding_HasDoorsOnAtLeastTwoSides(int seed)
        {
            var layout = MapGenerator.Generate(seed, MapSettings.Default);

            foreach (var building in layout.Buildings)
            {
                var sides = new HashSet<int2>();
                ForEachPerimeterSegment(building, (inside, outward) =>
                {
                    if (layout.GetEdge(inside, outward) == MapEdgeKind.Door)
                    {
                        sides.Add(outward);
                    }
                });

                Assert.That(sides.Count, Is.GreaterThanOrEqualTo(2), $"building at {building.Min}");
            }
        }

        [TestCaseSource(nameof(Seeds))]
        public void BothSidesOfEveryDoor_AreWalkable(int seed)
        {
            var layout = MapGenerator.Generate(seed, MapSettings.Default);

            foreach (var building in layout.Buildings)
            {
                ForEachPerimeterSegment(building, (inside, outward) =>
                {
                    if (layout.GetEdge(inside, outward) != MapEdgeKind.Door)
                    {
                        return;
                    }

                    Assert.That(layout.GetCell(inside), Is.EqualTo(MapCellKind.Interior), $"inside door at {inside}");
                    Assert.That(layout.GetCell(inside + outward), Is.EqualTo(MapCellKind.Open), $"outside door at {inside}");
                });
            }
        }

        [TestCaseSource(nameof(Seeds))]
        public void Buildings_KeepTheirGap_AndStayOutOfSpawnZones(int seed)
        {
            var settings = MapSettings.Default;
            var layout = MapGenerator.Generate(seed, settings);

            for (var i = 0; i < layout.Buildings.Count; i++)
            {
                var a = layout.Buildings[i];
                Assert.That(a.Min.y, Is.GreaterThan(settings.SpawnZoneDepth));
                Assert.That(a.Max.y, Is.LessThan(settings.Depth - settings.SpawnZoneDepth - 1));
                Assert.That(a.Min.x, Is.GreaterThanOrEqualTo(settings.BuildingGap));
                Assert.That(a.Max.x, Is.LessThan(settings.Width - settings.BuildingGap));

                for (var j = i + 1; j < layout.Buildings.Count; j++)
                {
                    var b = layout.Buildings[j];
                    var apartX = b.Min.x - a.Max.x - 1 >= settings.BuildingGap || a.Min.x - b.Max.x - 1 >= settings.BuildingGap;
                    var apartZ = b.Min.y - a.Max.y - 1 >= settings.BuildingGap || a.Min.y - b.Max.y - 1 >= settings.BuildingGap;
                    Assert.That(apartX || apartZ, Is.True, $"buildings {a.Min} and {b.Min} are too close");
                }
            }
        }

        [TestCaseSource(nameof(Seeds))]
        public void Spawns_AreOnWalkableCells(int seed)
        {
            var layout = MapGenerator.Generate(seed, MapSettings.Default);

            foreach (var spawn in layout.Spawns)
            {
                Assert.That(layout.InBounds(spawn.Cell), Is.True, $"spawn {spawn.Cell}");
                Assert.That(MapLayout.IsWalkable(layout.GetCell(spawn.Cell)), Is.True, $"spawn {spawn.Cell}");
                Assert.That(spawn.Position, Is.EqualTo(layout.CellCenter(spawn.Cell)));
            }
        }

        [TestCaseSource(nameof(Seeds))]
        public void TeamSpawns_SitInTheirOwnZone(int seed)
        {
            var settings = MapSettings.Default;
            var layout = MapGenerator.Generate(seed, settings);

            var team0 = layout.Spawns.Where(s => s.Team == 0).ToList();
            var team1 = layout.Spawns.Where(s => s.Team == 1).ToList();
            var free = layout.Spawns.Where(s => s.Team == -1).ToList();

            Assert.That(team0.Count, Is.EqualTo(settings.TeamSpawns));
            Assert.That(team1.Count, Is.EqualTo(settings.TeamSpawns));
            Assert.That(free.Count, Is.EqualTo(settings.FreeSpawns));
            Assert.That(team0.All(s => s.Cell.y < settings.SpawnZoneDepth), Is.True);
            Assert.That(team1.All(s => s.Cell.y >= settings.Depth - settings.SpawnZoneDepth), Is.True);
            Assert.That(free.All(s => s.Cell.y >= settings.SpawnZoneDepth && s.Cell.y < settings.Depth - settings.SpawnZoneDepth),
                Is.True);
        }

        [TestCaseSource(nameof(Seeds))]
        public void FreeSpawns_AreSpreadOut(int seed)
        {
            var layout = MapGenerator.Generate(seed, MapSettings.Default);
            var free = layout.Spawns.Where(s => s.Team == -1).ToList();

            for (var i = 0; i < free.Count; i++)
            {
                for (var j = i + 1; j < free.Count; j++)
                {
                    Assert.That(math.distance(free[i].Cell, free[j].Cell), Is.GreaterThanOrEqualTo(3f),
                        $"free spawns {free[i].Cell} and {free[j].Cell}");
                }
            }
        }

        [TestCaseSource(nameof(Seeds))]
        public void OutdoorCover_NeverTouchesOtherCover(int seed)
        {
            var layout = MapGenerator.Generate(seed, MapSettings.Default);

            for (var z = 0; z < layout.Depth; z++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    var cell = new int2(x, z);
                    if (layout.GetCell(cell) != MapCellKind.Blocked || layout.GetBuilding(cell) != MapLayout.NoBuilding)
                    {
                        continue;
                    }

                    for (var dz = -1; dz <= 1; dz++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            var next = cell + new int2(dx, dz);
                            if ((dx == 0 && dz == 0) || !layout.InBounds(next) || layout.GetBuilding(next) != MapLayout.NoBuilding)
                            {
                                continue;
                            }

                            Assert.That(layout.GetCell(next), Is.Not.EqualTo(MapCellKind.Blocked), $"cover at {cell} touches {next}");
                        }
                    }
                }
            }
        }

        [TestCaseSource(nameof(Seeds))]
        public void RoofStairs_LeanOnAWall_AndCanBeReached(int seed)
        {
            var layout = MapGenerator.Generate(seed, MapSettings.Default);

            foreach (var building in layout.Buildings.Where(b => b.HasRoofAccess))
            {
                var stairs = building.StairsCell;
                var top = stairs + building.StairsDirection;
                var approach = stairs - building.StairsDirection;

                Assert.That(layout.GetCell(stairs), Is.EqualTo(MapCellKind.Stairs));
                Assert.That(building.Contains(top), Is.True, "the stairs climb into the building's roof");
                Assert.That(layout.GetEdge(stairs, building.StairsDirection), Is.EqualTo(MapEdgeKind.Wall));
                Assert.That(layout.GetCell(approach), Is.EqualTo(MapCellKind.Open), "the foot of the stairs is free");
            }
        }

        [TestCaseSource(nameof(Seeds))]
        public void ArenaPieces_StayInsideTheBorder(int seed)
        {
            var layout = MapGenerator.Generate(seed, MapSettings.Default);
            var min = layout.Origin;
            var max = layout.Corner(layout.Width, layout.Depth);
            var outside = new[] { MapPieceKind.Tree, MapPieceKind.Rock, MapPieceKind.Bush, MapPieceKind.Backdrop };

            foreach (var piece in layout.Pieces.Where(p => !outside.Contains(p.Kind)))
            {
                Assert.That(piece.Position.x, Is.InRange(min.x, max.x), piece.ToString());
                Assert.That(piece.Position.z, Is.InRange(min.z, max.z), piece.ToString());
            }
        }

        [Test]
        public void TheBorder_IsClosed()
        {
            var layout = MapGenerator.Generate(3, MapSettings.Default);

            for (var x = 0; x < layout.Width; x++)
            {
                Assert.That(layout.GetEdgeX(x, 0), Is.EqualTo(MapEdgeKind.Border));
                Assert.That(layout.GetEdgeX(x, layout.Depth), Is.EqualTo(MapEdgeKind.Border));
            }

            for (var z = 0; z < layout.Depth; z++)
            {
                Assert.That(layout.GetEdgeZ(0, z), Is.EqualTo(MapEdgeKind.Border));
                Assert.That(layout.GetEdgeZ(layout.Width, z), Is.EqualTo(MapEdgeKind.Border));
            }
        }

        [Test]
        public void IsConnected_SeesAWallThatCutsTheMapInTwo()
        {
            var layout = new MapLayout(0, 12, 12, 2.5f);
            Assert.That(MapGenerator.IsConnected(layout), Is.True);

            for (var x = 0; x < layout.Width; x++)
            {
                layout.SetEdgeX(x, 6, MapEdgeKind.Wall);
            }

            Assert.That(MapGenerator.IsConnected(layout), Is.False);

            layout.SetEdgeX(4, 6, MapEdgeKind.Door);
            Assert.That(MapGenerator.IsConnected(layout), Is.True);
        }

        [Test]
        public void Generate_RejectsSettingsItCannotHonour()
        {
            var settings = MapSettings.Default;
            settings.Width = 4;
            Assert.Throws<ArgumentException>(() => MapGenerator.Generate(1, settings));

            settings = MapSettings.Default;
            settings.MaxBuildingSize = 1;
            Assert.Throws<ArgumentException>(() => MapGenerator.Generate(1, settings));
        }

        [Test]
        public void CustomSizes_StillHoldTheGuarantees()
        {
            var settings = MapSettings.Default;
            settings.Width = 40;
            settings.Depth = 20;
            settings.MaxBuildings = 10;

            for (var seed = 1; seed <= 10; seed++)
            {
                var layout = MapGenerator.Generate(seed, settings);
                Assert.That(MapGenerator.IsConnected(layout), Is.True, $"seed {seed}");
                Assert.That(layout.Width, Is.EqualTo(40));
            }
        }

        private static void ForEachPerimeterSegment(MapBuilding building, Action<int2, int2> visit)
        {
            for (var z = building.Min.y; z <= building.Max.y; z++)
            {
                for (var x = building.Min.x; x <= building.Max.x; x++)
                {
                    var cell = new int2(x, z);
                    foreach (var step in Steps)
                    {
                        if (!building.Contains(cell + step))
                        {
                            visit(cell, step);
                        }
                    }
                }
            }
        }
    }
}

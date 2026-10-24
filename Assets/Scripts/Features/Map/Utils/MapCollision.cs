using System.Collections.Generic;
using Features.Map.Enums;
using Unity.Mathematics;
using static Features.Map.Constants.MapCollisionSizes;

namespace Features.Map.Utils
{
    /// <summary>
    /// The arena as static boxes for phys-r (Docs/11): <see cref="MapLayout"/> in, boxes out. Pure C#,
    /// deterministic, no Unity objects — built from the layout and nominal piece sizes
    /// (<see cref="Constants.MapCollisionSizes"/>), never from meshes.
    /// </summary>
    /// <remarks>
    /// What collides: the ground slab, walls (doors and windows leave their openings), border walls,
    /// pillars, one roof slab per building, parapets, stairs as steps, and cover / interior props.
    /// Interior floors (2 cm above the ground), ceilings and the outskirts' scenery don't: they are
    /// either flush with something solid or out of reach behind the border wall.
    /// </remarks>
    public static class MapCollision
    {
        public static List<MapCollisionBox> Build(MapLayout layout, MapSettings settings)
        {
            var boxes = new List<MapCollisionBox>();
            var storey = settings.StoreyHeight;
            var cell = layout.CellSize;

            AddGround(boxes, layout, settings);

            foreach (var building in layout.Buildings)
            {
                // One slab per building instead of one per roof cell: fewer bodies, no seams to trip on.
                var min = layout.Corner(building.Min.x, building.Min.y);
                var max = layout.Corner(building.Max.x + 1, building.Max.y + 1);
                var center = (min + max) * 0.5f;
                boxes.Add(new MapCollisionBox(
                    new float3(center.x, storey - RoofThickness * 0.5f, center.z),
                    new float3((max.x - min.x) * 0.5f, RoofThickness * 0.5f, (max.z - min.z) * 0.5f), 0f));
            }

            foreach (var piece in layout.Pieces)
            {
                var yaw = math.radians(piece.Yaw);
                switch (piece.Kind)
                {
                    case MapPieceKind.Wall:
                    case MapPieceKind.BorderWall:
                        Slab(boxes, piece.Position, yaw, 0f, cell, 0f, storey);
                        break;
                    case MapPieceKind.WallDoor:
                        Opening(boxes, piece.Position, yaw, cell, storey, DoorWidth, 0f, DoorHeight);
                        break;
                    case MapPieceKind.WallWindow:
                        Opening(boxes, piece.Position, yaw, cell, storey, WindowWidth, WindowSill, WindowTop);
                        break;
                    case MapPieceKind.Parapet:
                        Slab(boxes, piece.Position, yaw, 0f, cell, 0f, ParapetHeight);
                        break;
                    case MapPieceKind.CoverWall:
                        Slab(boxes, piece.Position, yaw, 0f, cell, 0f, CoverWallHeight);
                        break;
                    case MapPieceKind.Pillar:
                        Block(boxes, piece.Position, yaw, new float3(PillarWidth, storey, PillarWidth));
                        break;
                    case MapPieceKind.CoverLow:
                        Block(boxes, piece.Position, yaw, new float3(CoverLowSize));
                        break;
                    case MapPieceKind.CoverHigh:
                        Block(boxes, piece.Position, yaw, new float3(CoverHighSize));
                        break;
                    case MapPieceKind.InteriorProp:
                        Block(boxes, piece.Position, yaw, new float3(InteriorPropSize));
                        break;
                    case MapPieceKind.Stairs:
                        Stairs(boxes, piece.Position, yaw, cell, storey);
                        break;
                }
            }

            return boxes;
        }

        private static void AddGround(List<MapCollisionBox> boxes, MapLayout layout, MapSettings settings)
        {
            // Same extent as the view's ground plane (MapBuilder), out past the outskirts.
            var margin = layout.CellSize * (settings.OutskirtsRings + 2);
            var half = new float3(layout.Width * layout.CellSize * 0.5f + margin, GroundThickness * 0.5f,
                layout.Depth * layout.CellSize * 0.5f + margin);
            var center = layout.Origin + new float3(layout.Width * layout.CellSize * 0.5f, -GroundThickness * 0.5f,
                layout.Depth * layout.CellSize * 0.5f);
            boxes.Add(new MapCollisionBox(center, half, 0f));
        }

        /// <summary>
        /// A wall-like slab in the piece's frame: local X along the wall, centred at
        /// <paramref name="offset"/>, <paramref name="width"/> wide, from <paramref name="bottom"/> to
        /// <paramref name="top"/> above the piece's base.
        /// </summary>
        private static void Slab(List<MapCollisionBox> boxes, float3 position, float yaw, float offset, float width,
            float bottom, float top)
        {
            if (width <= 0f || top <= bottom)
            {
                return;
            }

            var along = math.mul(quaternion.RotateY(yaw), new float3(1f, 0f, 0f));
            var center = position + along * offset + new float3(0f, (bottom + top) * 0.5f, 0f);
            boxes.Add(new MapCollisionBox(center,
                new float3(width * 0.5f, (top - bottom) * 0.5f, WallThickness * 0.5f), yaw));
        }

        /// <summary>A wall with a centred opening from <paramref name="sill"/> to <paramref name="top"/>.</summary>
        private static void Opening(List<MapCollisionBox> boxes, float3 position, float yaw, float length,
            float height, float openingWidth, float sill, float top)
        {
            var side = (length - openingWidth) * 0.5f;
            var sideOffset = (openingWidth + side) * 0.5f;

            Slab(boxes, position, yaw, -sideOffset, side, 0f, height);
            Slab(boxes, position, yaw, sideOffset, side, 0f, height);
            Slab(boxes, position, yaw, 0f, openingWidth, 0f, sill);
            Slab(boxes, position, yaw, 0f, openingWidth, top, height);
        }

        /// <summary>A block standing on the piece's base, footprint centred on its position.</summary>
        private static void Block(List<MapCollisionBox> boxes, float3 position, float yaw, float3 size)
        {
            boxes.Add(new MapCollisionBox(position + new float3(0f, size.y * 0.5f, 0f), size * 0.5f, yaw));
        }

        /// <summary>
        /// One cell of stairs climbing from the base to <paramref name="height"/> towards local +Z
        /// (the yaw convention of <see cref="MapPieceKind"/>), as solid steps a character can step up.
        /// </summary>
        private static void Stairs(List<MapCollisionBox> boxes, float3 position, float yaw, float cell, float height)
        {
            var rotation = quaternion.RotateY(yaw);
            var depth = cell / StairSteps;
            var rise = height / StairSteps;

            for (var i = 1; i <= StairSteps; i++)
            {
                var top = rise * i;
                var localZ = -cell * 0.5f + (i - 0.5f) * depth;
                var center = position + math.mul(rotation, new float3(0f, top * 0.5f, localZ));
                boxes.Add(new MapCollisionBox(center, new float3(cell * 0.5f, top * 0.5f, depth * 0.5f), yaw));
            }
        }
    }
}

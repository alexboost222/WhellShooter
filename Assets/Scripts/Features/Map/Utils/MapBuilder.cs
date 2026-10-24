using System.Collections.Generic;
using Features.Map.Configs;
using Features.Map.Enums;
using UnityEngine;

namespace Features.Map.Utils
{
    /// <summary>
    /// Turns a <see cref="MapLayout"/> into GameObjects with a <see cref="MapConfig"/>'s palette.
    /// View-side only: used by <c>MapViewSystem</c> in play and by the editor preview.
    /// </summary>
    /// <remarks>
    /// <b>Prefabs are placed by the centre of their mesh bounds, not their pivot.</b> PolygonGeneric
    /// pivots its building pieces on a corner, other kits on the centre; measuring once per prefab
    /// means the palette can mix kits without per-prefab offsets. Height stays the pivot's, which
    /// every kit puts at the base.
    /// <para>
    /// Synty prefabs carry no providers, so a plain <c>Instantiate</c> is right here; a palette
    /// prefab that ever grows <c>[Inject]</c> fields must go through
    /// <c>container.InstantiateInjected</c> instead.
    /// </para>
    /// </remarks>
    public sealed class MapBuilder
    {
        private readonly MapConfig _config;
        private readonly Dictionary<GameObject, Bounds> _bounds = new Dictionary<GameObject, Bounds>();

        public MapBuilder(MapConfig config)
        {
            _config = config;
        }

        public GameObject Build(MapLayout layout, Transform parent)
        {
            var root = new GameObject($"Map (seed {layout.Seed})");
            root.transform.SetParent(parent, false);

            var groups = new Dictionary<MapPieceKind, Transform>();
            foreach (var piece in layout.Pieces)
            {
                var set = _config.Find(piece.Kind);
                if (set == null || set.Prefabs.Length == 0)
                {
                    continue;
                }

                var prefab = set.Prefabs[piece.Variant % set.Prefabs.Length];
                if (prefab == null)
                {
                    continue;
                }

                if (!groups.TryGetValue(piece.Kind, out var group))
                {
                    group = new GameObject(piece.Kind.ToString()).transform;
                    group.SetParent(root.transform, false);
                    groups.Add(piece.Kind, group);
                }

                Place(prefab, set, piece, group);
            }

            AddGround(layout, root.transform);
            return root;
        }

        private void Place(GameObject prefab, MapPrefabSet set, MapPiece piece, Transform group)
        {
            var rotation = Quaternion.Euler(0f, piece.Yaw + set.YawOffset, 0f);
            var bounds = MeshBounds(prefab);
            var scale = prefab.transform.localScale * piece.Scale;
            var center = bounds.center;
            var offset = rotation * Vector3.Scale(new Vector3(center.x, 0f, center.z), scale);
            var position = new Vector3(piece.Position.x, piece.Position.y, piece.Position.z) - offset;

            var instance = Object.Instantiate(prefab, position, rotation, group);
            instance.transform.localScale = scale;

            // Views carry no colliders (Docs/11): the arena's collision is phys-r statics built from
            // the layout (MapCollision). Immediate, so nothing ever sees them.
            foreach (var collider in instance.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(collider);
            }

            if (!CastsShadow(piece.Kind))
            {
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        /// <summary>
        /// Small or flat things don't cast shadows (Docs/10): grass and flowers are most of the
        /// arena's renderers and their shadows are invisible noise; floors and ceilings sit flush
        /// against something that already does.
        /// </summary>
        private static bool CastsShadow(MapPieceKind kind)
        {
            switch (kind)
            {
                case MapPieceKind.GroundDetail:
                case MapPieceKind.Bush:
                case MapPieceKind.Floor:
                case MapPieceKind.Ceiling:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>Bounds of the prefab's meshes, in the prefab root's space.</summary>
        private Bounds MeshBounds(GameObject prefab)
        {
            if (_bounds.TryGetValue(prefab, out var cached))
            {
                return cached;
            }

            var root = prefab.transform;
            var bounds = new Bounds();
            var any = false;
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var local = filter.sharedMesh.bounds;
                var toRoot = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                for (var i = 0; i < 8; i++)
                {
                    var corner = local.center + Vector3.Scale(local.extents,
                        new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    var point = toRoot.MultiplyPoint3x4(corner);
                    if (!any)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }

            _bounds.Add(prefab, bounds);
            return bounds;
        }

        private void AddGround(MapLayout layout, Transform root)
        {
            // One flat plane under everything, out past the outskirts: cheap to draw on mobile and
            // nothing to trip over. What the player walks on is MapCollision's ground slab.
            var margin = layout.CellSize * (_config.Settings.OutskirtsRings + 2);
            var size = new Vector2(layout.Width * layout.CellSize + margin * 2f, layout.Depth * layout.CellSize + margin * 2f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ground.name = "Ground";
            ground.transform.SetParent(root, false);
            ground.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ground.transform.localScale = new Vector3(size.x, size.y, 1f);
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            if (_config.GroundMaterial != null)
            {
                ground.GetComponent<MeshRenderer>().sharedMaterial = _config.GroundMaterial;
            }
        }
    }
}

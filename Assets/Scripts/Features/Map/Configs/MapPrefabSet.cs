using System;
using Features.Map.Enums;
using UnityEngine;

namespace Features.Map.Configs
{
    /// <summary>The prefabs one <see cref="MapPieceKind"/> is drawn with.</summary>
    [Serializable]
    public sealed class MapPrefabSet
    {
        public MapPieceKind Kind;

        [Tooltip("Variants; a piece picks one by its variant number.")]
        public GameObject[] Prefabs = Array.Empty<GameObject>();

        [Tooltip("Added to the piece's yaw, for meshes modelled facing another way than the convention " +
                 "(walls run along +X, stairs climb towards +Z).")]
        public float YawOffset;

        [Tooltip("Remove the prefab's colliders — for pure decoration the player should walk through.")]
        public bool StripColliders;
    }
}

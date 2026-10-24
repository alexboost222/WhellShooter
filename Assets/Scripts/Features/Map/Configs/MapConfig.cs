using System.Collections.Generic;
using Engine.Configs;
using Features.Map.Enums;
using Features.Map.Utils;
using UnityEngine;

namespace Features.Map.Configs
{
    /// <summary>
    /// The arena generator's tuning and its prefab palette (Docs/08). Pieces in a layout store a
    /// <see cref="MapPieceKind"/> and a variant number; this is the only place a prefab is named.
    /// </summary>
    [CreateAssetMenu(menuName = "WheelShooter/Configs/Map", fileName = nameof(MapConfig))]
    public sealed class MapConfig : Config
    {
        [SerializeField] private MapSettings _settings = MapSettings.Default;
        [SerializeField] private List<MapPrefabSet> _palette = new List<MapPrefabSet>();

        [Tooltip("The one flat ground plane under the arena and its outskirts. World-space (triplanar) " +
                 "materials tile best on it.")]
        [SerializeField] private Material _groundMaterial;

        public MapSettings Settings => _settings;

        public Material GroundMaterial => _groundMaterial;

        public IReadOnlyList<MapPrefabSet> Palette => _palette;

        /// <summary>The set for a kind, or null when the palette doesn't draw that kind.</summary>
        public MapPrefabSet Find(MapPieceKind kind)
        {
            for (var i = 0; i < _palette.Count; i++)
            {
                if (_palette[i].Kind == kind)
                {
                    return _palette[i];
                }
            }

            return null;
        }

        /// <summary>For tests and tooling: a config built in code rather than loaded.</summary>
        internal void Set(MapSettings settings, List<MapPrefabSet> palette)
        {
            _settings = settings;
            _palette = palette;
        }
    }
}

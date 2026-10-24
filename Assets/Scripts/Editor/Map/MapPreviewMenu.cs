using Engine.Configs;
using Features.Map.Configs;
using Features.Map.Utils;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Map
{
    /// <summary>
    /// Builds the arena in the open scene without entering play mode, for judging the generator and
    /// the palette by eye. The preview is marked editor-only and never saved with the scene.
    /// </summary>
    public static class MapPreviewMenu
    {
        private const string PreviewName = "[Map Preview]";

        private static int _lastSeed = 1;

        [MenuItem("WheelShooter/Map/Preview In Scene (New Seed)")]
        private static void PreviewNewSeed()
        {
            Preview(new System.Random().Next(1, int.MaxValue));
        }

        [MenuItem("WheelShooter/Map/Preview In Scene (Same Seed)")]
        private static void PreviewSameSeed()
        {
            Preview(_lastSeed);
        }

        [MenuItem("WheelShooter/Map/Clear Preview")]
        private static void Clear()
        {
            var existing = GameObject.Find(PreviewName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        /// <summary>Builds the preview for <paramref name="seed"/>. Public for CLI and tests.</summary>
        public static GameObject Preview(int seed)
        {
            Clear();
            _lastSeed = seed;

            var config = Resources.Load<MapConfig>($"{ConfigCatalog.DefaultResourcesPath}/{nameof(MapConfig)}");
            if (config == null)
            {
                Debug.LogError($"No {nameof(MapConfig)} in Resources/{ConfigCatalog.DefaultResourcesPath}.");
                return null;
            }

            var layout = MapGenerator.Generate(seed, config.Settings);
            var holder = new GameObject(PreviewName) { tag = "EditorOnly" };
            new MapBuilder(config).Build(layout, holder.transform);

            // On every object, not just the root: the flag is not inherited, and a child without it
            // would be saved into the scene.
            foreach (var transform in holder.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.hideFlags = HideFlags.DontSave;
            }

            Debug.Log($"[Map] Preview seed {seed}: {layout.Buildings.Count} buildings, {layout.Pieces.Count} pieces.");
            return holder;
        }
    }
}

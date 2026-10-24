using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Engine.Diagnostics;
using Engine.Diagnostics.Logs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Engine.Runtime
{
    /// <summary>
    /// Async scene loading behind an interface, so scene flow is testable and the netcode layer
    /// later has one place to hide scene synchronisation (Docs/02).
    /// </summary>
    public interface ISceneLoader
    {
        string ActiveScene { get; }

        bool IsLoading { get; }

        /// <summary>
        /// Loads single-mode, replacing whatever is open. No-ops if already there, unless
        /// <paramref name="reload"/> asks for the scene we are standing in to be built again.
        /// </summary>
        /// <param name="reload">
        /// <b>Load it even if it is the active scene.</b> A scene's objects are the world's
        /// entities, so a caller that has emptied the world is not standing in that scene any more
        /// however right its name looks — it is standing in a stripped copy of it, and only a
        /// second load puts the objects back.
        /// </param>
        UniTask LoadAsync(string sceneName, bool reload = false);
    }

    public sealed class SceneLoader : ISceneLoader
    {
        /// <summary>Raised after a scene finishes loading — Bootstrap uses it to inject scene providers.</summary>
        public event Action<Scene> Loaded;

        public string ActiveScene => SceneManager.GetActiveScene().name;

        public bool IsLoading { get; private set; }

        public async UniTask LoadAsync(string sceneName, bool reload = false)
        {
            Guard.ThrowIfNullOrEmpty(sceneName, nameof(sceneName));
            Guard.ThrowIfNotMainThread();
            Guard.ThrowIf(IsLoading, $"Already loading a scene; cannot start '{sceneName}'.");

            if (!reload && ActiveScene == sceneName) return;

            IsLoading = true;
            try
            {
                Log.Info(LogChannels.Runtime, $"Loading scene '{sceneName}'.");

                var operation = Begin(sceneName);
                Guard.ThrowIf(operation == null,
                    $"No scene called '{sceneName}' could be loaded. In a player that means it is not " +
                    "in Build Settings; in the editor, that no such scene asset exists.");

                await operation.ToUniTask();
            }
            finally
            {
                IsLoading = false;
            }

            Loaded?.Invoke(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// Starts the load, by build index normally and by asset path for an editor-only scene that
        /// is not in Build Settings. Null when neither route can find it.
        /// </summary>
        /// <remarks>
        /// <b>The fallback is what lets a developer jump into any scene in the project</b>, most of
        /// which were never added to Build Settings and should not be.
        /// <para>
        /// <b>Build Settings is checked first rather than caught after</b>, because
        /// <see cref="SceneManager.LoadSceneAsync(string, LoadSceneMode)"/> answers an unknown name
        /// with a console error and a null, and an error in the log for a case we handle is how
        /// people learn to ignore the log.
        /// </para>
        /// </remarks>
        private static AsyncOperation Begin(string sceneName)
        {
#if UNITY_EDITOR
            if (!InBuildSettings(sceneName))
            {
                return EditorLoad(sceneName);
            }
#endif

            return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }

        private static bool InBuildSettings(string sceneName)
        {
            var count = SceneManager.sceneCountInBuildSettings;

            for (var i = 0; i < count; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);

                if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        /// <summary>The editor's own play-mode load, by asset path, for a scene no build index knows.</summary>
        private static AsyncOperation EditorLoad(string sceneName)
        {
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:SceneAsset {sceneName}");

            for (var i = 0; i < guids.Length; i++)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);

                // FindAssets matches loosely — "Level1" also returns "Level10" — so the name has to
                // be checked rather than trusted.
                if (!string.Equals(Path.GetFileNameWithoutExtension(path), sceneName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Log.Info(LogChannels.Runtime,
                    $"'{sceneName}' is not in Build Settings; loading it from '{path}'. Editor only.");

                return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    path, new LoadSceneParameters(LoadSceneMode.Single));
            }

            return null;
        }
#endif
    }
}

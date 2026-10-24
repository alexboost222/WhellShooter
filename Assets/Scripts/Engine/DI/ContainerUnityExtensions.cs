using Engine.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Engine.DI
{
    /// <summary>
    /// Injection into MonoBehaviours. Kept out of <see cref="Container"/> itself so the
    /// container stays free of <c>UnityEngine</c> and remains usable in headless edit-mode tests.
    /// </summary>
    /// <remarks>
    /// The primary customer is <b>Providers</b> — the thin MonoBehaviours on scene and prefab
    /// objects that register themselves into a Storage (Docs/03, Docs/06). A Provider has to
    /// reach its Storage somehow, and the alternative to injection is a singleton, which those
    /// docs rule out explicitly.
    /// <para>
    /// This does not license views to reach into gameplay: a View still binds a ViewModel and
    /// the ViewModel still does the writing (Docs/02). Injecting a bus into a view to publish
    /// straight from a button handler is the thing the UI rules exist to prevent.
    /// </para>
    /// <para>
    /// <b>Note the method names.</b> <c>container.Inject(someGameObject)</c> compiles but does
    /// nothing — it binds to the generic <see cref="Container.Inject{T}"/>, and a GameObject
    /// has no <c>[Inject]</c> fields of its own. Use these named methods for anything in the
    /// hierarchy.
    /// </para>
    /// </remarks>
    public static class ContainerUnityExtensions
    {
        /// <summary>Injects every MonoBehaviour on this GameObject, ignoring its children.</summary>
        public static GameObject InjectGameObject(this Container container, GameObject gameObject)
        {
            Guard.ThrowIfNull(container, nameof(container));
            Guard.ThrowIfDestroyed(gameObject, nameof(gameObject));

            InjectAll(container, gameObject.GetComponents<MonoBehaviour>());
            return gameObject;
        }

        /// <summary>
        /// Injects every MonoBehaviour on this GameObject and all of its children, active or
        /// not — a disabled view still needs its dependencies before it is switched on.
        /// </summary>
        /// <remarks>
        /// Allocates an array per call, so this belongs at boot, on scene load, and on window
        /// or view instantiation — not inside a hot spawn loop.
        /// </remarks>
        public static GameObject InjectHierarchy(this Container container, GameObject gameObject)
        {
            Guard.ThrowIfNull(container, nameof(container));
            Guard.ThrowIfDestroyed(gameObject, nameof(gameObject));

            InjectAll(container, gameObject.GetComponentsInChildren<MonoBehaviour>(true));
            return gameObject;
        }

        /// <summary>
        /// Instantiates a prefab with its dependencies already in place — <c>Awake</c> does not run
        /// until every <c>[Inject]</c> field in the new hierarchy is filled.
        /// </summary>
        /// <remarks>
        /// <b>Use this for any prefab carrying a Provider, and never <c>Instantiate</c> +
        /// <c>InjectHierarchy</c> by hand.</b> That order looks right and is not:
        /// <c>Object.Instantiate</c> runs <c>Awake</c> <em>synchronously, before it returns</em>, so
        /// injection lands afterwards and every provider has already run with null fields. A
        /// provider that guards with <c>_storage?.Register(...)</c> then registers nothing, says
        /// nothing, and is discovered much later by whatever needed the registration (found
        /// 2026-07-30, when the UiRouter became the first thing to read <c>BootstrapStorage</c> —
        /// no provider registration in the project had ever actually worked).
        /// <para>
        /// Unity has no "instantiate inactive" overload, so the instance is parented to a disabled
        /// holder — which is what defers <c>Awake</c> — injected, and then reparented out, which is
        /// what runs it. The holder sits at the origin with an identity transform, so the
        /// instance's local transform ends up exactly where plain <c>Instantiate(prefab)</c> would
        /// have put it.
        /// </para>
        /// </remarks>
        /// <summary>
        /// As <see cref="InstantiateInjected(Container,GameObject)"/>, placed where it belongs
        /// before <c>Awake</c> runs.
        /// </summary>
        /// <remarks>
        /// <b>The pose has to be set inside the holder, not after the call returns</b> — which is
        /// why this is an overload rather than two lines at the call site. Providers read their
        /// transform in <c>Awake</c>: <c>MechanismProvider.CaptureRest</c> records the pose a door
        /// is closed in, and a door built at the origin and moved afterwards believes it is already
        /// open by however far it travelled. The holder is at the origin with an identity transform,
        /// so setting world position here and reparenting with <c>worldPositionStays: false</c>
        /// leaves the instance exactly where it was asked for.
        /// </remarks>
        public static GameObject InstantiateInjected(this Container container, GameObject prefab,
            Vector3 position, Quaternion rotation)
        {
            return InstantiateInjected(container, prefab, position, rotation, place: true);
        }

        public static GameObject InstantiateInjected(this Container container, GameObject prefab)
        {
            return InstantiateInjected(container, prefab, Vector3.zero, Quaternion.identity, place: false);
        }

        private static GameObject InstantiateInjected(Container container, GameObject prefab,
            Vector3 position, Quaternion rotation, bool place)
        {
            Guard.ThrowIfNull(container, nameof(container));
            Guard.ThrowIfDestroyed(prefab, nameof(prefab));

            var holder = new GameObject("~injecting");
            holder.SetActive(false);

            try
            {
                var instance = Object.Instantiate(prefab, holder.transform);
                instance.name = prefab.name;

                if (place)
                {
                    instance.transform.SetPositionAndRotation(position, rotation);
                }

                container.InjectHierarchy(instance);

                // Leaving the disabled holder is what activates the instance, and therefore what
                // finally runs Awake — now with every dependency in place.
                instance.transform.SetParent(null, worldPositionStays: false);
                return instance;
            }
            finally
            {
                // finally, so a throwing injection doesn't strand a disabled object in the scene
                // holding a half-built hierarchy.
                if (Application.isPlaying) Object.Destroy(holder);
                else Object.DestroyImmediate(holder);
            }
        }

        /// <summary>
        /// Injects every MonoBehaviour in a loaded scene. Meta hooks this to scene loading so
        /// Providers authored in a level are wired before anything ticks.
        /// </summary>
        public static void InjectScene(this Container container, Scene scene)
        {
            Guard.ThrowIfNull(container, nameof(container));
            Guard.ThrowIfNot(scene.IsValid(), $"Scene '{scene.name}' is not valid.");
            Guard.ThrowIfNot(scene.isLoaded, $"Scene '{scene.name}' is not loaded yet.");

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++) container.InjectHierarchy(roots[i]);
        }

        private static void InjectAll(Container container, MonoBehaviour[] components)
        {
            for (var i = 0; i < components.Length; i++)
            {
                // Null shows up here for a component whose script failed to load — a broken
                // reference in a prefab shouldn't take the whole boot down with it.
                if (components[i] == null) continue;

                container.Inject(components[i]);
            }
        }
    }
}

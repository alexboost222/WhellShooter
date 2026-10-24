using System;
using UnityEngine;

namespace Engine.Disposables
{
    /// <summary>
    /// Ties a <see cref="DisposableBag"/> to a GameObject's lifetime. You never add this
    /// yourself — <c>disposable.AddTo(gameObject)</c> attaches one on first use, and everything
    /// it holds is disposed in <c>OnDestroy</c>.
    /// </summary>
    /// <remarks>
    /// Kept out of the Add Component menu, but deliberately still visible in the Inspector: when
    /// a subscription outlives what you expected, seeing the component (and its count) on the
    /// object is how you find out why.
    /// </remarks>
    /// <remarks>
    /// <see cref="ExecuteAlways"/> is load-bearing, not a test accommodation: outside play mode
    /// Unity does not send <c>OnDestroy</c> to a plain MonoBehaviour at all, so without it
    /// anything attached to an object destroyed in the editor leaks silently. It costs nothing
    /// here — this component has no update loop.
    /// </remarks>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class DestroyTrigger : MonoBehaviour
    {
        private readonly DisposableBag _bag = new DisposableBag();

        /// <summary>How many disposables this GameObject is keeping alive.</summary>
        public int Count => _bag.Count;

        internal static DestroyTrigger For(GameObject gameObject)
        {
            var trigger = gameObject.GetComponent<DestroyTrigger>();
            if (trigger == null) trigger = gameObject.AddComponent<DestroyTrigger>();
            return trigger;
        }

        internal void Add(IDisposable disposable) => _bag.Add(disposable);

        private void OnDestroy() => _bag.Dispose();
    }
}

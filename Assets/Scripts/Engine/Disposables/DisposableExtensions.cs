using System;
using Engine.Diagnostics;
using UnityEngine;

namespace Engine.Disposables
{
    /// <summary>
    /// The shared <c>.AddTo(...)</c> vocabulary. Every subscription and binding in the project
    /// ends in one of these, so a disposable that isn't attached to something is visibly odd at
    /// the call site.
    /// </summary>
    public static class DisposableExtensions
    {
        /// <summary>
        /// Hands the disposable to a bag whose owner disposes it explicitly — ViewModels,
        /// systems, services. Returns the disposable so it can be assigned in the same statement.
        /// </summary>
        public static T AddTo<T>(this T disposable, DisposableBag bag) where T : class, IDisposable
        {
            Guard.ThrowIfNull(disposable, nameof(disposable));
            Guard.ThrowIfNull(bag, nameof(bag));

            bag.Add(disposable);
            return disposable;
        }

        /// <summary>
        /// Ties the disposable to a GameObject: it is disposed when that object is destroyed.
        /// For views whose lifetime is the hierarchy — world-space entity views, UI widgets.
        /// </summary>
        /// <remarks>
        /// Attaches a <see cref="DestroyTrigger"/> on first use, so this costs one component per
        /// GameObject. Fine for UI; don't reach for it inside a hot spawn loop.
        /// </remarks>
        public static T AddTo<T>(this T disposable, GameObject gameObject) where T : class, IDisposable
        {
            Guard.ThrowIfNull(disposable, nameof(disposable));

            // A destroyed target can never fire OnDestroy again, so attaching here would leak
            // silently. That's a bug at the call site, not something to paper over.
            Guard.ThrowIfDestroyed(gameObject, nameof(gameObject));

            DestroyTrigger.For(gameObject).Add(disposable);
            return disposable;
        }

        /// <summary>
        /// Convenience for the common case inside a view: <c>subscription.AddTo(this)</c>.
        /// Identical to passing <c>component.gameObject</c>.
        /// </summary>
        public static T AddTo<T>(this T disposable, Component component) where T : class, IDisposable
        {
            Guard.ThrowIfDestroyed(component, nameof(component));
            return disposable.AddTo(component.gameObject);
        }
    }
}

using System;
using System.Collections.Generic;
using Engine.Diagnostics;
using Engine.Diagnostics.Logs;

namespace Engine.Disposables
{
    /// <summary>
    /// Collects <see cref="IDisposable"/>s so an owner can drop them all at once — bus
    /// subscriptions, UI bindings, anything with a lifetime. The owner disposes the bag
    /// explicitly (ViewModels in <c>OnClose</c>, systems, services); for lifetimes that follow
    /// the scene hierarchy use <c>AddTo(gameObject)</c> instead.
    /// </summary>
    /// <remarks>
    /// Main-thread only, asserted — same rule as the message bus.
    /// </remarks>
    public sealed class DisposableBag : IDisposable
    {
        private const int DefaultCapacity = 4;

        private readonly List<IDisposable> _items;
        private bool _disposed;

        public DisposableBag() : this(DefaultCapacity)
        {
        }

        public DisposableBag(int capacity)
        {
            Guard.ThrowIfNegative(capacity, nameof(capacity));
            _items = new List<IDisposable>(capacity);
        }

        /// <summary>How many disposables are currently held.</summary>
        public int Count => _items.Count;

        /// <summary><see cref="Dispose"/> has been called; the bag is spent.</summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Takes ownership of a disposable. Adding to an already-disposed bag disposes the
        /// newcomer immediately rather than holding it — a subscription that completes after
        /// its owner is gone must not leak.
        /// </summary>
        public void Add(IDisposable disposable)
        {
            Guard.ThrowIfNull(disposable, nameof(disposable));
            Guard.ThrowIfNotMainThread();

            if (_disposed)
            {
                disposable.Dispose();
                return;
            }

            _items.Add(disposable);
        }

        /// <summary>
        /// Disposes everything held and empties the bag, leaving it usable. This is what a
        /// ViewModel wants between <c>OnClose</c> and a later <c>OnOpen</c> — the router can
        /// reopen a window without allocating a new bag.
        /// </summary>
        public void Clear()
        {
            Guard.ThrowIfNotMainThread();
            DisposeItems();
        }

        /// <summary>Disposes everything held. Idempotent, and terminal — use <see cref="Clear"/> to reuse.</summary>
        public void Dispose()
        {
            Guard.ThrowIfNotMainThread();
            if (_disposed) return;

            _disposed = true;
            DisposeItems();
        }

        private void DisposeItems()
        {
            if (_items.Count == 0) return;

            // Detach before disposing: a Dispose() that adds back into this bag (or clears it)
            // then can't corrupt the iteration, and after Dispose the re-entrant Add disposes
            // the newcomer on the spot.
            var items = _items.ToArray();
            _items.Clear();

            // Reverse order mirrors construction, the same way nested `using` blocks unwind.
            for (var i = items.Length - 1; i >= 0; i--)
            {
                try
                {
                    items[i].Dispose();
                }
                catch (Exception exception)
                {
                    // One bad disposable must not strand the rest — the same error isolation
                    // the bus applies to a throwing handler.
                    Log.Error(LogChannels.Core, "A disposable threw while emptying a DisposableBag; the rest were still disposed.", exception);
                }
            }
        }
    }
}

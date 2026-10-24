using System;
using System.Threading;
using Engine.Diagnostics;

namespace Engine.Disposables
{
    /// <summary>
    /// Factories for the small disposables everything else is built from — most importantly
    /// the unsubscribe token a publisher hands back from <c>Subscribe</c>.
    /// </summary>
    public static class Disposable
    {
        /// <summary>A disposable that does nothing. Shared instance; safe to hand out freely.</summary>
        public static IDisposable Empty { get; } = new EmptyDisposable();

        /// <summary>
        /// Runs <paramref name="onDispose"/> the first time the result is disposed, and never
        /// again however many times it is disposed after that.
        /// </summary>
        public static IDisposable Create(Action onDispose)
        {
            Guard.ThrowIfNull(onDispose, nameof(onDispose));
            return new ActionDisposable(onDispose);
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

        private sealed class ActionDisposable : IDisposable
        {
            private Action _onDispose;

            internal ActionDisposable(Action onDispose) => _onDispose = onDispose;

            public void Dispose()
            {
                // Swap-then-invoke: run-once holds even if two callers race here.
                var onDispose = Interlocked.Exchange(ref _onDispose, null);
                onDispose?.Invoke();
            }
        }
    }
}

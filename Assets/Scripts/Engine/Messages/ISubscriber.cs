using System;

namespace Engine.Messages
{
    /// <summary>
    /// Push delivery — the UI side. Handlers run when the Runner flushes, at one defined point
    /// (the start of the Update phase), never inside <see cref="IPublisher{T}.Publish"/>. UI
    /// therefore always observes consistent post-tick state.
    /// </summary>
    public interface ISubscriber<T> where T : struct
    {
        /// <summary>
        /// Registers a handler and returns its unsubscribe token — collect it with
        /// <c>.AddTo(bag)</c> or <c>.AddTo(gameObject)</c>. Disposing twice is harmless.
        /// </summary>
        IDisposable Subscribe(Action<T> handler);
    }
}

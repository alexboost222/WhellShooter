using System;
using System.Collections.Generic;
using Engine.Diagnostics;
using Engine.Diagnostics.Logs;
using Engine.Disposables;

namespace Engine.Messages
{
    /// <summary>Phase-driving surface of a pipe, so the bus can hold pipes of every type together.</summary>
    internal interface IMessagePipe
    {
        void BeginPhase(MessagePhase phase);
        void FlushPush();

        /// <summary>The <c>T</c> this pipe carries. For the bus's type-erased diagnostics.</summary>
        Type MessageType { get; }

        int HandlerCount { get; }

        int QueueCount { get; }

        /// <summary>How many messages have ever been published through it.</summary>
        long PublishedCount { get; }

        /// <summary>How many are waiting for the next push flush. Zero when nothing subscribes.</summary>
        int PendingPush { get; }
    }

    /// <summary>
    /// Everything for one message type: the push subscribers and the per-phase queues.
    /// Created lazily by <see cref="MessageBus"/> on first request.
    /// </summary>
    internal sealed class MessagePipe<T> : IPublisher<T>, ISubscriber<T>, IMessagePipe where T : struct
    {
        private static readonly Action<T>[] NoHandlers = Array.Empty<Action<T>>();

        private readonly List<T> _pendingPush = new List<T>();
        private readonly List<SubscriberQueue<T>> _queues = new List<SubscriberQueue<T>>();

        // Copy-on-write: delivery iterates a snapshot, so a handler that unsubscribes itself
        // mid-flush cannot shift the loop out from under it.
        private Action<T>[] _handlers = NoHandlers;
        private T[] _flushBuffer = Array.Empty<T>();

        private long _published;

        public int HandlerCount => _handlers.Length;

        public Type MessageType => typeof(T);

        public int QueueCount => _queues.Count;

        public long PublishedCount => _published;

        public int PendingPush => _pendingPush.Count;

        public void Publish(in T message)
        {
            Guard.ThrowIfNotMainThread();

            _published++;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Above the gate below, deliberately: a monitor that subscribed would flip that
            // condition for every type with no subscribers and change what the bus does.
            // MessageMonitor's remarks carry the whole argument. Nothing boxes while it is off.
            Diagnostics.MessageMonitor.Observe(in message);
#endif

            // Transient events only: with nobody subscribed there is nothing to hold, and
            // buffering anyway would grow without bound in a build that never opens that UI.
            if (_handlers.Length > 0) _pendingPush.Add(message);

            for (var i = 0; i < _queues.Count; i++) _queues[i].Enqueue(message);
        }

        public IDisposable Subscribe(Action<T> handler)
        {
            Guard.ThrowIfNull(handler, nameof(handler));
            Guard.ThrowIfNotMainThread();

            var handlers = new Action<T>[_handlers.Length + 1];
            Array.Copy(_handlers, handlers, _handlers.Length);
            handlers[_handlers.Length] = handler;
            _handlers = handlers;

            return Disposable.Create(() => Unsubscribe(handler));
        }

        public void BeginPhase(MessagePhase phase)
        {
            for (var i = 0; i < _queues.Count; i++)
                if (_queues[i].Phase == phase)
                    _queues[i].Swap();
        }

        public void FlushPush()
        {
            var count = _pendingPush.Count;
            if (count == 0) return;

            // Take the pending set and clear it *before* delivering: a handler that publishes
            // is heard on the next flush, never this one. Without that, a pair of handlers
            // answering each other would spin here forever.
            if (_flushBuffer.Length < count) _flushBuffer = new T[count];
            _pendingPush.CopyTo(0, _flushBuffer, 0, count);
            _pendingPush.Clear();

            var handlers = _handlers;
            for (var message = 0; message < count; message++)
            {
                for (var handler = 0; handler < handlers.Length; handler++)
                {
                    try
                    {
                        handlers[handler](_flushBuffer[message]);
                    }
                    catch (Exception exception)
                    {
                        // One bad handler must not swallow the others or stall the phase.
                        Log.Error(LogChannels.Messages,
                            $"A {typeof(T).Name} subscriber threw; the remaining subscribers still ran.",
                            exception);
                    }
                }
            }
        }

        internal SubscriberQueue<T> Queue(MessagePhase phase)
        {
            for (var i = 0; i < _queues.Count; i++)
                if (_queues[i].Phase == phase)
                    return _queues[i];

            var queue = new SubscriberQueue<T>(phase);
            _queues.Add(queue);
            return queue;
        }

        private void Unsubscribe(Action<T> handler)
        {
            var index = Array.IndexOf(_handlers, handler);
            if (index < 0) return;

            if (_handlers.Length == 1)
            {
                _handlers = NoHandlers;
                return;
            }

            var handlers = new Action<T>[_handlers.Length - 1];
            Array.Copy(_handlers, handlers, index);
            Array.Copy(_handlers, index + 1, handlers, index, handlers.Length - index);
            _handlers = handlers;
        }
    }
}

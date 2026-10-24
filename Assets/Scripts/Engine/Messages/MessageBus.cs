using System;
using System.Collections.Generic;
using Engine.Diagnostics;

namespace Engine.Messages
{
    /// <summary>
    /// The bridge between ECS and UI. Owns one pipe per message type, created on first request,
    /// and exposes the two points the Runner drives.
    /// </summary>
    /// <remarks>
    /// <b>Transport, not logic.</b> UI intent is drained into request components by bridge
    /// systems; components remain the truth. No sticky or latest-value messages, no
    /// request-response — that is two messages by convention. "UI needs current state" is a
    /// ViewModel reading storages, not a bus feature.
    /// <para>
    /// <b>The Runner owns the clock.</b> <see cref="BeginPhase"/> at the start of each phase,
    /// <see cref="FlushPush"/> at the start of Update. Consumers never empty anything — which
    /// is why <see cref="Queue{T}"/> registers the queue here as it creates it: no injectable
    /// queue can exist that nobody empties (Docs/02).
    /// </para>
    /// <para>Main-thread only, asserted rather than synchronized.</para>
    /// </remarks>
    public sealed class MessageBus
    {
        private readonly Dictionary<Type, IMessagePipe> _pipes = new Dictionary<Type, IMessagePipe>();

        // Same pipes, kept in a list: the phase drivers run every tick and must not allocate
        // a dictionary enumerator to do it.
        private readonly List<IMessagePipe> _ordered = new List<IMessagePipe>();

        /// <summary>How many message types have been touched. Diagnostics and tests.</summary>
        public int PipeCount => _ordered.Count;

        /// <summary>The send end for <typeparamref name="T"/>.</summary>
        public IPublisher<T> Publisher<T>() where T : struct => Pipe<T>();

        /// <summary>The push end for <typeparamref name="T"/> — UI-side callbacks.</summary>
        public ISubscriber<T> Subscriber<T>() where T : struct => Pipe<T>();

        /// <summary>
        /// The pull end for <typeparamref name="T"/> in one phase — ECS-side. One instance per
        /// (type, phase), shared by every system that asks for it.
        /// </summary>
        public SubscriberQueue<T> Queue<T>(MessagePhase phase = MessagePhase.FixedUpdate) where T : struct =>
            Pipe<T>().Queue(phase);

        /// <summary>
        /// Runner: call at the <b>start</b> of a phase. Swaps every queue bound to it, freezing
        /// its contents for the phase so system order can't change what gets delivered.
        /// </summary>
        public void BeginPhase(MessagePhase phase)
        {
            Guard.ThrowIfNotMainThread();

            for (var i = 0; i < _ordered.Count; i++) _ordered[i].BeginPhase(phase);
        }

        /// <summary>
        /// Runner: call at the start of the Update phase. Delivers everything published since
        /// the last flush to push subscribers.
        /// </summary>
        public void FlushPush()
        {
            Guard.ThrowIfNotMainThread();

            for (var i = 0; i < _ordered.Count; i++) _ordered[i].FlushPush();
        }

        /// <summary>
        /// Every pipe that exists, into the caller's list, which is cleared first. What the Messages
        /// window's table is.
        /// </summary>
        /// <remarks>
        /// <b>The set is complete once boot has finished and never grows again.</b> A pipe is created
        /// on the first <c>Publisher</c>/<c>Subscriber</c>/<c>Queue</c> call for its type, and in
        /// practice every one of those is an <c>[Inject]</c> field resolved during composition — so
        /// this is, in effect, the list of message types anything in this build cares about.
        /// </remarks>
        public void CollectPipes(List<MessagePipeInfo> into)
        {
            Guard.ThrowIfNull(into, nameof(into));

            into.Clear();

            for (var i = 0; i < _ordered.Count; i++)
            {
                var pipe = _ordered[i];

                into.Add(new MessagePipeInfo(pipe.MessageType, pipe.HandlerCount, pipe.QueueCount,
                    pipe.PublishedCount, pipe.PendingPush));
            }

            into.Sort(static (a, b) => string.CompareOrdinal(a.MessageType.Name, b.MessageType.Name));
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Publishes a message whose type is only known at runtime. Editor tooling only.
        /// </summary>
        /// <remarks>
        /// <b>The same trampoline idiom as <c>ComponentAccessor</c>'s reader</b>, and the second
        /// occurrence is what makes it a pattern rather than a one-off: the closed generic is a
        /// method of ours taking <c>object</c>, so reflection never meets an <c>in</c> parameter.
        /// <para>
        /// <b>A write into a running game, so it is fenced and the window confirms first.</b>
        /// Publishing a message by hand runs every subscriber and every system draining the queue, in
        /// a state nothing produced — which is exactly what makes it useful for reaching a UI flow
        /// without playing to it, and exactly why it is not a thing to do by accident.
        /// </para>
        /// </remarks>
        public void PublishBoxed(Type messageType, object message)
        {
            Guard.ThrowIfNull(messageType, nameof(messageType));
            Guard.ThrowIfNull(message, nameof(message));
            Guard.ThrowIfNotMainThread();

            if (!messageType.IsInstanceOfType(message))
            {
                throw Guard.Fail($"{message.GetType().Name} is not a {messageType.Name}.");
            }

            PublisherFor(messageType)(this, message);
        }

        private static readonly Dictionary<Type, Action<MessageBus, object>> BoxedPublishers =
            new Dictionary<Type, Action<MessageBus, object>>();

        private static readonly System.Reflection.MethodInfo PublishBoxedMethod =
            typeof(MessageBus).GetMethod(nameof(PublishBoxedOf),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        private static Action<MessageBus, object> PublisherFor(Type messageType)
        {
            if (BoxedPublishers.TryGetValue(messageType, out var publisher))
            {
                return publisher;
            }

            var closed = PublishBoxedMethod.MakeGenericMethod(messageType);
            publisher = (Action<MessageBus, object>)Delegate.CreateDelegate(
                typeof(Action<MessageBus, object>), closed);

            BoxedPublishers.Add(messageType, publisher);
            return publisher;
        }

        /// <summary>The trampoline. Closed by <see cref="PublisherFor"/> and never called directly.</summary>
        private static void PublishBoxedOf<T>(MessageBus bus, object boxed) where T : struct
        {
            bus.Publisher<T>().Publish((T)boxed);
        }
#endif

        private MessagePipe<T> Pipe<T>() where T : struct
        {
            Guard.ThrowIfNotMainThread();

            if (_pipes.TryGetValue(typeof(T), out var existing)) return (MessagePipe<T>)existing;

            var pipe = new MessagePipe<T>();
            _pipes.Add(typeof(T), pipe);
            _ordered.Add(pipe);
            return pipe;
        }
    }
}

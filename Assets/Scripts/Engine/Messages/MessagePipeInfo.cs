using System;

namespace Engine.Messages
{
    /// <summary>One message type's pipe, flattened for a reader.</summary>
    /// <remarks>
    /// <b><see cref="PendingPush"/> is the one worth understanding.</b> It is nonzero only between a
    /// publish and the next push flush, and only for a type something has subscribed to — a pipe with
    /// no subscribers never buffers at all (<see cref="MessagePipe{T}"/>). So a type showing a large
    /// pending count is a type whose subscribers are not being flushed, which means the Runner's
    /// Update phase is not running.
    /// </remarks>
    public readonly struct MessagePipeInfo
    {
        public MessagePipeInfo(Type messageType, int handlerCount, int queueCount, long publishedCount,
            int pendingPush)
        {
            MessageType = messageType;
            HandlerCount = handlerCount;
            QueueCount = queueCount;
            PublishedCount = publishedCount;
            PendingPush = pendingPush;
        }

        public Type MessageType { get; }

        /// <summary>Push subscribers — UI-side callbacks, delivered once a frame.</summary>
        public int HandlerCount { get; }

        /// <summary>Pull queues — ECS-side, one per phase that asked for one.</summary>
        public int QueueCount { get; }

        /// <summary>Published since this pipe existed, which is since boot.</summary>
        public long PublishedCount { get; }

        /// <summary>Waiting for the next push flush. See the remarks on the type.</summary>
        public int PendingPush { get; }

        public override string ToString() =>
            $"{MessageType.Name}: {PublishedCount} published, {HandlerCount} handler(s), {QueueCount} queue(s)";
    }
}

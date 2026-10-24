#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace Engine.Messages.Diagnostics
{
    /// <summary>
    /// A tap on every publish, for the Messages window. Off by default and free while it is.
    /// </summary>
    /// <remarks>
    /// <b>It is not a subscriber, and that is the entire design.</b> The obvious way to watch the bus
    /// is <c>bus.Subscriber&lt;T&gt;().Subscribe(...)</c>, and it is wrong here for a reason that is
    /// invisible until it bites: <see cref="MessagePipe{T}.Publish"/> buffers into
    /// <c>_pendingPush</c> only when a handler exists. Subscribing to a type that had none therefore
    /// <i>changes what the bus does</i> — a list that was always empty starts filling and draining
    /// once a frame, for as long as the window is open. A monitor whose act of watching alters the
    /// thing watched is a Heisenbug factory, and the bug it manufactures looks like the bug you
    /// opened it to find.
    /// <para>
    /// So the hook sits <b>above</b> that gate: publish tells this, unconditionally, before deciding
    /// anything. Nothing downstream can tell whether anybody is listening.
    /// </para>
    /// <para>
    /// <b><see cref="Enabled"/> is what makes the fence honest rather than decorative.</b> While off,
    /// observing is a static bool read and a return — no boxing, no delegate call, nothing allocated.
    /// The boxing that a type-erased observer needs happens only while somebody is actually watching,
    /// which is a moment when a few allocations per frame are the cheapest thing in the room.
    /// </para>
    /// <para>
    /// <b>Editor and development builds only.</b> A release build has no window to show any of this
    /// in, and a per-publish branch it can never take is a per-publish branch worth deleting.
    /// </para>
    /// </remarks>
    public static class MessageMonitor
    {
        /// <summary>Whether <see cref="Observer"/> is called at all. False by default.</summary>
        public static bool Enabled { get; set; }

        /// <summary>
        /// Called with the message type and the boxed message, on the publishing thread, at the
        /// moment of publish — not at flush.
        /// </summary>
        /// <remarks>
        /// <b>At publish rather than at flush, deliberately.</b> Flush order is once a frame and
        /// batched; publish order is what actually happened, and "what happened, in what order" is
        /// the only question this window exists to answer.
        /// </remarks>
        public static Action<Type, object> Observer { get; set; }

        internal static void Observe<T>(in T message) where T : struct
        {
            if (!Enabled)
            {
                return;
            }

            var observer = Observer;

            if (observer == null)
            {
                return;
            }

            // The one box, and only while somebody is watching.
            observer(typeof(T), message);
        }

        /// <summary>Turns it off and drops the observer. Called when the window closes.</summary>
        public static void Stop()
        {
            Enabled = false;
            Observer = null;
        }
    }
}
#endif

using System.Collections.Generic;

namespace Engine.Messages
{
    /// <summary>
    /// Everything of type <typeparamref name="T"/> pending for one phase. Systems read it;
    /// <b>only the Runner empties it</b>, because several systems may read the same queue in a
    /// single tick (Docs/02).
    /// </summary>
    /// <remarks>
    /// Double-buffered. Publishes land in the back buffer, and the Runner swaps at the start of
    /// the phase, so the contents are frozen for the whole phase: every system sees exactly the
    /// same messages no matter where it sits in the feature list. A publish from inside the
    /// phase is therefore visible on the <em>next</em> tick — which is the guarantee in Docs/02
    /// §1, and it cannot hold with a single buffer.
    /// <para>
    /// One instance per (message type, phase), shared by everyone who injects it.
    /// </para>
    /// </remarks>
    public sealed class SubscriberQueue<T> : ISubscriberQueue<T> where T : struct
    {
        private List<T> _front = new List<T>();
        private List<T> _back = new List<T>();

        internal SubscriberQueue(MessagePhase phase) => Phase = phase;

        /// <summary>The phase whose start swaps this queue.</summary>
        public MessagePhase Phase { get; }

        public int Count => _front.Count;

        public T this[int index] => _front[index];

        /// <summary>Allocation-free iteration: <c>foreach (var message in _requests)</c>.</summary>
        public Enumerator GetEnumerator() => new Enumerator(_front);

        internal void Enqueue(in T message) => _back.Add(message);

        internal void Swap()
        {
            _front.Clear();

            var spent = _front;
            _front = _back;
            _back = spent;
        }

        /// <summary>Struct enumerator — <c>foreach</c> binds to this by shape, without boxing.</summary>
        public struct Enumerator
        {
            private readonly List<T> _messages;
            private int _index;

            internal Enumerator(List<T> messages)
            {
                _messages = messages;
                _index = -1;
            }

            public T Current => _messages[_index];

            public bool MoveNext() => ++_index < _messages.Count;
        }
    }
}

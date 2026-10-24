using System;
using System.Collections.Generic;
using Engine.Diagnostics;

namespace Engine.Net
{
    /// <summary>
    /// Wraps a transport and makes its <b>outgoing</b> link bad on purpose: delay, jitter, loss
    /// (Docs/05). Both ends wrap theirs, so each direction gets its own conditions.
    /// </summary>
    /// <remarks>
    /// Runs on its own clock — the sum of <see cref="Poll"/> deltas — so it is deterministic under
    /// a seed and never reads <c>Time</c>. Delayed packets leave on the first poll at or after their
    /// due time, in due order.
    /// </remarks>
    public sealed class LinkConditioner : INetTransport
    {
        private readonly INetTransport _inner;
        private readonly Random _random;
        private readonly List<Pending> _pending = new List<Pending>();

        private double _now;

        public LinkConditioner(INetTransport inner, LinkConditions conditions, int seed)
        {
            Guard.ThrowIfNull(inner, nameof(inner));

            _inner = inner;
            _random = new Random(seed);
            Conditions = conditions;
        }

        /// <summary>May change at any time; packets already in flight keep their due time.</summary>
        public LinkConditions Conditions { get; set; }

        /// <summary>Packets dropped so far.</summary>
        public int Dropped { get; private set; }

        /// <summary>Packets handed to the conditioner so far.</summary>
        public int Sent { get; private set; }

        public bool IsServer => _inner.IsServer;

        public void Start()
        {
            _inner.Start();
        }

        public void Poll(float deltaTime, List<NetEvent> into)
        {
            _now += deltaTime;
            Flush();
            _inner.Poll(deltaTime, into);
        }

        public void Send(int peer, byte[] data, int length)
        {
            Sent++;
            var conditions = Conditions;

            if (conditions.LossPercent > 0f && _random.NextDouble() * 100.0 < conditions.LossPercent)
            {
                Dropped++;
                return;
            }

            var delayMs = conditions.LatencyMs + (_random.NextDouble() * 2.0 - 1.0) * conditions.JitterMs;
            if (delayMs <= 0.0)
            {
                _inner.Send(peer, data, length);
                return;
            }

            var copy = new byte[length];
            Buffer.BlockCopy(data, 0, copy, 0, length);
            _pending.Add(new Pending(_now + delayMs / 1000.0, peer, copy));
        }

        public void Dispose()
        {
            _pending.Clear();
            _inner.Dispose();
        }

        private void Flush()
        {
            while (true)
            {
                var next = -1;
                for (var i = 0; i < _pending.Count; i++)
                {
                    // The epsilon absorbs float deltas summing to just under a whole delay.
                    if (_pending[i].Due <= _now + 1e-6 &&(next < 0 || _pending[i].Due < _pending[next].Due))
                    {
                        next = i;
                    }
                }

                if (next < 0)
                {
                    return;
                }

                var packet = _pending[next];
                _pending.RemoveAt(next);
                _inner.Send(packet.Peer, packet.Data, packet.Data.Length);
            }
        }

        private readonly struct Pending
        {
            public readonly double Due;
            public readonly int Peer;
            public readonly byte[] Data;

            public Pending(double due, int peer, byte[] data)
            {
                Due = due;
                Peer = peer;
                Data = data;
            }
        }
    }
}

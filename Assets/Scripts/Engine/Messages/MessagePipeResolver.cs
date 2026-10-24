using System;
using System.Collections.Generic;
using System.Reflection;
using Engine.DI;
using Engine.Diagnostics;

namespace Engine.Messages
{
    /// <summary>
    /// Resolves the three bus endpoints from the shape of the requested type:
    /// <c>IPublisher&lt;T&gt;</c>, <c>ISubscriber&lt;T&gt;</c>, and
    /// <c>SubscriberQueue&lt;T&gt;</c> / <c>ISubscriberQueue&lt;T&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Pipes are created lazily by the bus on first request, so injecting an endpoint is what
    /// brings its pipe into existence — including registering a queue for phase clearing.
    /// <para>
    /// <b>Queues resolve to the FixedUpdate phase</b>, because a field type cannot express one.
    /// A message type that belongs in another phase is registered explicitly in Bootstrap —
    /// <c>container.Register(bus.Queue&lt;T&gt;(MessagePhase.Update))</c> — which wins because
    /// <see cref="InstanceResolver"/> is always consulted first.
    /// </para>
    /// </remarks>
    public sealed class MessagePipeResolver : IResolver
    {
        private static readonly MethodInfo PublisherFactory = Find(nameof(MessageBus.Publisher));
        private static readonly MethodInfo SubscriberFactory = Find(nameof(MessageBus.Subscriber));
        private static readonly MethodInfo QueueFactory = Find(nameof(MessageBus.Queue));

        private readonly MessageBus _bus;
        private readonly Dictionary<Type, object> _endpoints = new Dictionary<Type, object>();
        private readonly object[] _queueArguments = { MessagePhase.FixedUpdate };

        public MessagePipeResolver(MessageBus bus)
        {
            Guard.ThrowIfNull(bus, nameof(bus));

            _bus = bus;
        }

        public bool TryResolve(Type type, out object value)
        {
            // Reflection once per endpoint type, then never again.
            if (_endpoints.TryGetValue(type, out value)) return true;

            if (!type.IsGenericType)
            {
                value = null;
                return false;
            }

            var definition = type.GetGenericTypeDefinition();
            MethodInfo factory;
            object[] arguments = null;

            if (definition == typeof(IPublisher<>))
            {
                factory = PublisherFactory;
            }
            else if (definition == typeof(ISubscriber<>))
            {
                factory = SubscriberFactory;
            }
            else if (definition == typeof(SubscriberQueue<>) || definition == typeof(ISubscriberQueue<>))
            {
                factory = QueueFactory;
                arguments = _queueArguments;
            }
            else
            {
                value = null;
                return false;
            }

            value = factory.MakeGenericMethod(type.GetGenericArguments()[0]).Invoke(_bus, arguments);
            _endpoints.Add(type, value);
            return true;
        }

        public override string ToString() => $"MessagePipeResolver ({_endpoints.Count} endpoints)";

        private static MethodInfo Find(string name) =>
            typeof(MessageBus).GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
    }
}

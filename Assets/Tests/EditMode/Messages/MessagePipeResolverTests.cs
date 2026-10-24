using System;
using Engine.DI;
using Engine.Messages;
using NUnit.Framework;

namespace Engine.Tests.EditMode.Messages
{
    [TestFixture]
    public sealed class MessagePipeResolverTests
    {
        private MessageBus _bus;
        private MessagePipeResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _bus = new MessageBus();
            _resolver = new MessagePipeResolver(_bus);
        }

        [Test]
        public void Resolves_APublisher()
        {
            Assert.That(_resolver.TryResolve(typeof(IPublisher<Planted>), out var value), Is.True);
            Assert.That(value, Is.InstanceOf<IPublisher<Planted>>());
        }

        [Test]
        public void Resolves_ASubscriber()
        {
            Assert.That(_resolver.TryResolve(typeof(ISubscriber<Planted>), out var value), Is.True);
            Assert.That(value, Is.InstanceOf<ISubscriber<Planted>>());
        }

        [Test]
        public void Resolves_AConcreteQueue_BoundToFixedUpdate()
        {
            Assert.That(_resolver.TryResolve(typeof(SubscriberQueue<Planted>), out var value), Is.True);
            Assert.That(((SubscriberQueue<Planted>)value).Phase, Is.EqualTo(MessagePhase.FixedUpdate));
        }

        [Test]
        public void Resolves_TheQueueInterfaceToTheSameInstance()
        {
            _resolver.TryResolve(typeof(SubscriberQueue<Planted>), out var concrete);
            _resolver.TryResolve(typeof(ISubscriberQueue<Planted>), out var asInterface);

            Assert.That(asInterface, Is.SameAs(concrete));
        }

        [Test]
        public void ResolvedEndpoints_AreTheBusesOwn()
        {
            _resolver.TryResolve(typeof(IPublisher<Planted>), out var publisher);
            _resolver.TryResolve(typeof(SubscriberQueue<Planted>), out var queue);

            Assert.That(publisher, Is.SameAs(_bus.Publisher<Planted>()));
            Assert.That(queue, Is.SameAs(_bus.Queue<Planted>()));
        }

        [Test]
        public void RepeatedRequests_ReturnTheSameEndpoint()
        {
            _resolver.TryResolve(typeof(IPublisher<Planted>), out var first);
            _resolver.TryResolve(typeof(IPublisher<Planted>), out var second);

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void Injecting_AnEndpointCreatesItsPipe()
        {
            Assert.That(_bus.PipeCount, Is.Zero);

            _resolver.TryResolve(typeof(SubscriberQueue<Planted>), out _);

            Assert.That(_bus.PipeCount, Is.EqualTo(1),
                "injecting a queue is what registers it with the bus for phase clearing");
        }

        [Test]
        public void Passes_OnUnrelatedTypes()
        {
            Assert.That(_resolver.TryResolve(typeof(string), out _), Is.False);
            Assert.That(_resolver.TryResolve(typeof(int), out _), Is.False);
            Assert.That(_resolver.TryResolve(typeof(System.Collections.Generic.List<Planted>), out _), Is.False);
        }

        [Test]
        public void WiresAWholeSystemThroughTheContainer()
        {
            var container = new Container();
            container.AddResolver(_resolver);

            var system = container.New<PlantingSystem>();

            system.Publish(new Planted(3));
            _bus.BeginPhase(MessagePhase.FixedUpdate);

            Assert.That(system.Requests.Count, Is.EqualTo(1));
            Assert.That(system.Requests[0].Id, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new MessagePipeResolver(null));
        }

#pragma warning disable 0649

        private sealed class PlantingSystem
        {
            [Inject] private IPublisher<Planted> _publisher;
            [Inject] private SubscriberQueue<Planted> _requests;

            public SubscriberQueue<Planted> Requests => _requests;

            public void Publish(in Planted message) => _publisher.Publish(message);
        }

#pragma warning restore 0649

        private readonly struct Planted
        {
            public readonly int Id;

            public Planted(int id) => Id = id;
        }
    }
}

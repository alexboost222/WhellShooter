using System;
using System.Collections.Generic;
using Engine.Disposables;
using Engine.Messages;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Engine.Tests.EditMode.Messages
{
    [TestFixture]
    public sealed class MessageBusTests
    {
        private MessageBus _bus;

        [SetUp]
        public void SetUp() => _bus = new MessageBus();

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        // --- queues: the double-buffer contract ----------------------------------------

        [Test]
        public void Queue_IsEmptyUntilThePhaseBegins()
        {
            var queue = _bus.Queue<Planted>();
            _bus.Publisher<Planted>().Publish(new Planted(1));

            Assert.That(queue.Count, Is.Zero, "a publish lands in the back buffer");

            _bus.BeginPhase(MessagePhase.FixedUpdate);

            Assert.That(queue.Count, Is.EqualTo(1));
            Assert.That(queue[0].Id, Is.EqualTo(1));
        }

        [Test]
        public void Queue_PublishDuringItsOwnPhase_IsVisibleNextTick_NotThisOne()
        {
            var queue = _bus.Queue<Planted>();
            _bus.BeginPhase(MessagePhase.FixedUpdate);

            // A system publishing mid-phase: nobody in this phase may see it, whatever the
            // system order — that is the whole point of swapping rather than appending.
            _bus.Publisher<Planted>().Publish(new Planted(7));
            Assert.That(queue.Count, Is.Zero);

            _bus.BeginPhase(MessagePhase.FixedUpdate);

            Assert.That(queue.Count, Is.EqualTo(1));
            Assert.That(queue[0].Id, Is.EqualTo(7));
        }

        [Test]
        public void Queue_IsClearedByTheNextPhase_NotByReading()
        {
            var queue = _bus.Queue<Planted>();
            _bus.Publisher<Planted>().Publish(new Planted(1));
            _bus.BeginPhase(MessagePhase.FixedUpdate);

            // Several systems read the same queue in one tick; reading consumes nothing.
            Assert.That(queue.Count, Is.EqualTo(1));
            Assert.That(queue.Count, Is.EqualTo(1));

            _bus.BeginPhase(MessagePhase.FixedUpdate);

            Assert.That(queue.Count, Is.Zero);
        }

        [Test]
        public void Queue_PreservesPublishOrder()
        {
            var queue = _bus.Queue<Planted>();
            var publisher = _bus.Publisher<Planted>();
            publisher.Publish(new Planted(1));
            publisher.Publish(new Planted(2));
            publisher.Publish(new Planted(3));

            _bus.BeginPhase(MessagePhase.FixedUpdate);

            Assert.That(Drain(queue), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Queue_SameTypeAndPhase_IsOneSharedInstance()
        {
            Assert.That(_bus.Queue<Planted>(), Is.SameAs(_bus.Queue<Planted>()));
        }

        [Test]
        public void Queue_DefaultsToFixedUpdate()
        {
            Assert.That(_bus.Queue<Planted>().Phase, Is.EqualTo(MessagePhase.FixedUpdate));
        }

        [Test]
        public void Queue_PhasesAreIndependentAndBothReceiveThePublish()
        {
            var fixedQueue = _bus.Queue<Planted>(MessagePhase.FixedUpdate);
            var updateQueue = _bus.Queue<Planted>(MessagePhase.Update);
            _bus.Publisher<Planted>().Publish(new Planted(1));

            _bus.BeginPhase(MessagePhase.FixedUpdate);

            Assert.That(fixedQueue.Count, Is.EqualTo(1));
            Assert.That(updateQueue.Count, Is.Zero, "the Update queue waits for its own phase");

            _bus.BeginPhase(MessagePhase.Update);

            Assert.That(updateQueue.Count, Is.EqualTo(1));
        }

        [Test]
        public void Queue_CreatedAfterAPublish_DoesNotSeeIt()
        {
            _bus.Publisher<Planted>().Publish(new Planted(1));
            var queue = _bus.Queue<Planted>();

            _bus.BeginPhase(MessagePhase.FixedUpdate);

            Assert.That(queue.Count, Is.Zero, "transient events, not a replayable log");
        }

        [Test]
        public void Queue_ForeachUsesTheStructEnumerator()
        {
            var queue = _bus.Queue<Planted>();
            _bus.Publisher<Planted>().Publish(new Planted(5));
            _bus.BeginPhase(MessagePhase.FixedUpdate);

            var seen = 0;
            foreach (var message in queue) seen += message.Id;

            Assert.That(seen, Is.EqualTo(5));
        }

        // --- push subscribers ------------------------------------------------------------

        [Test]
        public void Publish_DoesNotCallHandlersSynchronously()
        {
            var called = false;
            _bus.Subscriber<Planted>().Subscribe(_ => called = true);

            _bus.Publisher<Planted>().Publish(new Planted(1));

            Assert.That(called, Is.False, "Publish must never execute foreign code");

            _bus.FlushPush();

            Assert.That(called, Is.True);
        }

        [Test]
        public void FlushPush_DeliversInPublishOrderToEveryHandler()
        {
            var first = new List<int>();
            var second = new List<int>();
            _bus.Subscriber<Planted>().Subscribe(m => first.Add(m.Id));
            _bus.Subscriber<Planted>().Subscribe(m => second.Add(m.Id));

            _bus.Publisher<Planted>().Publish(new Planted(1));
            _bus.Publisher<Planted>().Publish(new Planted(2));
            _bus.FlushPush();

            Assert.That(first, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(second, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void FlushPush_DeliversEachMessageOnce()
        {
            var count = 0;
            _bus.Subscriber<Planted>().Subscribe(_ => count++);
            _bus.Publisher<Planted>().Publish(new Planted(1));

            _bus.FlushPush();
            _bus.FlushPush();

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void PublishFromInsideAHandler_ArrivesOnTheNextFlush()
        {
            var received = new List<int>();
            var publisher = _bus.Publisher<Planted>();
            _bus.Subscriber<Planted>().Subscribe(m =>
            {
                received.Add(m.Id);
                if (m.Id == 1) publisher.Publish(new Planted(2));
            });

            publisher.Publish(new Planted(1));
            _bus.FlushPush();

            Assert.That(received, Is.EqualTo(new[] { 1 }), "a re-entrant publish must not extend this flush");

            _bus.FlushPush();

            Assert.That(received, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            var count = 0;
            var subscription = _bus.Subscriber<Planted>().Subscribe(_ => count++);

            subscription.Dispose();
            _bus.Publisher<Planted>().Publish(new Planted(1));
            _bus.FlushPush();

            Assert.That(count, Is.Zero);
        }

        [Test]
        public void Unsubscribe_IsIdempotent()
        {
            var subscription = _bus.Subscriber<Planted>().Subscribe(_ => { });

            Assert.DoesNotThrow(() =>
            {
                subscription.Dispose();
                subscription.Dispose();
            });
        }

        [Test]
        public void Unsubscribe_DuringFlush_DoesNotDisturbTheOthers()
        {
            var order = new List<string>();
            IDisposable subscription = null;
            subscription = _bus.Subscriber<Planted>().Subscribe(_ =>
            {
                order.Add("first");
                // ReSharper disable once AccessToModifiedClosure
                subscription.Dispose();
            });
            _bus.Subscriber<Planted>().Subscribe(_ => order.Add("second"));

            _bus.Publisher<Planted>().Publish(new Planted(1));
            _bus.FlushPush();

            Assert.That(order, Is.EqualTo(new[] { "first", "second" }));
        }

        [Test]
        public void Subscription_ComposesWithADisposableBag()
        {
            var bag = new DisposableBag();
            var count = 0;
            _bus.Subscriber<Planted>().Subscribe(_ => count++).AddTo(bag);

            bag.Dispose();
            _bus.Publisher<Planted>().Publish(new Planted(1));
            _bus.FlushPush();

            Assert.That(count, Is.Zero);
        }

        [Test]
        public void ThrowingHandler_IsLoggedAndSkipped_OthersStillRun()
        {
            LogAssert.ignoreFailingMessages = true;
            var reached = false;
            _bus.Subscriber<Planted>().Subscribe(_ => throw new InvalidOperationException("boom"));
            _bus.Subscriber<Planted>().Subscribe(_ => reached = true);

            _bus.Publisher<Planted>().Publish(new Planted(1));

            Assert.DoesNotThrow(() => _bus.FlushPush());
            Assert.That(reached, Is.True);
        }

        [Test]
        public void SubscriberAddedAfterAPublish_DoesNotReceiveIt()
        {
            _bus.Publisher<Planted>().Publish(new Planted(1));
            var count = 0;
            _bus.Subscriber<Planted>().Subscribe(_ => count++);

            _bus.FlushPush();

            Assert.That(count, Is.Zero, "transient events: no sticky or latest-value delivery");
        }

        [Test]
        public void Subscribe_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _bus.Subscriber<Planted>().Subscribe(null));
        }

        // --- pipes -------------------------------------------------------------------------

        [Test]
        public void Pipes_AreCreatedLazilyAndSharedPerType()
        {
            Assert.That(_bus.PipeCount, Is.Zero);

            _bus.Publisher<Planted>();
            _bus.Subscriber<Planted>();
            _bus.Queue<Planted>();

            Assert.That(_bus.PipeCount, Is.EqualTo(1), "one pipe per message type, whichever end you ask for");

            _bus.Publisher<Harvested>();

            Assert.That(_bus.PipeCount, Is.EqualTo(2));
        }

        [Test]
        public void MessageTypes_DoNotBleedIntoEachOther()
        {
            var planted = _bus.Queue<Planted>();
            var harvested = _bus.Queue<Harvested>();

            _bus.Publisher<Planted>().Publish(new Planted(1));
            _bus.BeginPhase(MessagePhase.FixedUpdate);

            Assert.That(planted.Count, Is.EqualTo(1));
            Assert.That(harvested.Count, Is.Zero);
        }

        [Test]
        public void PublishingWithNoConsumers_IsHarmless()
        {
            Assert.DoesNotThrow(() =>
            {
                _bus.Publisher<Planted>().Publish(new Planted(1));
                _bus.FlushPush();
                _bus.BeginPhase(MessagePhase.FixedUpdate);
            });
        }

        private static int[] Drain(SubscriberQueue<Planted> queue)
        {
            var ids = new List<int>();
            foreach (var message in queue) ids.Add(message.Id);
            return ids.ToArray();
        }

        private readonly struct Planted
        {
            public readonly int Id;

            public Planted(int id) => Id = id;
        }

        private readonly struct Harvested
        {
            public readonly int Id;

            public Harvested(int id) => Id = id;
        }
    }
}

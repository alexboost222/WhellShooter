using System;
using System.Collections.Generic;
using Engine.Messages;
using Engine.Messages.Diagnostics;
using NUnit.Framework;

namespace Engine.Tests.EditMode.Messages
{
    /// <summary>
    /// The bus's diagnostics: the publish tap, the pipe table, and publishing a runtime-typed message.
    /// </summary>
    /// <remarks>
    /// <b><see cref="Watching_does_not_change_what_the_bus_does"/> is the reason this fixture
    /// exists</b>, and it was written before the hook was. The natural way to watch a bus is to
    /// subscribe to it, and here that would flip <c>MessagePipe</c>'s "buffer only if somebody is
    /// listening" rule for every type nothing subscribes to — turning a list that is always empty
    /// into one that fills and drains once a frame, for as long as the window is open. This asserts
    /// that observing changes nothing, so that a later refactor back to the obvious design fails
    /// here rather than in somebody's debugging session.
    /// </remarks>
    [TestFixture]
    public sealed class MessageBusMonitorTests
    {
        private MessageBus _bus;
        private List<MessagePipeInfo> _pipes;

        [SetUp]
        public void SetUp()
        {
            _bus = new MessageBus();
            _pipes = new List<MessagePipeInfo>();
            MessageMonitor.Stop();
        }

        [TearDown]
        public void TearDown() => MessageMonitor.Stop();

        [Test]
        public void Watching_does_not_change_what_the_bus_does()
        {
            var seen = 0;
            MessageMonitor.Observer = (_, _) => seen++;
            MessageMonitor.Enabled = true;

            // Nothing subscribes, so nothing should be buffered.
            _bus.Publisher<Ping>().Publish(new Ping { Value = 1 });

            _bus.CollectPipes(_pipes);

            Assert.That(seen, Is.EqualTo(1), "the tap saw it");
            Assert.That(_pipes[0].PendingPush, Is.Zero,
                "and buffering is exactly what it was without the tap — a subscriber would have changed this");
        }

        [Test]
        public void While_off_the_observer_is_not_called()
        {
            var seen = 0;
            MessageMonitor.Observer = (_, _) => seen++;
            MessageMonitor.Enabled = false;

            _bus.Publisher<Ping>().Publish(new Ping());

            Assert.That(seen, Is.Zero);
        }

        [Test]
        public void The_observer_gets_the_type_and_the_value()
        {
            Type type = null;
            object boxed = null;

            MessageMonitor.Observer = (t, o) =>
            {
                type = t;
                boxed = o;
            };

            MessageMonitor.Enabled = true;
            _bus.Publisher<Ping>().Publish(new Ping { Value = 42 });

            Assert.That(type, Is.EqualTo(typeof(Ping)));
            Assert.That(((Ping)boxed).Value, Is.EqualTo(42));
        }

        [Test]
        public void Published_counts_even_with_nobody_listening()
        {
            _bus.Publisher<Ping>().Publish(new Ping());
            _bus.Publisher<Ping>().Publish(new Ping());

            _bus.CollectPipes(_pipes);

            Assert.That(_pipes[0].PublishedCount, Is.EqualTo(2));
            Assert.That(_pipes[0].HandlerCount, Is.Zero);
        }

        [Test]
        public void Handlers_and_queues_are_counted()
        {
            using (_bus.Subscriber<Ping>().Subscribe(_ => { }))
            {
                _bus.Queue<Ping>(MessagePhase.FixedUpdate);
                _bus.Queue<Ping>(MessagePhase.Update);

                _bus.CollectPipes(_pipes);

                Assert.That(_pipes[0].HandlerCount, Is.EqualTo(1));
                Assert.That(_pipes[0].QueueCount, Is.EqualTo(2));
            }
        }

        [Test]
        public void Pipes_are_sorted_by_type_name()
        {
            _bus.Publisher<Pong>().Publish(new Pong());
            _bus.Publisher<Ping>().Publish(new Ping());

            _bus.CollectPipes(_pipes);

            Assert.That(_pipes[0].MessageType, Is.EqualTo(typeof(Ping)));
            Assert.That(_pipes[1].MessageType, Is.EqualTo(typeof(Pong)));
        }

        [Test]
        public void A_boxed_publish_reaches_a_typed_subscriber()
        {
            var got = 0;

            using (_bus.Subscriber<Ping>().Subscribe(p => got = p.Value))
            {
                _bus.PublishBoxed(typeof(Ping), new Ping { Value = 7 });
                _bus.FlushPush();
            }

            Assert.That(got, Is.EqualTo(7));
        }

        [Test]
        public void A_boxed_publish_of_the_wrong_type_is_refused()
        {
            Assert.Throws<InvalidOperationException>(() => _bus.PublishBoxed(typeof(Ping), new Pong()));
        }

        [Test]
        public void The_callers_list_is_cleared()
        {
            _pipes.Add(new MessagePipeInfo(typeof(Ping), 0, 0, 0, 0));
            _bus.CollectPipes(_pipes);

            Assert.That(_pipes, Is.Empty);
        }

        private struct Ping
        {
            public int Value;
        }

        private struct Pong
        {
        }
    }
}

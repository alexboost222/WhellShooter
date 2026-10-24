using System.Collections.Generic;
using Engine.Net;
using NUnit.Framework;

namespace Engine.Tests.Net
{
    public sealed class LinkConditionerTests
    {
        private const float Dt = 0.02f;

        [Test]
        public void Ideal_DeliversOnTheNextPoll()
        {
            var network = new LoopbackNetwork();
            var server = network.CreateServer();
            var client = new LinkConditioner(network.CreateClient(), new LinkConditions(), 1);
            var events = Connect(server, client);

            client.Send(0, new byte[] { 7 }, 1);
            server.Poll(Dt, events);

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Data, Is.EqualTo(new byte[] { 7 }));
        }

        [Test]
        public void Latency_HoldsThePacketUntilDue()
        {
            var network = new LoopbackNetwork();
            var server = network.CreateServer();
            var client = new LinkConditioner(network.CreateClient(), new LinkConditions(100f, 0f, 0f), 1);
            var events = Connect(server, client);

            client.Send(0, new byte[] { 1 }, 1);

            // 100 ms = 5 polls of 20 ms; the packet leaves on the fifth.
            for (var i = 0; i < 4; i++)
            {
                client.Poll(Dt, events);
                server.Poll(Dt, events);
                Assert.That(events, Is.Empty, $"poll {i + 1}");
            }

            client.Poll(Dt, events);
            server.Poll(Dt, events);
            Assert.That(events.Count, Is.EqualTo(1));
        }

        [Test]
        public void Loss_DropsAboutThatShare()
        {
            var network = new LoopbackNetwork();
            network.CreateServer();
            var client = new LinkConditioner(network.CreateClient(), new LinkConditions(0f, 0f, 10f), 42);

            for (var i = 0; i < 10000; i++)
            {
                client.Send(0, new byte[1], 1);
            }

            Assert.That(client.Dropped, Is.InRange(850, 1150));
        }

        [Test]
        public void Jitter_ReordersButLosesNothing()
        {
            var network = new LoopbackNetwork();
            var server = network.CreateServer();
            var client = new LinkConditioner(network.CreateClient(), new LinkConditions(100f, 50f, 0f), 3);
            var events = Connect(server, client);

            for (var i = 0; i < 200; i++)
            {
                client.Send(0, new[] { (byte)i }, 1);
                client.Poll(Dt, events);
                server.Poll(Dt, events);
            }

            for (var i = 0; i < 20; i++)
            {
                client.Poll(Dt, events);
                server.Poll(Dt, events);
            }

            var reordered = false;
            for (var i = 1; i < events.Count; i++)
            {
                reordered |= events[i].Data[0] < events[i - 1].Data[0];
            }

            Assert.That(events.Count, Is.EqualTo(200));
            Assert.That(reordered, Is.True);
        }

        [Test]
        public void SameSeed_SameFate()
        {
            Assert.That(DropPattern(5), Is.EqualTo(DropPattern(5)));
        }

        private static List<NetEvent> Connect(INetTransport server, INetTransport client)
        {
            var events = new List<NetEvent>();
            server.Start();
            client.Start();
            client.Poll(0f, events);
            server.Poll(0f, events);
            events.Clear();
            return events;
        }

        private static string DropPattern(int seed)
        {
            var network = new LoopbackNetwork();
            network.CreateServer();
            var client = new LinkConditioner(network.CreateClient(), new LinkConditions(0f, 0f, 30f), seed);
            var pattern = new char[64];
            for (var i = 0; i < pattern.Length; i++)
            {
                var before = client.Dropped;
                client.Send(0, new byte[1], 1);
                pattern[i] = client.Dropped > before ? 'x' : '.';
            }

            return new string(pattern);
        }
    }
}

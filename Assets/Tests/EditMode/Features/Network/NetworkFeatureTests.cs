using System;
using System.Collections.Generic;
using Engine.Net;
using Features.Network.Components;
using Features.Network.Configs;
using Features.Network.Constants;
using Features.Network.Enums;
using Features.Network.Providers;
using Features.Network.Utils;
using NUnit.Framework;
using Scellecs.Morpeh;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Network
{
    /// <summary>
    /// A server and two clients as three headless worlds over loopback + link conditioner (Docs/05).
    /// The bar: <b>no input ever misses its tick</b>, every consumed input is the one the client
    /// sampled for that tick, client ticks never go back, and nothing is taken more than
    /// <see cref="NetConfig.MaxInputLeadMs"/> ahead.
    /// </summary>
    public sealed class NetworkFeatureTests
    {
        private const float Dt = 0.02f;
        private const int Clients = 2;

        private NetConfig _config;
        private NetNode _server;
        private NetNode[] _clients;
        private Dictionary<int, PlayerInput>[] _produced;
        private int[] _lastProduced;
        private int[] _consumed;
        private int[] _maxArrivalSlack;
        private int _maxLead;
        private float[] _clockRates;
        private float[] _clockDebt;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<NetConfig>();
            _config.StatsIntervalTicks = 0;
            _maxLead = SlackMath.MaxLeadTicks(_config.MaxInputLeadMs, Dt);
        }

        [TearDown]
        public void TearDown()
        {
            if (_clients != null)
            {
                foreach (var client in _clients)
                {
                    client.Dispose();
                }
            }

            _server?.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [TestCase(0f, 0f, 0f, TestName = "Ideal")]
        [TestCase(25f, 5f, 1f, TestName = "Ping50_Jitter5_Loss1")]
        [TestCase(50f, 10f, 3f, TestName = "Ping100_Jitter10_Loss3")]
        [TestCase(100f, 25f, 5f, TestName = "Ping200_Jitter25_Loss5")]
        [TestCase(150f, 50f, 10f, TestName = "Ping300_Jitter50_Loss10")]
        public void EveryInputArrivesInTime(float latencyMs, float jitterMs, float lossPercent)
        {
            Build(new LinkConditions(latencyMs, jitterMs, lossPercent));

            Run(180 * 50);

            AssertDelivery(160 * 50);
        }

        [Test]
        public void Ping300_Jitter50_Loss10_ManySeeds([NUnit.Framework.Range(1, 5)] int seed)
        {
            Build(new LinkConditions(150f, 50f, 10f), seed * 7919);

            Run(180 * 50);

            AssertDelivery(160 * 50);
        }

        [Test]
        public void ClientClocksDrift_OneFastOneSlow([Values(0f, 10f)] float lossPercent)
        {
            Build(new LinkConditions(150f, 50f, lossPercent));
            _clockRates[0] = 1.01f;
            _clockRates[1] = 0.99f;

            Run(180 * 50);

            AssertDelivery(160 * 50);
        }

        [Test]
        public void LatencyRamp_UpAndDown_NoMissAndLeadShrinksBack()
        {
            Build(new LinkConditions());
            var worst = new LinkConditions(150f, 50f, 10f);

            Run(10 * 50);
            Ramp(new LinkConditions(), worst, 60 * 50);
            Run(30 * 50);
            Ramp(worst, new LinkConditions(), 60 * 50);
            Run(60 * 50);

            AssertDelivery(200 * 50);
            for (var i = 0; i < Clients; i++)
            {
                var sync = Sync(_clients[i]);
                Assert.That(sync.LastSlack, Is.LessThanOrEqualTo(sync.NeedSlack + _config.Hysteresis + 2),
                    $"client {i} did not shed its lead once the link got better");
            }
        }

        [Test]
        public void TooMuchLead_IsRejectedThenShed_WithoutMisses()
        {
            _config.StartSlack = 40;
            Build(new LinkConditions(20f, 0f, 0f));

            Run(60 * 50);

            var rejected = 0;
            foreach (var entity in _server.World.Filter.With<ServerClientStats>().Build())
            {
                rejected += _server.World.GetStash<ServerClientStats>().Get(entity).Rejected;
            }

            Assert.That(rejected, Is.GreaterThan(0));
            AssertDelivery(50 * 50);
            for (var i = 0; i < Clients; i++)
            {
                var sync = Sync(_clients[i]);
                Assert.That(sync.LastSlack, Is.LessThanOrEqualTo(sync.NeedSlack + _config.Hysteresis + 2),
                    $"client {i} kept its excess lead");
            }
        }

        [Test]
        public void ClientLeaving_DestroysItsServerEntity()
        {
            Build(new LinkConditions());
            Run(100);

            _clients[0].Dispose();
            _clients = new[] { _clients[1] };
            _server.Tick(Dt);
            _server.Tick(Dt);

            Assert.That(_server.World.Filter.With<ServerClient>().Build().GetLengthSlow(), Is.EqualTo(1));
        }

        private void Build(LinkConditions conditions, int seed = 0)
        {
            var network = new LoopbackNetwork();
            _server = new NetNode(network.CreateServer(), _config, conditions, seed + 1000);
            _clients = new NetNode[Clients];
            _produced = new Dictionary<int, PlayerInput>[Clients];
            _lastProduced = new int[Clients];
            _consumed = new int[Clients];
            _maxArrivalSlack = new int[Clients];
            _clockRates = new[] { 1f, 1f };
            _clockDebt = new float[Clients];

            for (var i = 0; i < Clients; i++)
            {
                _clients[i] = new NetNode(network.CreateClient(), _config, conditions, seed + 2000 + i,
                    new ScriptedInput(i));
                _produced[i] = new Dictionary<int, PlayerInput>();
                _lastProduced[i] = NetConstants.NoTick;
            }
        }

        private void Ramp(LinkConditions from, LinkConditions to, int ticks)
        {
            for (var t = 0; t < ticks; t++)
            {
                var k = (float)t / ticks;
                var conditions = new LinkConditions(
                    Mathf.Lerp(from.LatencyMs, to.LatencyMs, k),
                    Mathf.Lerp(from.JitterMs, to.JitterMs, k),
                    Mathf.Lerp(from.LossPercent, to.LossPercent, k));

                _server.Link.Conditions = conditions;
                foreach (var client in _clients)
                {
                    client.Link.Conditions = conditions;
                }

                Run(1);
            }
        }

        private void Run(int ticks)
        {
            for (var t = 0; t < ticks; t++)
            {
                _server.Tick(Dt);
                RecordConsumed();

                // A client whose clock runs fast steps twice now and then, a slow one skips a step —
                // what separate processes' fixed steps do relative to each other.
                for (var i = 0; i < _clients.Length; i++)
                {
                    _clockDebt[i] += _clockRates[i];
                    while (_clockDebt[i] >= 1f)
                    {
                        _clockDebt[i] -= 1f;
                        _clients[i].Tick(Dt);
                        RecordProduced(i);
                    }
                }
            }
        }

        private void RecordProduced(int i)
        {
            var clock = _clients[i].World.GetStash<NetClock>();
            var tick = clock.Get(_clients[i].World.Filter.With<NetClock>().Build().First()).Tick;
            var sync = Sync(_clients[i]);

            Assert.That(tick, Is.GreaterThanOrEqualTo(_lastProduced[i]), $"client {i} tick went back");

            if (sync.State == ClientSyncState.Running)
            {
                for (var t = Math.Max(_lastProduced[i] + 1, sync.FirstTick); t <= tick; t++)
                {
                    _produced[i][t] = sync.Inputs[t & NetConstants.RingMask];
                }
            }

            _lastProduced[i] = tick;
        }

        private void RecordConsumed()
        {
            var world = _server.World;
            var clients = world.GetStash<ServerClient>();
            var consumed = world.GetStash<ConsumedInput>();

            foreach (var entity in world.Filter.With<ServerClient>().With<ConsumedInput>().Build())
            {
                ref var client = ref clients.Get(entity);
                ref var input = ref consumed.Get(entity);
                if (input.Tick == NetConstants.NoTick || input.Missed || client.Peer >= Clients)
                {
                    continue;
                }

                var peer = client.Peer;
                Assert.That(_produced[peer].TryGetValue(input.Tick, out var expected), Is.True,
                    $"server consumed tick {input.Tick} client {peer} never produced");
                Assert.That(input.Input, Is.EqualTo(expected), $"client {peer} tick {input.Tick}");

                var arrivalSlack = input.Tick - client.ArrivalTicks[input.Tick & NetConstants.RingMask];
                _maxArrivalSlack[peer] = Math.Max(_maxArrivalSlack[peer], arrivalSlack);
                _consumed[peer]++;
            }
        }

        private void AssertDelivery(int minConsumed)
        {
            var world = _server.World;
            var stats = world.GetStash<ServerClientStats>();
            var clients = world.GetStash<ServerClient>();

            foreach (var entity in world.Filter.With<ServerClient>().With<ServerClientStats>().Build())
            {
                var peer = clients.Get(entity).Peer;
                ref var s = ref stats.Get(entity);
                var sync = Sync(_clients[peer]);

                TestContext.WriteLine(
                    $"client {peer}: consumed {s.Consumed} missed {s.Missed} late {s.Late} rejected {s.Rejected} " +
                    $"max arrival slack {_maxArrivalSlack[peer]} last slack {sync.LastSlack} need {sync.NeedSlack} " +
                    $"jumps {sync.Jumps} (+{sync.JumpedTicks}) holds {sync.Holds} sent {_clients[peer].Link.Sent} " +
                    $"dropped {_clients[peer].Link.Dropped}");

                Assert.That(s.Missed, Is.Zero, $"client {peer} missed inputs");
                Assert.That(s.Consumed, Is.GreaterThanOrEqualTo(minConsumed), $"client {peer} consumed");
                Assert.That(_consumed[peer], Is.EqualTo(s.Consumed), $"client {peer} verified");
                Assert.That(_maxArrivalSlack[peer], Is.LessThanOrEqualTo(_maxLead),
                    $"client {peer} input taken too far ahead");
            }
        }

        private static ClientSync Sync(NetNode node)
        {
            return node.World.GetStash<ClientSync>().Get(node.World.Filter.With<ClientSync>().Build().First());
        }

        /// <summary>A distinct, varying input per tick, so a wrong or shifted tick cannot compare equal.</summary>
        private sealed class ScriptedInput : IPlayerInputSource
        {
            private readonly int _salt;
            private int _count;

            public ScriptedInput(int salt)
            {
                _salt = salt;
            }

            public PlayerInput Sample()
            {
                _count++;
                return new PlayerInput
                {
                    MoveX = (sbyte)(_count * 7 + _salt),
                    MoveY = (sbyte)(_count * 13),
                    LookYaw = (ushort)(_count * 31 + _salt * 1000),
                    LookPitch = (short)(_count >> 3),
                    Buttons = (byte)(_count & 3),
                    ViewDelay = (byte)(_count % 5),
                };
            }
        }
    }
}

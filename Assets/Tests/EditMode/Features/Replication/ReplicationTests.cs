using Engine.Net;
using Features.Bots.Messages;
using Features.CharacterFeature.Components;
using Features.Map.Messages;
using Features.Map.Systems;
using Features.Network.Components;
using Features.Network.Providers;
using Features.Network.Utils;
using Features.PlayerFeature.Components;
using Features.PlayerFeature.Systems;
using Features.Replication.Components;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;
using CharacterTag = Features.CharacterFeature.Components.Character;
using PlayerTag = Features.PlayerFeature.Components.Player;

namespace Game.Tests.EditMode.Features.Replication
{
    /// <summary>
    /// The whole game as two processes over a loopback link: a dedicated server and one client, each
    /// with the real feature list (Docs/05 §Replication and prediction).
    /// </summary>
    public sealed class ReplicationTests
    {
        private const int Seed = 4242;

        private GameNode _server;
        private GameNode _client;
        private ScriptedInput _input;
        private GameObject _spawn;
        private float3 _spawnFeet;

        [SetUp]
        public void SetUp()
        {
            var network = new LoopbackNetwork();
            _server = new GameNode(network.CreateServer());
            _input = new ScriptedInput();
            _client = new GameNode(network.CreateClient(), _input);

            // What the scene's providers do on the server.
            _server.Bus.Publisher<GenerateMap>().Publish(new GenerateMap { Seed = Seed });
            _server.Tick();
            _spawnFeet = _server.Container.Resolve<MapStorage>().Layout.Spawns[0].Position;
            _spawn = new GameObject("Spawn");
            _spawn.transform.position = _spawnFeet;
            _server.Container.Resolve<PlayerSpawnPointStorage>().Register(_spawn.transform);
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _server.Dispose();
            Object.DestroyImmediate(_spawn);
        }

        [Test]
        public void TheClient_BuildsTheServersArena_AndGetsItsOwnBody()
        {
            Run(250);

            Assert.That(_client.Container.Resolve<MapStorage>().Layout?.Seed, Is.EqualTo(Seed));
            var local = _client.World.Filter.With<PlayerTag>().With<NetEntity>().Build();
            Assert.That(local.GetLengthSlow(), Is.EqualTo(1));
            var server = _server.World.Filter.With<NetPlayer>().With<NetEntity>().Build();
            Assert.That(server.GetLengthSlow(), Is.EqualTo(1));
            Assert.That(Get<NetEntity>(_client, local.First()).Id, Is.EqualTo(Get<NetEntity>(_server, server.First()).Id));
        }

        [Test]
        public void TheLocalBody_MovesAtOnce_AndEndsWhereTheServerHasIt()
        {
            Run(250);
            var local = _client.World.Filter.With<PlayerTag>().With<CharacterMotion>().Build().First();
            var start = Get<CharacterMotion>(_client, local).Position;

            _input.Forward = true;
            _client.Tick();
            _client.Tick();
            Assert.That(math.distance(Get<CharacterMotion>(_client, local).Position.xz, start.xz), Is.GreaterThan(0f),
                "predicted: the body moves on the client's own tick, before any snapshot answers");

            Run(100);
            _input.Forward = false;
            Run(100);

            var body = _server.World.Filter.With<NetPlayer>().With<CharacterMotion>().Build().First();
            var serverFeet = Get<CharacterMotion>(_server, body).Position;
            var clientFeet = Get<CharacterMotion>(_client, local).Position;
            Assert.That(math.distance(serverFeet.xz, _spawnFeet.xz), Is.GreaterThan(1f), "the server moved it too");
            Assert.That(math.distance(clientFeet, serverFeet), Is.LessThan(0.05f));
        }

        [Test]
        public void ServerBots_AreRemoteBodies_OnTheClient()
        {
            _server.Bus.Publisher<SpawnBots>().Publish(new SpawnBots { Count = 2 });

            Run(250);

            var remote = _client.World.Filter.With<RemoteBody>().With<CharacterMotion>().Build();
            Assert.That(remote.GetLengthSlow(), Is.EqualTo(2));
            Assert.That(_client.World.Filter.With<CharacterTag>().With<RemoteBody>().Build().GetLengthSlow(), Is.Zero,
                "nothing simulates a remote body on the client");
        }

        private void Run(int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                _server.Tick();
                _client.Tick();
            }
        }

        private static T Get<T>(GameNode node, Entity entity) where T : struct, IComponent
        {
            return node.World.GetStash<T>().Get(entity);
        }

        private sealed class ScriptedInput : IPlayerInputSource
        {
            public bool Forward;

            public PlayerInput Sample()
            {
                return new PlayerInput
                {
                    MoveY = (sbyte)(Forward ? 127 : 0),
                    LookYaw = LookCodec.EncodeYaw(0f),
                };
            }
        }
    }
}

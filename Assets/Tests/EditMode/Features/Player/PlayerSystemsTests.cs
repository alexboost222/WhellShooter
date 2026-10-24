using Features.CharacterFeature.Components;
using Features.PlayerFeature.Components;
using Features.PlayerFeature.Configs;
using Features.PlayerFeature.Systems;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;
using CharacterTag = Features.CharacterFeature.Components.Character;
using PlayerTag = Features.PlayerFeature.Components.Player;

namespace Game.Tests.EditMode.Features.Player
{
    public sealed class PlayerSystemsTests
    {
        private EcsTestWorld _world;
        private PlayerSpawnPointStorage _spawnPoints;

        [SetUp]
        public void SetUp()
        {
            _world = new EcsTestWorld(ScriptableObject.CreateInstance<PlayerConfig>());
            _spawnPoints = _world.AddInitializer(new PlayerSpawnPointStorage());
            _world.AddInitializer(_world.Container.New<PlayerSettings>());
            _world.Add<PlayerSpawnSystem>();
            _world.Add<PlayerLookSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void NoSpawnPoint_NoPlayer()
        {
            _world.Tick(0.02f);

            Assert.That(_world.World.Filter.With<PlayerTag>().Build().GetLengthSlow(), Is.EqualTo(0));
        }

        [Test]
        public void ASpawnPoint_GetsExactlyOneBody_OnItFacingItsWay()
        {
            var point = _world.CreateObject("Spawn").transform;
            point.SetPositionAndRotation(new Vector3(3f, 1f, -2f), Quaternion.Euler(0f, 90f, 0f));
            _spawnPoints.Register(point);

            _world.Tick(0.02f, 3);

            var players = _world.World.Filter
                .With<PlayerTag>()
                .With<CharacterTag>()
                .With<CharacterIntent>()
                .Build();
            Assert.That(players.GetLengthSlow(), Is.EqualTo(1));

            var body = players.First();
            Assert.That(_world.Get<CharacterMotion>(body).Position, Is.EqualTo(new float3(3f, 1f, -2f)));
            Assert.That(_world.Get<Look>(body).Yaw, Is.EqualTo(math.radians(90f)).Within(1e-4f));
        }

        [Test]
        public void Look_SpendsThePointerDelta_ClampsPitch_AndEmptiesTheDelta()
        {
            var body = _world.World.CreateEntity();
            _world.Set(body, new Look());
            _world.Set(body, new PlayerLookInput { Delta = new float2(100f, 10000f) });

            _world.Tick(0.016f);

            var look = _world.Get<Look>(body);
            Assert.That(look.Yaw, Is.EqualTo(math.radians(100f * 0.08f)).Within(1e-4f), "default sensitivity 0.08°/px");
            Assert.That(look.Pitch, Is.EqualTo(math.radians(-85f)).Within(1e-4f), "pointer up looks up, clamped");
            Assert.That(_world.Get<PlayerLookInput>(body).Delta, Is.EqualTo(float2.zero));
        }
    }
}

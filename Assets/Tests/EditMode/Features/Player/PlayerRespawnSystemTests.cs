using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Features.PlayerFeature.Components;
using Features.PlayerFeature.Configs;
using Features.PlayerFeature.Systems;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;
using PlayerTag = Features.PlayerFeature.Components.Player;

namespace Game.Tests.EditMode.Features.Player
{
    public sealed class PlayerRespawnSystemTests
    {
        private EcsTestWorld _world;
        private PlayerSpawnPointStorage _spawnPoints;

        [SetUp]
        public void SetUp()
        {
            _world = new EcsTestWorld(ScriptableObject.CreateInstance<PlayerConfig>());
            _spawnPoints = _world.AddInitializer(new PlayerSpawnPointStorage());
            _world.Add<PlayerRespawnSystem>();

            var point = _world.CreateObject("Spawn").transform;
            point.SetPositionAndRotation(new Vector3(3f, 0f, -2f), Quaternion.Euler(0f, 90f, 0f));
            _spawnPoints.Register(point);
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void ADeadPlayer_ComesBackOnTheSpawnPoint_AfterTheDelay()
        {
            var body = _world.World.CreateEntity();
            _world.Set(body, new PlayerTag());
            _world.Set(body, new Dead());

            _world.Tick(0.5f, 6);
            Assert.That(_world.Has<ReviveRequest>(body), Is.False, "the default delay is 3 s");

            // Exactly the tick the delay runs out (Combat, not here, would remove Dead in the same tick).
            _world.Tick(0.5f);

            Assert.That(_world.Has<ReviveRequest>(body), Is.True);
            Assert.That(_world.Has<PlayerRespawn>(body), Is.False);
            var teleport = _world.Get<CharacterTeleportRequest>(body);
            Assert.That(teleport.Position, Is.EqualTo(new float3(3f, 0f, -2f)));
            Assert.That(teleport.Yaw, Is.EqualTo(math.radians(90f)).Within(1e-4f));
        }

        [Test]
        public void ALivePlayer_IsLeftAlone()
        {
            var body = _world.World.CreateEntity();
            _world.Set(body, new PlayerTag());

            _world.Tick(0.5f, 10);

            Assert.That(_world.Has<ReviveRequest>(body), Is.False);
            Assert.That(_world.Has<PlayerRespawn>(body), Is.False);
        }
    }
}

using Features.CharacterFeature.Components;
using Features.Network.Systems;
using Features.Network.Utils;
using Features.PlayerFeature.Components;
using Features.PlayerFeature.Configs;
using Features.PlayerFeature.Systems;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Player
{
    public sealed class PlayerSystemsTests
    {
        private EcsTestWorld _world;
        private PlayerSpawnPointStorage _spawnPoints;
        private LocalInputBuffer _buffer;

        [SetUp]
        public void SetUp()
        {
            _world = new EcsTestWorld(ScriptableObject.CreateInstance<PlayerConfig>());
            _spawnPoints = _world.AddInitializer(new PlayerSpawnPointStorage());
            _buffer = _world.Container.Register(new LocalInputBuffer());
            _world.AddInitializer(_world.Container.New<PlayerSettings>());
            _world.Add<PlayerLookSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void NoSpawnPoint_NoSpawn()
        {
            Assert.That(_spawnPoints.TryGetSpawn(0, out _, out _), Is.False);
        }

        [Test]
        public void SpawnSlots_StandSideBySide_FacingThePointsWay()
        {
            var point = _world.CreateObject("Spawn").transform;
            point.SetPositionAndRotation(new Vector3(3f, 1f, -2f), Quaternion.Euler(0f, 90f, 0f));
            _spawnPoints.Register(point);

            Assert.That(_spawnPoints.TryGetSpawn(0, out var first, out var yaw), Is.True);
            _spawnPoints.TryGetSpawn(1, out var second, out _);
            _spawnPoints.TryGetSpawn(2, out var third, out _);

            Assert.That(math.distance(first, new float3(3f, 1f, -2f)), Is.LessThan(1e-4f), "slot 0 on the point");
            Assert.That(yaw, Is.EqualTo(math.radians(90f)).Within(1e-4f));
            // Facing +X, the point's right is −Z.
            Assert.That(math.distance(second, new float3(3f, 1f, -3.5f)), Is.LessThan(1e-4f));
            Assert.That(math.distance(third, new float3(3f, 1f, -0.5f)), Is.LessThan(1e-4f));
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

        [Test]
        public void Look_IsWhatTheNextTickSends()
        {
            var body = _world.World.CreateEntity();
            _world.Set(body, new Look());
            _world.Set(body, new PlayerLookInput { Delta = new float2(-500f, 200f) });

            _world.Tick(0.016f);

            var look = _world.Get<Look>(body);
            var sent = _buffer.Sample();
            Assert.That(LookCodec.DecodeYaw(sent.LookYaw), Is.EqualTo(look.Yaw).Within(1e-4f));
            Assert.That(LookCodec.DecodePitch(sent.LookPitch), Is.EqualTo(look.Pitch).Within(1e-4f));
        }
    }
}

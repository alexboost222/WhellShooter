using Features.CharacterFeature.Components;
using Features.CharacterFeature.Systems;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Game.Tests.EditMode.Features.Character
{
    public sealed class CharacterTeleportSystemTests
    {
        private EcsTestWorld _world;

        [SetUp]
        public void SetUp()
        {
            _world = new EcsTestWorld();
            _world.Add<CharacterTeleportSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void MovesTheBody_StopsIt_LevelsItsView_AndIsConsumed()
        {
            var body = _world.World.CreateEntity();
            _world.Set(body, new CharacterMotion
            {
                Position = new float3(1f, 2f, 3f),
                PreviousPosition = new float3(0f, 2f, 3f),
                Velocity = new float3(4f, -5f, 6f),
                Grounded = true,
            });
            _world.Set(body, new Look { Yaw = 1f, Pitch = 0.5f });
            _world.Set(body, new CharacterTeleportRequest { Position = new float3(10f, 0f, -4f), Yaw = 2f });

            _world.Tick(0.02f);

            var motion = _world.Get<CharacterMotion>(body);
            Assert.That(motion.Position, Is.EqualTo(new float3(10f, 0f, -4f)));
            Assert.That(motion.PreviousPosition, Is.EqualTo(motion.Position), "no interpolated sweep across the map");
            Assert.That(motion.Velocity, Is.EqualTo(float3.zero));
            Assert.That(motion.Grounded, Is.False);
            Assert.That(_world.Get<Look>(body).Yaw, Is.EqualTo(2f).Within(1e-5f));
            Assert.That(_world.Get<Look>(body).Pitch, Is.EqualTo(0f));
            Assert.That(_world.Has<CharacterTeleportRequest>(body), Is.False);
        }
    }
}

using Features.CharacterFeature.Components;
using Features.CharacterFeature.Utils;
using NUnit.Framework;
using Unity.Mathematics;

namespace Game.Tests.EditMode.Features.Character
{
    public sealed class CharacterMovementTests
    {
        [Test]
        public void Sprint_NeedsTheButtonAndForwardInput_AndYieldsToAimAndFire()
        {
            var forward = new float2(0f, 1f);

            Assert.That(CharacterMovement.WantsSprint(true, forward, false, false), Is.True);
            Assert.That(CharacterMovement.WantsSprint(false, forward, false, false), Is.False);
            Assert.That(CharacterMovement.WantsSprint(true, new float2(1f, 0f), false, false), Is.False, "strafing");
            Assert.That(CharacterMovement.WantsSprint(true, -forward, false, false), Is.False, "backwards");
            Assert.That(CharacterMovement.WantsSprint(true, forward, true, false), Is.False, "aiming");
            Assert.That(CharacterMovement.WantsSprint(true, forward, false, true), Is.False, "firing");
        }

        [Test]
        public void Horizontal_IsSteeredByYaw()
        {
            var facingRight = new Look { Yaw = math.radians(90f) }.Heading;

            var velocity = CharacterMovement.Horizontal(float3.zero, new float2(0f, 1f), facingRight, 5f, 1000f, 0.1f);

            Assert.That(velocity.x, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(velocity.z, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(velocity.y, Is.EqualTo(0f));
        }

        [Test]
        public void Horizontal_ChangesByAtMostAccelerationTimesDelta()
        {
            var velocity = CharacterMovement.Horizontal(float3.zero, new float2(0f, 1f), quaternion.identity, 5f, 10f, 0.1f);

            Assert.That(math.length(velocity), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Horizontal_IgnoresVerticalVelocity()
        {
            var velocity = CharacterMovement.Horizontal(new float3(0f, -9f, 0f), float2.zero, quaternion.identity, 5f, 10f, 0.1f);

            Assert.That(velocity, Is.EqualTo(float3.zero));
        }

        [Test]
        public void JumpSpeed_PeaksAtTheJumpHeight()
        {
            var speed = CharacterMovement.JumpSpeed(1.1f, 22f);

            Assert.That(speed * speed / (2f * 22f), Is.EqualTo(1.1f).Within(1e-4f));
        }

        [Test]
        public void WrapAngle_KeepsAnglesInMinusPiToPi()
        {
            Assert.That(LookMath.WrapAngle(math.radians(190f)), Is.EqualTo(math.radians(-170f)).Within(1e-4f));
            Assert.That(LookMath.WrapAngle(math.radians(-190f)), Is.EqualTo(math.radians(170f)).Within(1e-4f));
            Assert.That(LookMath.WrapAngle(0.5f), Is.EqualTo(0.5f).Within(1e-6f));
        }

        [Test]
        public void DeltaAngle_TakesTheShortWayAcrossTheWrap()
        {
            Assert.That(LookMath.DeltaAngle(math.radians(170f), math.radians(-170f)),
                Is.EqualTo(math.radians(20f)).Within(1e-4f));
        }

        [Test]
        public void Look_ForwardFollowsUnitysConvention()
        {
            var down = new Look { Pitch = math.radians(90f) }.Forward;
            var right = new Look { Yaw = math.radians(90f) }.Forward;

            Assert.That(down.y, Is.EqualTo(-1f).Within(1e-4f), "positive pitch looks down");
            Assert.That(right.x, Is.EqualTo(1f).Within(1e-4f), "positive yaw turns right");
        }
    }
}

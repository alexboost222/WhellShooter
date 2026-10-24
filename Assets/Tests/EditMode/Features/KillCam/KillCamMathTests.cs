using System.Collections.Generic;
using Features.KillCam.Systems;
using Features.KillCam.Utils;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Game.Tests.EditMode.Features.KillCam
{
    public sealed class KillCamMathTests
    {
        private static readonly EntityId Bot = new EntityId(3, 0);
        private static readonly EntityId Other = new EntityId(4, 0);

        [Test]
        public void Sample_LerpsBetweenFrames_AndTakesFlagsFromTheLater()
        {
            var from = new List<BodySample> { new BodySample { Id = Bot, Position = new float3(0f, 0f, 0f), Yaw = 0f } };
            var to = new List<BodySample> { new BodySample { Id = Bot, Position = new float3(2f, 0f, 0f), Yaw = 1f, Dead = true } };

            Assert.That(KillCamMath.TrySample(from, to, Bot, 0.25f, out var sample), Is.True);
            Assert.That(sample.Position.x, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(sample.Yaw, Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(sample.Dead, Is.True);
        }

        [Test]
        public void Sample_OfABodyMissingFromAFrame_UsesTheOther_AndOfAnAbsentOne_Fails()
        {
            var from = new List<BodySample> { new BodySample { Id = Bot, Position = new float3(1f, 0f, 0f) } };
            var to = new List<BodySample>();

            Assert.That(KillCamMath.TrySample(from, to, Bot, 0.5f, out var sample), Is.True);
            Assert.That(sample.Position.x, Is.EqualTo(1f));
            Assert.That(KillCamMath.TrySample(from, to, Other, 0.5f, out _), Is.False);
        }

        [Test]
        public void LerpAngle_GoesTheShortWayRound()
        {
            var a = math.PI - 0.1f;
            var b = -math.PI + 0.1f;

            var mid = KillCamMath.LerpAngle(a, b, 0.5f);

            Assert.That(math.abs(math.abs(mid) - math.PI), Is.LessThan(1e-4f), "through ±π, not through 0");
        }

        [Test]
        public void EyeRotation_MatchesTheLookConvention()
        {
            var forward = math.mul(KillCamMath.EyeRotation(math.PI / 2f, 0f), new float3(0f, 0f, 1f));
            var down = math.mul(KillCamMath.EyeRotation(0f, 0.5f), new float3(0f, 0f, 1f));

            Assert.That(forward.x, Is.EqualTo(1f).Within(1e-4f), "+yaw turns right");
            Assert.That(down.y, Is.LessThan(0f), "+pitch looks down");
        }

        [Test]
        public void LookAt_StraightDown_IsStillDefined()
        {
            var forward = math.mul(KillCamMath.LookAt(new float3(0f, 5f, 0f), float3.zero), new float3(0f, 0f, 1f));

            Assert.That(forward.y, Is.EqualTo(-1f).Within(1e-4f));
        }
    }
}

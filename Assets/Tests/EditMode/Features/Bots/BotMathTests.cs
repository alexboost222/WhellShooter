using Features.Bots.Components;
using Features.Bots.Enums;
using Features.Bots.Utils;
using NUnit.Framework;
using Unity.Mathematics;

namespace Game.Tests.EditMode.Features.Bots
{
    /// <summary>The bot brain's pure parts: angles and frames, response curves, the bidding rule.</summary>
    public sealed class BotMathTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void YawAndPitch_FollowTheBodyConvention()
        {
            Assert.That(BotMath.YawOf(new float3(0f, 0f, 1f)), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(BotMath.YawOf(new float3(1f, 0f, 0f)), Is.EqualTo(math.PI / 2f).Within(Tolerance));
            // +pitch looks down.
            Assert.That(BotMath.PitchOf(new float3(0f, -1f, 1f)), Is.EqualTo(math.PI / 4f).Within(Tolerance));
        }

        [Test]
        public void DeltaAngle_TakesTheShortWayRound()
        {
            Assert.That(BotMath.DeltaAngle(math.radians(170f), math.radians(-170f)),
                Is.EqualTo(math.radians(20f)).Within(Tolerance));
        }

        [Test]
        public void LocalMove_IsRelativeToTheFacing()
        {
            // Facing +X, walking +X is straight ahead; walking −Z is to the right.
            var yaw = math.PI / 2f;
            var ahead = BotMath.ToLocalMove(new float3(1f, 0f, 0f), yaw);
            var right = BotMath.ToLocalMove(new float3(0f, 0f, -1f), yaw);

            Assert.That(ahead.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(ahead.y, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(right.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(right.y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void TurnTowards_NeverExceedsTheTurnSpeed_AndArrives()
        {
            var yaw = 0f;
            var target = math.radians(90f);
            var maxSpeed = math.radians(180f);

            yaw = BotMath.TurnTowards(yaw, target, 100f, maxSpeed, 0.1f);
            Assert.That(yaw, Is.EqualTo(math.radians(18f)).Within(Tolerance));

            for (var i = 0; i < 200; i++)
            {
                yaw = BotMath.TurnTowards(yaw, target, 10f, maxSpeed, 0.02f);
            }

            Assert.That(yaw, Is.EqualTo(target).Within(Tolerance));
        }

        [Test]
        public void RangeCurve_PeaksAtThePreferredRange_AndKeepsItsFloor()
        {
            Assert.That(BotScores.Range(14f, 14f, 0.6f), Is.EqualTo(1f));
            Assert.That(BotScores.Range(7f, 14f, 0.6f), Is.LessThan(1f).And.GreaterThanOrEqualTo(0.6f));
            Assert.That(BotScores.Range(100f, 14f, 0.6f), Is.EqualTo(0.6f));
        }

        [Test]
        public void Fade_GoesFromOneToZero()
        {
            Assert.That(BotScores.Fade(0f, 6f), Is.EqualTo(1f));
            Assert.That(BotScores.Fade(3f, 6f), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(BotScores.Fade(9f, 6f), Is.EqualTo(0f));
        }

        [Test]
        public void Offer_HighestBidWins_AndTheRunningActionGetsItsCommitment()
        {
            var brain = new BotBrain { Action = BotAction.Wander, Commitment = 0.2f };
            brain.Offer(BotAction.Wander, 0.1f);
            brain.Offer(BotAction.Chase, 0.25f);

            // 0.1 + 0.2 commitment beats 0.25.
            Assert.That(brain.BestAction, Is.EqualTo(BotAction.Wander));

            brain.Offer(BotAction.Attack, 0.8f);
            Assert.That(brain.BestAction, Is.EqualTo(BotAction.Attack));
        }
    }
}

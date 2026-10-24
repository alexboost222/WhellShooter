using Engine.Motion;
using NUnit.Framework;
using Unity.Mathematics;

namespace Engine.Tests.EditMode.Motion
{
    public sealed class SpringsTests
    {
        private const float Tolerance = 1e-4f;

        [TestCase(0.3f)]
        [TestCase(1f)]
        [TestCase(2.5f)]
        public void OneBigStep_LandsWhereManySmallStepsDo(float damping)
        {
            var big = new Spring3 { Value = new float3(1f, -2f, 0.5f), Velocity = new float3(3f, 0f, -1f) };
            var small = big;

            Springs.Step(ref big, float3.zero, 4f, damping, 0.1f);
            for (var i = 0; i < 10; i++)
            {
                Springs.Step(ref small, float3.zero, 4f, damping, 0.01f);
            }

            AssertClose(big.Value, small.Value);
            AssertClose(big.Velocity, small.Velocity);
        }

        [TestCase(0.3f)]
        [TestCase(1f)]
        [TestCase(2.5f)]
        public void SettlesOnTheTarget(float damping)
        {
            var spring = new Spring3 { Value = new float3(5f), Velocity = new float3(-2f) };
            var target = new float3(1f, 2f, 3f);

            for (var i = 0; i < 600; i++)
            {
                Springs.Step(ref spring, target, 3f, damping, 1f / 60f);
            }

            AssertClose(spring.Value, target);
            AssertClose(spring.Velocity, float3.zero);
        }

        [Test]
        public void Underdamped_Overshoots_CriticallyDamped_DoesNot()
        {
            var under = new Spring3 { Value = new float3(1f) };
            var critical = new Spring3 { Value = new float3(1f) };
            var underMin = 1f;
            var criticalMin = 1f;

            for (var i = 0; i < 300; i++)
            {
                Springs.Step(ref under, float3.zero, 2f, 0.3f, 1f / 60f);
                Springs.Step(ref critical, float3.zero, 2f, 1f, 1f / 60f);
                underMin = math.min(underMin, under.Value.x);
                criticalMin = math.min(criticalMin, critical.Value.x);
            }

            Assert.That(underMin, Is.LessThan(-0.1f));
            Assert.That(criticalMin, Is.GreaterThanOrEqualTo(-Tolerance));
        }

        [Test]
        public void AnImpulse_SwingsOutAndComesBack()
        {
            var spring = new Spring3();
            spring.Impulse(new float3(0f, 0f, -1f));

            var peak = 0f;
            for (var i = 0; i < 120; i++)
            {
                Springs.Step(ref spring, float3.zero, 8f, 0.5f, 1f / 120f);
                peak = math.min(peak, spring.Value.z);
            }

            Assert.That(peak, Is.LessThan(-0.005f));
            Assert.That(math.abs(spring.Value.z), Is.LessThan(1e-3f));
        }

        [Test]
        public void ZeroFrequency_SnapsToTheTarget()
        {
            var spring = new Spring3 { Value = new float3(4f), Velocity = new float3(9f) };

            Springs.Step(ref spring, new float3(1f), 0f, 0.5f, 0.016f);

            AssertClose(spring.Value, new float3(1f));
            AssertClose(spring.Velocity, float3.zero);
        }

        [Test]
        public void ZeroDelta_ChangesNothing()
        {
            var spring = new Spring3 { Value = new float3(4f), Velocity = new float3(9f) };

            Springs.Step(ref spring, float3.zero, 5f, 0.5f, 0f);

            AssertClose(spring.Value, new float3(4f));
            AssertClose(spring.Velocity, new float3(9f));
        }

        [Test]
        public void Damp_IsFrameRateIndependent()
        {
            var big = Springs.Damp(10f, 0f, 5f, 0.2f);
            var small = 10f;
            for (var i = 0; i < 20; i++)
            {
                small = Springs.Damp(small, 0f, 5f, 0.01f);
            }

            Assert.That(big, Is.EqualTo(small).Within(Tolerance));
        }

        [Test]
        public void MoveTowards_StopsAtTheTargetAndNeverPassesIt()
        {
            Assert.That(Springs.MoveTowards(0f, 1f, 0.3f), Is.EqualTo(0.3f).Within(Tolerance));
            Assert.That(Springs.MoveTowards(0.9f, 1f, 0.3f), Is.EqualTo(1f));
            Assert.That(Springs.MoveTowards(0f, -1f, 0.3f), Is.EqualTo(-0.3f).Within(Tolerance));

            AssertClose(Springs.MoveTowards(float3.zero, new float3(3f, 4f, 0f), 1f), new float3(0.6f, 0.8f, 0f));
            AssertClose(Springs.MoveTowards(float3.zero, new float3(0.1f, 0f, 0f), 1f), new float3(0.1f, 0f, 0f));
        }

        private static void AssertClose(float3 actual, float3 expected)
        {
            Assert.That(math.distance(actual, expected), Is.LessThan(Tolerance), $"{actual} vs {expected}");
        }
    }
}

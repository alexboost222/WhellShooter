using Engine.Runtime;
using NUnit.Framework;

namespace Engine.Tests.Runtime
{
    public sealed class TickAccumulatorTests
    {
        [Test]
        public void ShortFrames_TickOnlyOnceAWholeTickAccumulates()
        {
            var clock = new TickAccumulator(0.02f, 5);

            Assert.That(clock.Advance(0.008f), Is.EqualTo(0));
            Assert.That(clock.Advance(0.008f), Is.EqualTo(0));
            Assert.That(clock.Advance(0.008f), Is.EqualTo(1));
            Assert.That(clock.Accumulated, Is.EqualTo(0.004).Within(1e-6));
        }

        [Test]
        public void LongFrame_RunsSeveralTicks()
        {
            var clock = new TickAccumulator(0.02f, 5);

            Assert.That(clock.Advance(0.065f), Is.EqualTo(3));
        }

        [Test]
        public void Hitch_IsCappedAndTheExcessDropped()
        {
            var clock = new TickAccumulator(0.02f, 5);

            Assert.That(clock.Advance(1f), Is.EqualTo(5));
            Assert.That(clock.DroppedTicks, Is.EqualTo(45));
            Assert.That(clock.Advance(0.02f), Is.EqualTo(1), "nothing carried into the next frame");
        }

        [Test]
        public void OverManyFrames_TicksMatchElapsedTime()
        {
            var clock = new TickAccumulator(0.02f, 5);
            var ticks = 0;

            for (var i = 0; i < 6000; i++)
            {
                ticks += clock.Advance(1f / 60f);
            }

            Assert.That(ticks, Is.InRange(4999, 5000));
        }
    }
}

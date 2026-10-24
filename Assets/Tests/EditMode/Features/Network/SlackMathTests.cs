using Features.Network.Utils;
using NUnit.Framework;

namespace Game.Tests.EditMode.Features.Network
{
    public sealed class SlackMathTests
    {
        [TestCase(0f, 1)]
        [TestCase(0.01f, 2)]
        [TestCase(0.05f, 4)]
        [TestCase(0.10f, 5)]
        [TestCase(0.30f, 10)]
        public void NeedSlack_CoversTheLossRate(float loss, int expected)
        {
            Assert.That(SlackMath.NeedSlack(loss, 1e-6f, 1, 10), Is.EqualTo(expected));
        }

        [Test]
        public void MaxLeadTicks_300msAt50Hz_Is15()
        {
            Assert.That(SlackMath.MaxLeadTicks(300f, 0.02f), Is.EqualTo(15));
        }
    }
}

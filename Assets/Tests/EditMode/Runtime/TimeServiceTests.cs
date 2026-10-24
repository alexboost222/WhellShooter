using Engine.Messages;
using Engine.Runtime;
using NUnit.Framework;

namespace Engine.Tests.EditMode.Runtime
{
    public sealed class TimeServiceTests
    {
        private const float Fixed = 0.02f;

        [Test]
        public void BeforeTheFirstFixedTick_AlphaIsOne()
        {
            var time = new TimeService();
            time.AdvanceFrame(0.016f);

            Assert.That(time.InterpolationAlpha, Is.EqualTo(1f));
        }

        [Test]
        public void Alpha_IsTheFrameTimeSinceTheLastFixedTick()
        {
            var time = new TimeService();

            // Unity's order within a frame: the frame's fixed ticks, then Update.
            time.AdvanceFixed(Fixed);
            time.AdvanceFrame(0.025f);
            Assert.That(time.InterpolationAlpha, Is.EqualTo(0.25f).Within(1e-4f), "25 ms in, one tick run → 5 ms past it");

            time.AdvanceFrame(0.01f);
            Assert.That(time.InterpolationAlpha, Is.EqualTo(0.75f).Within(1e-4f), "no tick this frame → 15 ms past it");

            time.AdvanceFixed(Fixed);
            time.AdvanceFrame(0.01f);
            Assert.That(time.InterpolationAlpha, Is.EqualTo(0.25f).Within(1e-4f));
        }

        [Test]
        public void AHitch_NeverPushesAlphaOutOfRange()
        {
            var time = new TimeService();
            time.AdvanceFixed(Fixed);
            time.AdvanceFrame(0.5f);

            Assert.That(time.InterpolationAlpha, Is.EqualTo(1f));

            time.AdvanceFixed(Fixed);
            time.AdvanceFixed(Fixed);
            time.AdvanceFrame(0.001f);

            Assert.That(time.InterpolationAlpha, Is.InRange(0f, 1f));
        }

        [Test]
        public void Counts_FixedTicksAndRemembersDeltas()
        {
            var time = new TimeService();
            time.AdvanceFixed(Fixed);
            time.AdvanceFixed(Fixed);
            time.AdvanceFrame(0.033f);

            Assert.That(time.FixedTicks, Is.EqualTo(2));
            Assert.That(time.FixedDeltaTime, Is.EqualTo(Fixed));
            Assert.That(time.FrameDeltaTime, Is.EqualTo(0.033f));
        }

        [Test]
        public void Install_PutsOneStepInFixedUpdateAndOneInUpdate()
        {
            var runner = new Runner(new MessageBus());
            var time = new TimeService();

            time.Install(runner);
            runner.Initialize();
            runner.FixedTick(Fixed);
            runner.FrameTick(0.03f);

            Assert.That(runner.StepCount(RunnerPhase.FixedUpdate), Is.EqualTo(1));
            Assert.That(runner.StepCount(RunnerPhase.Update), Is.EqualTo(1));
            Assert.That(time.FixedTicks, Is.EqualTo(1));
            Assert.That(time.InterpolationAlpha, Is.EqualTo(0.5f).Within(1e-4f));
        }
    }
}

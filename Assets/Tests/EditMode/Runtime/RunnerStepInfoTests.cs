using System;
using System.Collections.Generic;
using Engine.Messages;
using Engine.Runtime;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Engine.Tests.EditMode.Runtime
{
    /// <summary>
    /// <see cref="Runner.CollectSteps"/> — and specifically the one fact nothing else can carry.
    /// </summary>
    /// <remarks>
    /// <see cref="AStepThatKeepsThrowingIsReportedAsDisabled"/> is the reason the seam exists. A
    /// dropped step simply stops running, so from outside it is indistinguishable from one that
    /// runs instantly — and the difference is a feature that has silently stopped working.
    /// </remarks>
    [TestFixture]
    public sealed class RunnerStepInfoTests
    {
        private const float Delta = 1f / 30f;

        private Runner _runner;
        private List<RunnerStepInfo> _steps;

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        [SetUp]
        public void SetUp()
        {
            _runner = new Runner(new MessageBus());
            _steps = new List<RunnerStepInfo>();
        }

        [Test]
        public void StepsComeOutInInsertionOrderWithinAKind()
        {
            _runner.Add(RunnerPhase.FixedUpdate, RunnerStep.Create("first", _ => { }));
            _runner.Add(RunnerPhase.FixedUpdate, RunnerStep.Create("second", _ => { }));
            _runner.Add(RunnerPhase.Update, RunnerStep.Create("late", _ => { }));

            _runner.CollectSteps(_steps);

            var fixedSteps = _steps.FindAll(s => s.Kind == RunnerPhase.FixedUpdate.ToString());

            Assert.That(fixedSteps[0].Name, Is.EqualTo("first"));
            Assert.That(fixedSteps[0].Order, Is.Zero);
            Assert.That(fixedSteps[1].Name, Is.EqualTo("second"));
            Assert.That(fixedSteps[1].Order, Is.EqualTo(1));
        }

        /// <summary>
        /// Initializers and the physics hook are not phases, which is why <see cref="RunnerStepInfo.Kind"/>
        /// is a string. Every position has to come out or a reader is lied to by omission.
        /// </summary>
        [Test]
        public void InitializersAndHooksAreListedToo()
        {
            _runner.AddInitializer(RunnerStep.Create("storage", _ => { }));
            _runner.PhysicsHook = RunnerStep.Create("physics", _ => { });

            _runner.CollectSteps(_steps);

            var kinds = _steps.ConvertAll(s => s.Kind);

            Assert.That(kinds, Does.Contain("Initializer"));
            Assert.That(kinds, Does.Contain("Physics"));
        }

        [Test]
        public void AStepThatKeepsThrowingIsReportedAsDisabled()
        {
            _runner.Add(RunnerPhase.FixedUpdate, RunnerStep.Create("angry", _ => throw new InvalidOperationException()));
            _runner.Initialize();

            LogAssert.ignoreFailingMessages = true;

            for (var i = 0; i < Runner.DefaultFailureLimit; i++)
            {
                _runner.FixedTick(Delta);
            }

            _runner.CollectSteps(_steps);
            var step = _steps.Find(s => s.Name == "angry");

            Assert.That(step.Disabled, Is.True,
                "a dropped step just stops running, so this is the only place it can be seen");
            Assert.That(step.Failures, Is.EqualTo(Runner.DefaultFailureLimit));
        }

        [Test]
        public void ASucceedingStepReportsNoFailures()
        {
            _runner.Add(RunnerPhase.FixedUpdate, RunnerStep.Create("calm", _ => { }));
            _runner.Initialize();
            _runner.FixedTick(Delta);

            _runner.CollectSteps(_steps);
            var step = _steps.Find(s => s.Name == "calm");

            Assert.That(step.Disabled, Is.False);
            Assert.That(step.Failures, Is.Zero);
        }

        [Test]
        public void TheCallersListIsCleared()
        {
            _steps.Add(new RunnerStepInfo("stale", 0, "stale", 0, false));
            _runner.CollectSteps(_steps);

            Assert.That(_steps, Is.Empty);
        }
    }
}

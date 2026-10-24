using System;
using System.Collections.Generic;
using Engine.Messages;
using Engine.Runtime;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Engine.Tests.EditMode.Runtime
{
    [TestFixture]
    public sealed class RunnerTests
    {
        private List<string> _order;
        private MessageBus _bus;
        private Runner _runner;

        [SetUp]
        public void SetUp()
        {
            _order = new List<string>();
            _bus = new MessageBus();
            _runner = new Runner(_bus);
        }

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        // --- tick order -------------------------------------------------------------------

        [Test]
        public void FixedTick_RunsSimulationThenPhysics()
        {
            _runner.PhysicsHook = Step("physics");
            _runner.Add(RunnerPhase.FixedUpdate, Step("sim"));
            _runner.Initialize();

            _runner.FixedTick(Delta);

            Assert.That(_order, Is.EqualTo(new[] { "sim", "physics" }));
        }

        [Test]
        public void FixedTick_DoesNotRunFramePhases()
        {
            _runner.Add(RunnerPhase.Update, Step("view"));
            _runner.Add(RunnerPhase.LateUpdate, Step("late"));
            _runner.Initialize();

            _runner.FixedTick(Delta);

            Assert.That(_order, Is.Empty);
        }

        [Test]
        public void FrameTick_FlushesPushBeforeRunningViewSteps()
        {
            var received = new List<int>();
            _bus.Subscriber<Ping>().Subscribe(p => received.Add(p.Value));
            _runner.Add(RunnerPhase.Update, RunnerStep.Create("view", _ => _order.Add($"view:{received.Count}")));
            _runner.Initialize();

            _bus.Publisher<Ping>().Publish(new Ping(1));
            _runner.FrameTick(0.016f);

            Assert.That(_order, Is.EqualTo(new[] { "view:1" }),
                "the view step must already see what push delivered this frame");
        }

        [Test]
        public void FrameTick_DoesNotRunFixedPhases()
        {
            _runner.Add(RunnerPhase.FixedUpdate, Step("sim"));
            _runner.PhysicsHook = Step("physics");
            _runner.Initialize();

            _runner.FrameTick(0.016f);

            Assert.That(_order, Is.Empty);
        }

        /// <summary>
        /// The two frame phases are separate calls, and that separation is the entire point:
        /// Unity poses its Animators between them, so a step that has to run after an animation
        /// has somewhere to be (<see cref="RunnerPhase.LateUpdate"/>).
        /// </summary>
        [Test]
        public void FrameTick_DoesNotRunLateSteps_AndLateFrameTickDoes()
        {
            _runner.Add(RunnerPhase.Update, Step("view"));
            _runner.Add(RunnerPhase.LateUpdate, Step("late"));
            _runner.Initialize();

            _runner.FrameTick(0.016f);

            Assert.That(_order, Is.EqualTo(new[] { "view" }),
                "a late step must not run in the frame phase — there would be no Animator between them");

            _runner.LateFrameTick(0.016f);

            Assert.That(_order, Is.EqualTo(new[] { "view", "late" }));
        }

        [Test]
        public void LateFrameTick_RunsNothingElse()
        {
            _runner.Add(RunnerPhase.FixedUpdate, Step("sim"));
            _runner.Add(RunnerPhase.Update, Step("view"));
            _runner.PhysicsHook = Step("physics");
            _runner.Initialize();

            _runner.LateFrameTick(0.016f);

            Assert.That(_order, Is.Empty);
        }

        /// <summary>
        /// A late step is initialised like any other, rather than only when its phase first runs —
        /// otherwise a system whose <c>OnAwake</c> builds a filter would do it mid-frame.
        /// </summary>
        [Test]
        public void LateSteps_AreInitializedAtBoot()
        {
            var step = new Recording("late", _order);
            _runner.Add(RunnerPhase.LateUpdate, step);

            _runner.Initialize();

            Assert.That(step.Initialized, Is.EqualTo(1));
        }

        // --- bus integration ---------------------------------------------------------------

        [Test]
        public void SimulationSteps_SeeMessagesPublishedBeforeTheTick()
        {
            var queue = _bus.Queue<Ping>();
            var seen = -1;
            _runner.Add(RunnerPhase.FixedUpdate, RunnerStep.Create("sim", _ => seen = queue.Count));
            _runner.Initialize();

            _bus.Publisher<Ping>().Publish(new Ping(1));
            _runner.FixedTick(Delta);

            Assert.That(seen, Is.EqualTo(1), "the Runner swaps queues before running the phase");
        }

        [Test]
        public void MessagesPublishedDuringAPhase_ArriveOnTheNextTick()
        {
            var queue = _bus.Queue<Ping>();
            var publisher = _bus.Publisher<Ping>();
            var counts = new List<int>();
            _runner.Add(RunnerPhase.FixedUpdate, RunnerStep.Create("sim", _ =>
            {
                counts.Add(queue.Count);
                if (counts.Count == 1) publisher.Publish(new Ping(9));
            }));
            _runner.Initialize();

            _runner.FixedTick(Delta);
            _runner.FixedTick(Delta);

            Assert.That(counts, Is.EqualTo(new[] { 0, 1 }));
        }

        // --- lifecycle ----------------------------------------------------------------------

        [Test]
        public void Initialize_RunsEveryStepOnceIncludingHooks()
        {
            var sim = new Recording("sim", _order);
            var physics = new Recording("physics", _order);
            _runner.Add(RunnerPhase.FixedUpdate, sim);
            _runner.PhysicsHook = physics;

            _runner.Initialize();
            _runner.Initialize();

            Assert.That(sim.Initialized, Is.EqualTo(1));
            Assert.That(physics.Initialized, Is.EqualTo(1));
        }

        [Test]
        public void Initializers_WakeBeforeEveryPhaseStep()
        {
            var storage = new Recording("storage", _order);
            var system = new Recording("system", _order);

            // Added in the "wrong" order on purpose — the guarantee must not depend on it.
            _runner.Add(RunnerPhase.FixedUpdate, system);
            _runner.AddInitializer(storage);

            _runner.Initialize();

            Assert.That(storage.Initialized, Is.EqualTo(1));
            Assert.That(system.Initialized, Is.EqualTo(1));
            Assert.That(_order, Is.Empty, "Initialize must not run anything");
        }

        [Test]
        public void Initializers_AreNeverTicked()
        {
            _runner.AddInitializer(Step("storage"));
            _runner.Add(RunnerPhase.FixedUpdate, Step("system"));
            _runner.Initialize();

            _runner.FixedTick(Delta);
            _runner.FrameTick(0.016f);

            Assert.That(_order, Is.EqualTo(new[] { "system" }),
                "a storage wakes once and is then only reached through what it registered");
            Assert.That(_runner.InitializerCount, Is.EqualTo(1));
        }

        [Test]
        public void AddInitializer_AfterInitialize_Throws()
        {
            _runner.Initialize();

            Assert.Throws<InvalidOperationException>(() => _runner.AddInitializer(Step("late")));
        }

        [Test]
        public void Add_AfterInitialize_Throws()
        {
            _runner.Initialize();

            Assert.Throws<InvalidOperationException>(() => _runner.Add(RunnerPhase.FixedUpdate, Step("late")));
        }

        [Test]
        public void PhysicsHook_InstalledTwice_Throws()
        {
            _runner.PhysicsHook = Step("physics");

            Assert.Throws<InvalidOperationException>(() => _runner.PhysicsHook = Step("second"));
        }

        [Test]
        public void PhysicsHook_AfterInitialize_Throws()
        {
            _runner.Initialize();

            Assert.Throws<InvalidOperationException>(() => _runner.PhysicsHook = Step("late"));
        }

        [Test]
        public void Add_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _runner.Add(RunnerPhase.FixedUpdate, null));
        }

        // --- failure handling ----------------------------------------------------------------

        [Test]
        public void AThrowingStep_IsLoggedAndTheRestOfThePhaseStillRuns()
        {
            LogAssert.ignoreFailingMessages = true;
            _runner.Add(RunnerPhase.FixedUpdate, RunnerStep.Create("bad", _ => throw new InvalidOperationException("boom")));
            _runner.Add(RunnerPhase.FixedUpdate, Step("good"));
            _runner.PhysicsHook = Step("physics");
            _runner.Initialize();

            Assert.DoesNotThrow(() => _runner.FixedTick(Delta));

            Assert.That(_order, Is.EqualTo(new[] { "good", "physics" }), "physics must still step");
        }

        [Test]
        public void ARepeatOffender_IsDisabledAfterTheFailureLimit()
        {
            LogAssert.ignoreFailingMessages = true;
            var attempts = 0;
            _runner.FailureLimit = 3;
            _runner.Add(RunnerPhase.FixedUpdate, RunnerStep.Create("bad", _ =>
            {
                attempts++;
                throw new InvalidOperationException("boom");
            }));
            _runner.Initialize();

            for (var i = 0; i < 10; i++) _runner.FixedTick(Delta);

            Assert.That(attempts, Is.EqualTo(3), "it stops being called once disabled");
        }

        [Test]
        public void AStepThatRecovers_KeepsItsPlace()
        {
            LogAssert.ignoreFailingMessages = true;
            var runs = 0;
            _runner.FailureLimit = 3;
            _runner.Add(RunnerPhase.FixedUpdate, RunnerStep.Create("flaky", _ =>
            {
                runs++;
                // Throws on every other tick — never three in a row, so never disabled.
                if (runs % 2 == 1) throw new InvalidOperationException("boom");
            }));
            _runner.Initialize();

            for (var i = 0; i < 10; i++) _runner.FixedTick(Delta);

            Assert.That(runs, Is.EqualTo(10), "a step that recovers must not accumulate toward the limit");
        }

        [Test]
        public void ADisabledStep_DoesNotStopTheOthers()
        {
            LogAssert.ignoreFailingMessages = true;
            _runner.FailureLimit = 1;
            _runner.Add(RunnerPhase.FixedUpdate, RunnerStep.Create("bad", _ => throw new InvalidOperationException("boom")));
            _runner.Add(RunnerPhase.FixedUpdate, Step("good"));
            _runner.Initialize();

            _runner.FixedTick(Delta);
            _order.Clear();
            _runner.FixedTick(Delta);

            Assert.That(_order, Is.EqualTo(new[] { "good" }));
        }

        // --- helpers ---------------------------------------------------------------------------

        private const float Delta = 1f / 30f;

        private IRunnerStep Step(string name) => RunnerStep.Create(name, _ => _order.Add(name));

        private sealed class Recording : IRunnerStep
        {
            private readonly string _name;
            private readonly List<string> _log;

            internal Recording(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            internal int Initialized { get; private set; }

            public void Initialize() => Initialized++;

            public void Run(float deltaTime) => _log.Add(_name);

            public override string ToString() => _name;
        }

        private readonly struct Ping
        {
            public readonly int Value;

            public Ping(int value) => Value = value;
        }
    }
}

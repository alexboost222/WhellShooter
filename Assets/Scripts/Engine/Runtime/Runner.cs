using System;
using System.Collections.Generic;
using Engine.Diagnostics;
using Engine.Diagnostics.Logs;
using Engine.Messages;
using Unity.Profiling;

namespace Engine.Runtime
{
    /// <summary>
    /// The host: <b>when</b> things happen. Meta's Bootstrap decides what to assemble; the
    /// Runner decides when it runs, so no game code ever touches Unity's update loop.
    /// </summary>
    /// <remarks>
    /// Pure C# apart from its profiler markers — no Morpeh, no Unity object.
    /// <code>
    /// FixedTick(dt)                        fixed step
    ///   bus.BeginPhase(FixedUpdate)        queues swap; frozen for the phase
    ///   FixedUpdate steps                  simulation
    ///   PhysicsHook                        Physics.Simulate(dt)
    ///
    /// FrameTick(frameDelta)                every frame
    ///   bus.FlushPush()                    the one push-delivery point
    ///   bus.BeginPhase(Update)
    ///   Update steps                       views
    ///
    /// [Unity evaluates Animators here]
    ///
    /// LateFrameTick(frameDelta)            every frame, after the Animators
    ///   LateUpdate steps                   views an animation would otherwise overwrite
    /// </code>
    /// <para>
    /// <b>The Runner keeps no clock.</b> It passes each phase its delta and counts fixed ticks only
    /// so a failure can say when it happened; anything that needs time reads it from a service of
    /// its own, never from here.
    /// </para>
    /// <para>
    /// <b>Everything here is also a profiler sample</b> — the tick, each phase, each step — so the
    /// Profiler window names the step a slow tick was spent in. Markers are structs created once
    /// per step and cost nothing when nothing is recording, so they are always compiled in.
    /// </para>
    /// </remarks>
    public sealed class Runner
    {
        /// <summary>Consecutive throws before a step is dropped from its phase.</summary>
        public const int DefaultFailureLimit = 5;

        // Named once here, indexed by phase at tick time.
        private static readonly ProfilerMarker[] PhaseMarkers = BuildPhaseMarkers();

        private static readonly ProfilerMarker FixedTickMarker = new ProfilerMarker("Runner.FixedTick");
        private static readonly ProfilerMarker FrameTickMarker = new ProfilerMarker("Runner.FrameTick");

        private static readonly ProfilerMarker LateFrameTickMarker =
            new ProfilerMarker("Runner.LateFrameTick");

        private readonly List<Entry>[] _phases;

        // Initialized once and never run. Storages and services live here: they wake up, set
        // the world up, and are then only touched through whatever they registered.
        private readonly List<Entry> _initializers = new List<Entry>();

        private readonly MessageBus _bus;

        private Entry _physics;
        private bool _initialized;
        private ulong _fixedTicks;

        public Runner(MessageBus bus)
        {
            Guard.ThrowIfNull(bus, nameof(bus));

            _bus = bus;
            // Sized off the enum rather than a literal: a phase added without widening this array
            // throws an index exception on the first feature that registers into it.
            _phases = new List<Entry>[Enum.GetValues(typeof(RunnerPhase)).Length];
            for (var i = 0; i < _phases.Length; i++)
            {
                _phases[i] = new List<Entry>();
            }
        }

        public int FailureLimit { get; set; } = DefaultFailureLimit;

        /// <summary>Manual physics stepping, installed by the driver. Runs after all FixedUpdate steps.</summary>
        public IRunnerStep PhysicsHook
        {
            get => _physics?.Step;
            set => _physics = Wrap(value);
        }

        public void Add(RunnerPhase phase, IRunnerStep step)
        {
            Guard.ThrowIfNull(step, nameof(step));
            Guard.ThrowIfNotMainThread();
            Guard.ThrowIf(_initialized, "Steps must be added before Initialize — boot is the validation pass.");

            _phases[(int)phase].Add(new Entry(step));
        }

        /// <summary>
        /// Registers a step that is initialised once and <b>never ticked</b> — storages and
        /// services. They have no phase because they never run in one.
        /// </summary>
        /// <remarks>
        /// Initialised before every phase step, so a storage that creates a singleton entity in
        /// <c>OnAwake</c> is always ready before any system's <c>OnAwake</c> looks for it.
        /// </remarks>
        public void AddInitializer(IRunnerStep step)
        {
            Guard.ThrowIfNull(step, nameof(step));
            Guard.ThrowIfNotMainThread();
            Guard.ThrowIf(_initialized, "Initializers must be added before Initialize — boot is the validation pass.");

            _initializers.Add(new Entry(step));
        }

        public int StepCount(RunnerPhase phase) => _phases[(int)phase].Count;

        public int InitializerCount => _initializers.Count;

        /// <summary>
        /// Every step the Runner holds, in run order, into the caller's list.
        /// </summary>
        /// <remarks>
        /// It exists for what nothing else can say: <b>a step that was dropped</b> after
        /// <see cref="FailureLimit"/> consecutive throws (it stops running, which looks exactly like
        /// running fast), order within a phase (a contract, Docs/01), and initializers, which are
        /// never ticked. There is deliberately no way to re-enable a disabled step — restart play mode.
        /// </remarks>
        public void CollectSteps(List<RunnerStepInfo> into)
        {
            Guard.ThrowIfNull(into, nameof(into));

            into.Clear();

            Collect(into, _initializers, "Initializer");
            Collect(into, _phases[(int)RunnerPhase.FixedUpdate], RunnerPhase.FixedUpdate.ToString());
            Collect(into, _physics, "Physics");
            Collect(into, _phases[(int)RunnerPhase.Update], RunnerPhase.Update.ToString());
            Collect(into, _phases[(int)RunnerPhase.LateUpdate], RunnerPhase.LateUpdate.ToString());
        }

        /// <summary>
        /// Initialises every step once, before the first tick. A step that cannot start fails
        /// here rather than on frame one of gameplay.
        /// </summary>
        public void Initialize()
        {
            Guard.ThrowIfNotMainThread();
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            // Storages and services first, so the world they set up exists before any system
            // wakes up looking for it.
            for (var i = 0; i < _initializers.Count; i++)
            {
                _initializers[i].Step.Initialize();
            }

            for (var phase = 0; phase < _phases.Length; phase++)
            {
                var steps = _phases[phase];
                for (var i = 0; i < steps.Count; i++)
                {
                    steps[i].Step.Initialize();
                }
            }

            _physics?.Step.Initialize();
        }

        public void FixedTick(float deltaTime)
        {
            Guard.ThrowIfNotMainThread();

            using (FixedTickMarker.Auto())
            {
                _fixedTicks++;

                _bus.BeginPhase(MessagePhase.FixedUpdate);

                Run(RunnerPhase.FixedUpdate, deltaTime);
                Run(_physics, deltaTime);
            }
        }

        public void FrameTick(float frameDelta)
        {
            Guard.ThrowIfNotMainThread();

            using (FrameTickMarker.Auto())
            {
                // The one defined point where push subscribers hear anything, so UI always
                // observes consistent post-tick state (Docs/02).
                _bus.FlushPush();
                _bus.BeginPhase(MessagePhase.Update);

                Run(RunnerPhase.Update, frameDelta);
            }
        }

        /// <summary>
        /// The tail of the frame, after Unity has evaluated its Animators. See
        /// <see cref="RunnerPhase.LateUpdate"/> for the one reason a system belongs here.
        /// </summary>
        /// <remarks>
        /// <b>No bus phase.</b> <see cref="FrameTick"/> already began <c>MessagePhase.Update</c>;
        /// this is the same frame continuing.
        /// </remarks>
        public void LateFrameTick(float frameDelta)
        {
            Guard.ThrowIfNotMainThread();

            using (LateFrameTickMarker.Auto())
            {
                Run(RunnerPhase.LateUpdate, frameDelta);
            }
        }

        private void Run(RunnerPhase phase, float deltaTime)
        {
            var steps = _phases[(int)phase];
            if (steps.Count == 0)
            {
                return;
            }

            using (PhaseMarkers[(int)phase].Auto())
            {
                for (var i = 0; i < steps.Count; i++)
                {
                    Run(steps[i], deltaTime);
                }
            }
        }

        private void Run(Entry entry, float deltaTime)
        {
            if (entry == null || entry.Disabled)
            {
                return;
            }

            try
            {
                using (entry.Marker.Auto())
                {
                    entry.Step.Run(deltaTime);
                }

                entry.Failures = 0;
            }
            catch (Exception exception)
            {
                entry.Failures++;

                // One broken system must not stop physics, but a system throwing every tick would
                // drown the console — so a repeat offender is dropped.
                if (entry.Failures >= FailureLimit)
                {
                    entry.Disabled = true;
                    Log.Error(LogChannels.Runtime,
                        $"{entry.Step} threw {entry.Failures} times in a row and has been disabled for this session " +
                        $"(fixed tick {_fixedTicks}). Whatever it was updating has stopped.",
                        exception);
                }
                else
                {
                    Log.Error(LogChannels.Runtime,
                        $"{entry.Step} threw on fixed tick {_fixedTicks}; the remaining steps still ran.", exception);
                }
            }
        }

        private static void Collect(List<RunnerStepInfo> into, List<Entry> entries, string kind)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                into.Add(Describe(entries[i], kind, i));
            }
        }

        private static void Collect(List<RunnerStepInfo> into, Entry entry, string kind)
        {
            if (entry != null)
            {
                into.Add(Describe(entry, kind, 0));
            }
        }

        private static RunnerStepInfo Describe(Entry entry, string kind, int order)
        {
            return new RunnerStepInfo(kind, order, entry.Step.ToString(), entry.Failures, entry.Disabled);
        }

        private static Entry Wrap(IRunnerStep step)
        {
            return step == null ? null : new Entry(step);
        }

        private static ProfilerMarker[] BuildPhaseMarkers()
        {
            var phases = (RunnerPhase[])Enum.GetValues(typeof(RunnerPhase));
            var markers = new ProfilerMarker[phases.Length];

            for (var i = 0; i < phases.Length; i++)
            {
                markers[(int)phases[i]] = new ProfilerMarker($"Runner.{phases[i]}");
            }

            return markers;
        }

        private sealed class Entry
        {
            internal readonly IRunnerStep Step;
            internal int Failures;
            internal bool Disabled;

            /// <summary>
            /// The step's Profiler sample, interned when the step is added rather than per tick.
            /// </summary>
            internal readonly ProfilerMarker Marker;

            internal Entry(IRunnerStep step)
            {
                Step = step;
                Marker = new ProfilerMarker(step.ToString());
            }
        }
    }
}

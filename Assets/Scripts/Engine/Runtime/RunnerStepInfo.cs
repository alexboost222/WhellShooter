namespace Engine.Runtime
{
    /// <summary>
    /// One of the Runner's steps, flattened for a reader: where it runs, what it is called, and
    /// whether it is still running at all.
    /// </summary>
    /// <remarks>
    /// <b><see cref="Kind"/> is a string, not the <see cref="RunnerPhase"/> enum</b>, because two of
    /// the five positions a step can occupy are not phases: the initializers, and the physics hook
    /// the Runner runs after <c>FixedUpdate</c>. Widening the enum to cover them would put
    /// "initializers" in a type whose other members are things that tick.
    /// <para>
    /// <b>It carries no <c>IRunnerStep</c>.</b> A reader holding one across a play session would be
    /// holding a step belonging to a Runner that no longer exists.
    /// </para>
    /// </remarks>
    public readonly struct RunnerStepInfo
    {
        public RunnerStepInfo(string kind, int order, string name, int failures, bool disabled)
        {
            Kind = kind;
            Order = order;
            Name = name;
            Failures = failures;
            Disabled = disabled;
        }

        /// <summary>
        /// <c>Initializer</c>, <c>FixedUpdate</c>, <c>Physics</c>, <c>Update</c> or <c>LateUpdate</c>.
        /// </summary>
        public string Kind { get; }

        /// <summary>Position within its kind, which is the order it runs in — a contract (Docs/01).</summary>
        public int Order { get; }

        /// <summary>The step's <c>ToString</c>, which is also its Profiler marker name.</summary>
        public string Name { get; }

        /// <summary>Consecutive throws so far. Reset by any success.</summary>
        public int Failures { get; }

        /// <summary>
        /// Dropped for the rest of the session after too many consecutive throws. The one fact about
        /// a step that nothing else can report — a dropped step simply stops showing up.
        /// </summary>
        public bool Disabled { get; }

        public override string ToString() =>
            Disabled ? $"{Kind}[{Order}] {Name} (disabled after {Failures})" : $"{Kind}[{Order}] {Name}";
    }
}

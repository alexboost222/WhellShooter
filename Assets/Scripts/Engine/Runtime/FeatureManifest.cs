#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;

namespace Engine.Runtime
{
    /// <summary>What kind of thing a feature registered.</summary>
    public enum FeatureFact
    {
        /// <summary>A system added to a phase — <c>FixedUpdate: MovementSystem</c>.</summary>
        Step = 0,

        /// <summary>A storage or service woken once and never ticked.</summary>
        Initializer = 1,
    }

    /// <summary>Everything one feature installed, in the order it installed it.</summary>
    public sealed class FeatureRecord
    {
        private readonly List<string> _steps = new List<string>();
        private readonly List<string> _initializers = new List<string>();

        internal FeatureRecord(string name, int order)
        {
            Name = name;
            Order = order;
        }

        public string Name { get; }

        /// <summary>Position in <c>Features.cs</c>, which is the order everything runs in (Docs/01).</summary>
        public int Order { get; }

        public IReadOnlyList<string> Steps => _steps;

        public IReadOnlyList<string> Initializers => _initializers;

        public int Total => _steps.Count + _initializers.Count;

        internal void Note(FeatureFact kind, string value)
        {
            switch (kind)
            {
                case FeatureFact.Step:
                    _steps.Add(value);
                    break;

                case FeatureFact.Initializer:
                    _initializers.Add(value);
                    break;
            }
        }

        public override string ToString()
        {
            return $"{Order}. {Name} ({Total} registration(s))";
        }
    }

    /// <summary>
    /// Which feature installed what, recorded as it happens. The answer to "why did my system not
    /// run", without reading <c>Features.cs</c>.
    /// </summary>
    /// <remarks>
    /// A feature instance is discarded the moment <c>Register</c> returns, and what it built goes
    /// into the container and the Runner, neither of which keeps a back-reference — so nothing else
    /// could reconstruct this afterwards.
    /// <para>
    /// <b>Fenced to the editor and development builds:</b> a release build has no reader for it.
    /// Filled at boot by <c>Feature.Install</c> and <c>EcsFeature</c>'s registration methods.
    /// </para>
    /// </remarks>
    public static class FeatureManifest
    {
        private static readonly List<FeatureRecord> Records = new List<FeatureRecord>();

        private static FeatureRecord _current;

        /// <summary>Every feature that has installed, in list order.</summary>
        public static IReadOnlyList<FeatureRecord> All => Records;

        /// <summary>
        /// Forgets everything. Called by the composition root before it registers, so that a second
        /// play session with domain reload disabled does not report both.
        /// </summary>
        public static void Reset()
        {
            Records.Clear();
            _current = null;
        }

        internal static void Begin(string feature)
        {
            _current = new FeatureRecord(feature, Records.Count);
            Records.Add(_current);
        }

        internal static void End()
        {
            _current = null;
        }

        /// <summary>
        /// Records something the feature currently installing just registered. Silently ignored
        /// outside a <see cref="Begin"/>/<see cref="End"/> pair — the composition root registers a
        /// handful of steps itself, and those belong to no feature.
        /// </summary>
        internal static void Note(FeatureFact kind, string value)
        {
            _current?.Note(kind, value);
        }
    }
}
#endif

using Engine.DI;
using Engine.Diagnostics;
using Engine.Diagnostics.Logs;
using Engine.Runtime;
using Features.DestroyFeature;
using Features.Map;

namespace Meta
{
    /// <summary>
    /// <b>THE ordered feature list.</b> One screen, read top to bottom — this order is the
    /// system order, and it is the only place it is decided (Docs/01, Docs/06).
    /// </summary>
    /// <remarks>
    /// Infrastructure first, then gameplay, then views, and the entity-destroy feature
    /// <b>last</b>. The Runner special-cases nothing; if something runs at the wrong time, it is
    /// in the wrong place in this list.
    /// <para>
    /// The order is also a dependency contract: a feature builds its systems as it registers,
    /// so anything it injects from another feature must be listed above it.
    /// </para>
    /// </remarks>
    public static class Features
    {
        private static Container _container;
        private static Runner _runner;

        /// <summary>How many features the last <see cref="Register"/> installed.</summary>
        public static int Count { get; private set; }

        public static void Register(Container container, Runner runner)
        {
            Guard.ThrowIfNull(container, nameof(container));
            Guard.ThrowIfNull(runner, nameof(runner));

            _container = container;
            _runner = runner;
            Count = 0;

            try
            {
                Registrations();
            }
            finally
            {
                // Boot-only: nothing may register a feature after this returns.
                _container = null;
                _runner = null;
            }

            Log.Debug(LogChannels.Runtime, $"{Count} feature(s) registered.");
        }

        private static void Register<TFeature>() where TFeature : Feature, new()
        {
            new TFeature().Install(_container, _runner);
            Count++;
        }

        private static void Registrations()
        {
            // ---- gameplay ----
            // The arena: layout, spawn points (registers MapStorage).
            Register<MapFeature>();

            // ---- last ----
            // Disposes everything tagged Destroy at the end of the tick. Must stay last.
            Register<EntitiesDestroyFeature>();
        }
    }
}

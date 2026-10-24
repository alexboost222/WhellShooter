using Engine.Configs;
using Engine.DI;
using Engine.Diagnostics.Logs;
using Engine.Ecs;
using Engine.Messages;
using Engine.Physics;
using Engine.Runtime;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta
{
    /// <summary>
    /// The composition root. Builds the container, the world, the engine services and every feature,
    /// then hands the Runner to Unity's loop — identically for every way into the game.
    /// </summary>
    /// <remarks>
    /// <b>Static magic on purpose</b> (Docs/06): <c>BeforeSceneLoad</c> runs no matter which scene
    /// Play was pressed in, so there is no "boot scene" to forget.
    /// <para>
    /// <b>Boot is the validation pass.</b> Every feature builds its systems here and the container
    /// throws on the first unresolved <c>[Inject]</c>, so a broken graph dies on frame one with a
    /// readable error rather than three systems later.
    /// </para>
    /// <para>
    /// <b>Domain reload is disabled</b>, so every static here outlives play mode unless
    /// <see cref="Shutdown"/> nulls it, and the driver must die with the session or it keeps ticking
    /// a disposed world.
    /// </para>
    /// </remarks>
    public static class Bootstrap
    {
        private static Container _container;
        private static World _world;
        private static Runner _runner;
        private static PhysicsWorld _physics;
        private static UnityRunnerDriver _driver;

        /// <summary>The live container, for tests and tooling only. Null outside play mode.</summary>
        internal static Container Container => _container;

        /// <summary>The live Runner, for tests and tooling only. Null outside play mode.</summary>
        internal static Runner Runner => _runner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            // Domain reload disabled: don't build twice.
            if (_container != null)
            {
                return;
            }

            // 1. Container and world.
            _container = new Container();
            _world = World.Create();

            // 2. Engine services.
            var catalog = ConfigCatalog.FromResources();
            var bus = new MessageBus();
            _runner = new Runner(bus);

            // phys-r, ticking at Unity's fixed step (Docs/11). Stepped by the bodies feature's hook.
            _physics = new PhysicsWorld(PhysicsWorld.TickRateOf(Time.fixedDeltaTime));

            _container.Register(catalog);
            _container.Register(_world);
            _container.Register(bus);
            _container.Register(_runner);
            _container.Register(_physics);
            _container.Register<ISceneLoader>(new SceneLoader());

            // 3. Resolvers, in priority order — first match wins, InstanceResolver is always first.
            _container.AddResolver(new ConfigResolver(catalog));
            _container.AddResolver(new StashResolver(_world));
            _container.AddResolver(new MessagePipeResolver(bus));

            // 4. Features, in Features.cs order. A feature injects only what earlier ones registered.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Domain reload is off, so last session's manifest would otherwise be appended to.
            FeatureManifest.Reset();
#endif
            Features.Register(_container, _runner);

            // The console sink queues entries logged off the main thread (Debug.Log runs foreign
            // logMessageReceived subscribers on the calling thread); this is what prints them.
            _runner.Add(RunnerPhase.Update, RunnerStep.Create("Log.Flush", _ => UnityConsoleSink.Flush()));

            // 5. Wake storages, then systems — before the first tick.
            _runner.Initialize();

            // 6. Hand the Runner to Unity's loop.
            _driver = UnityRunnerDriver.Install(_runner);

            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting += Shutdown;

            // The scene we started in, which the sceneLoaded hook never sees: BeforeSceneLoad runs
            // before it has loaded, and the event for it has already fired by the time anything
            // could subscribe after.
            _container.InjectScene(SceneManager.GetActiveScene());

            Log.Info(LogChannels.Runtime, "Booted.");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _container?.InjectScene(scene);
        }

        /// <summary>
        /// Tears the session down. Raised by <c>Application.quitting</c>, which the editor also fires
        /// on leaving play mode.
        /// </summary>
        private static void Shutdown()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.quitting -= Shutdown;

            // Stop the tick before disposing what it ticks.
            if (_driver != null)
            {
                _driver.Uninstall();
            }

            UnityConsoleSink.Flush();
            _world?.Dispose();
            // After the ECS world: systems are disposed first and may still hold native handles.
            _physics?.Dispose();

            _driver = null;
            _physics = null;
            _runner = null;
            _world = null;
            _container = null;
        }
    }
}

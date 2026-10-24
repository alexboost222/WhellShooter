using Engine.Diagnostics;
using Engine.Diagnostics.Logs;
using UnityEngine;

namespace Engine.Runtime
{
    /// <summary>
    /// Feeds Unity's loop into the <see cref="Runner"/> and nothing else.
    /// </summary>
    /// <remarks>
    /// Also switches physics to <see cref="SimulationMode.Script"/> and installs
    /// <c>Physics.Simulate</c> as the Runner's physics hook. The tick's ordering guarantee is
    /// only real if Unity isn't also auto-stepping physics behind our back.
    /// <para>
    /// The fixed step is Unity's own (<c>ProjectSettings/TimeManager</c>); the driver forwards
    /// <c>Time.fixedDeltaTime</c> rather than owning a rate.
    /// </para>
    /// </remarks>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class UnityRunnerDriver : MonoBehaviour
    {
        private Runner _runner;

        /// <summary>
        /// Creates the hidden driver object and takes physics off auto. Called once from Bootstrap.
        /// </summary>
        public static UnityRunnerDriver Install(Runner runner)
        {
            Guard.ThrowIfNull(runner, nameof(runner));
            Guard.ThrowIfNotMainThread();

            Physics.simulationMode = SimulationMode.Script;

            runner.PhysicsHook = RunnerStep.Create("Physics.Simulate", Physics.Simulate);

            // DontDestroyOnLoad only — deliberately NOT HideFlags.DontSave. That flag makes the
            // object survive leaving play mode in the editor, so the next session's Bootstrap
            // builds a fresh world while this driver keeps ticking the old, disposed one.
            var host = new GameObject(nameof(UnityRunnerDriver));
            DontDestroyOnLoad(host);

            var driver = host.AddComponent<UnityRunnerDriver>();
            driver._runner = runner;

            Log.Debug(LogChannels.Runtime, $"Runner driving at {1f / Time.fixedDeltaTime:0.#} Hz.");

            return driver;
        }

        /// <summary>Stops ticking and destroys the driver object. Bootstrap calls this on shutdown.</summary>
        public void Uninstall()
        {
            _runner = null;
            if (this != null)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>Belt and braces: a driver that outlives its runner must not tick a dead world.</summary>
        private void OnDestroy()
        {
            _runner = null;
        }

        private void FixedUpdate()
        {
            _runner?.FixedTick(Time.fixedDeltaTime);
        }

        private void Update()
        {
            _runner?.FrameTick(Time.deltaTime);
        }

        /// <remarks>
        /// Unity poses Animators between <c>Update</c> and here, which is the whole reason the
        /// phase exists — see <see cref="RunnerPhase.LateUpdate"/>.
        /// </remarks>
        private void LateUpdate()
        {
            _runner?.LateFrameTick(Time.deltaTime);
        }
    }
}

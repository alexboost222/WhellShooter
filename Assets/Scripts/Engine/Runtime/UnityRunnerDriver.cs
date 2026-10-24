using Engine.Diagnostics;
using Engine.Diagnostics.Logs;
using UnityEngine;

namespace Engine.Runtime
{
    /// <summary>
    /// Feeds Unity's loop into the <see cref="Runner"/> and nothing else.
    /// </summary>
    /// <remarks>
    /// Also switches Unity's PhysX to <see cref="SimulationMode.Script"/> and never steps it: the
    /// simulation's physics is phys-r, stepped by the Runner's physics hook (Docs/11), so a stray
    /// collider in a view stays inert instead of being auto-simulated behind our back.
    /// <para>
    /// <b>Ticks are not Unity's <c>FixedUpdate</c>.</b> Each <c>Update</c> feeds the previous
    /// frame's unscaled delta into a <see cref="TickAccumulator"/> and runs the fixed phase as many
    /// times as it says (0, 1 or more, at most <see cref="MaxTicksPerFrame"/>), then the frame
    /// phase. The tick length is read once from <c>Time.fixedDeltaTime</c>
    /// (<c>ProjectSettings/TimeManager</c>) so there is still one place to set it.
    /// </para>
    /// </remarks>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class UnityRunnerDriver : MonoBehaviour
    {
        /// <summary>Most ticks one frame may run; the rest of a long frame is dropped (no death spiral).</summary>
        public const int MaxTicksPerFrame = 8;

        /// <summary>The frame-rate cap on iOS / Android (Docs/10).</summary>
        public const int MobileFrameRate = 60;

        private Runner _runner;
        private TickAccumulator _ticks;
        private long _reportedDrops;

        /// <summary>
        /// Creates the hidden driver object and takes PhysX off auto. Called once from Bootstrap.
        /// </summary>
        public static UnityRunnerDriver Install(Runner runner)
        {
            Guard.ThrowIfNull(runner, nameof(runner));
            Guard.ThrowIfNotMainThread();

            UnityEngine.Physics.simulationMode = SimulationMode.Script;

#if UNITY_IOS || UNITY_ANDROID
            // Mobile players default to 30 fps; a shooter wants every frame the screen offers up to
            // 60 (Docs/10). vSync is ignored on mobile — the frame rate is the cap.
            Application.targetFrameRate = MobileFrameRate;
#endif

            // DontDestroyOnLoad only — deliberately NOT HideFlags.DontSave. That flag makes the
            // object survive leaving play mode in the editor, so the next session's Bootstrap
            // builds a fresh world while this driver keeps ticking the old, disposed one.
            var host = new GameObject(nameof(UnityRunnerDriver));
            DontDestroyOnLoad(host);

            var driver = host.AddComponent<UnityRunnerDriver>();
            driver._runner = runner;
            driver._ticks = new TickAccumulator(Time.fixedDeltaTime, MaxTicksPerFrame);

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

        private void Update()
        {
            if (_runner == null)
            {
                return;
            }

            // Unscaled: the tick is real time, shared with other processes over the network.
            var ticks = _ticks.Advance(Time.unscaledDeltaTime);
            for (var i = 0; i < ticks; i++)
            {
                _runner.FixedTick(_ticks.TickSeconds);
            }

            if (_ticks.DroppedTicks != _reportedDrops)
            {
                Log.Warning(LogChannels.Runtime,
                    $"Frame owed more than {MaxTicksPerFrame} ticks; dropped {_ticks.DroppedTicks - _reportedDrops}.");
                _reportedDrops = _ticks.DroppedTicks;
            }

            _runner.FrameTick(Time.deltaTime);
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

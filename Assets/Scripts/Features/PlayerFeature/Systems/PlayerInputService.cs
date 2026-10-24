using Engine.DI;
using Engine.Diagnostics;
using Features.PlayerFeature.Configs;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// Owns the player's input actions: enables them at boot, hands them to
    /// <c>PlayerInputSystem</c>, and keeps the cursor captured while the game has it.
    /// </summary>
    /// <remarks>
    /// <b>A copy of the asset, not the asset.</b> Enabling actions on the project asset itself
    /// would leave them enabled after play mode — with domain reload off, into the next session.
    /// <para>
    /// The cursor rule is a desktop convenience: Escape releases it — which is what opens the pause
    /// screen — and Escape again or the screen's Resume takes it back. Look input is ignored while it
    /// is free, so moving to the inspector or a slider does not spin the view. A click does not
    /// recapture: it may be a click on the pause screen. On touch there is no cursor and it is
    /// always "captured".
    /// </para>
    /// </remarks>
    public sealed class PlayerInputService : IInitializer
    {
        private const string MapName = "Player";

        [Inject] private PlayerConfig _config;

        public World World { get; set; }

        private InputActionAsset _asset;
        private InputActionMap _map;

        public InputAction Move { get; private set; }
        public InputAction Look { get; private set; }
        public InputAction Jump { get; private set; }
        public InputAction Sprint { get; private set; }
        public InputAction Fire { get; private set; }
        public InputAction Aim { get; private set; }

        /// <summary>Whether pointer movement should turn the view right now.</summary>
        public bool LookCaptured => Cursor.lockState == CursorLockMode.Locked || Touchscreen.current != null;

        public void OnAwake()
        {
            Guard.ThrowIf(_config.InputActions == null, $"{nameof(PlayerConfig)} has no input actions assigned.");

            _asset = Object.Instantiate(_config.InputActions);
            _map = _asset.FindActionMap(MapName, throwIfNotFound: true);

            Move = _map.FindAction("Move", throwIfNotFound: true);
            Look = _map.FindAction("Look", throwIfNotFound: true);
            Jump = _map.FindAction("Jump", throwIfNotFound: true);
            Sprint = _map.FindAction("Sprint", throwIfNotFound: true);
            Fire = _map.FindAction("Attack", throwIfNotFound: true);
            Aim = _map.FindAction("Aim", throwIfNotFound: true);

            _map.Enable();
            Capture(true);
        }

        public void Dispose()
        {
            Capture(false);

            if (_map != null)
            {
                _map.Disable();
            }

            if (_asset != null)
            {
                Object.Destroy(_asset);
            }

            _asset = null;
            _map = null;
        }

        /// <summary>Applies the desktop cursor rule. Called once per frame by the input system.</summary>
        public void UpdateCursor()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Capture(Cursor.lockState != CursorLockMode.Locked);
            }
        }

        /// <summary>Takes the cursor back — the pause screen's Resume, via <c>ResumePlay</c>.</summary>
        public void Resume()
        {
            Capture(true);
        }

        private static void Capture(bool captured)
        {
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }
    }
}

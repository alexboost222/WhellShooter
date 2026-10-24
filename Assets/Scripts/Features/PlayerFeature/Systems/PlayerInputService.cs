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
    /// The cursor rule is a desktop convenience: Escape releases it, a click takes it back, and
    /// look input is ignored while it is free so moving to the inspector does not spin the view.
    /// On touch there is no cursor and it is always "captured".
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
                Capture(false);
                return;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Capture(true);
            }
        }

        private static void Capture(bool captured)
        {
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }
    }
}

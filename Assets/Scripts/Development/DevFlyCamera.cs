using UnityEngine;
using UnityEngine.InputSystem;

namespace Development
{
    /// <summary>
    /// Dev tool: a free camera for walking the arena before there is a player. Hold the right mouse
    /// button to look; WASD to move, Q/E down/up, Shift to go fast.
    /// </summary>
    /// <remarks>
    /// Reads the Input System devices directly and moves its own transform — debug tooling, not
    /// gameplay, which is why it may own logic that a game MonoBehaviour may not.
    /// </remarks>
    public sealed class DevFlyCamera : MonoBehaviour
    {
        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _fastMultiplier = 3f;
        [SerializeField] private float _lookSensitivity = 0.15f;

        private float _pitch;
        private float _yaw;

        private void OnEnable()
        {
            var angles = transform.eulerAngles;
            _pitch = angles.x > 180f ? angles.x - 360f : angles.x;
            _yaw = angles.y;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null)
            {
                return;
            }

            if (mouse.rightButton.isPressed)
            {
                var delta = mouse.delta.ReadValue() * _lookSensitivity;
                _yaw += delta.x;
                _pitch = Mathf.Clamp(_pitch - delta.y, -89f, 89f);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            var move = Vector3.zero;
            move += keyboard.wKey.isPressed ? Vector3.forward : Vector3.zero;
            move += keyboard.sKey.isPressed ? Vector3.back : Vector3.zero;
            move += keyboard.dKey.isPressed ? Vector3.right : Vector3.zero;
            move += keyboard.aKey.isPressed ? Vector3.left : Vector3.zero;
            move += keyboard.eKey.isPressed ? Vector3.up : Vector3.zero;
            move += keyboard.qKey.isPressed ? Vector3.down : Vector3.zero;

            var speed = _speed * (keyboard.leftShiftKey.isPressed ? _fastMultiplier : 1f);
            // Real frame time is right here: a dev camera is outside the simulation and its clock.
            transform.Translate(move * (speed * Time.unscaledDeltaTime), Space.Self);
        }
    }
}

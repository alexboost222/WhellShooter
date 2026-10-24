using Features.Network.Enums;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInput = Features.Network.Components.PlayerInput;

namespace Features.Network.Providers
{
    /// <summary>Keyboard + mouse through the Input System: WASD, mouse look, LMB fire, Space jump.</summary>
    public sealed class InputSystemPlayerInputSource : IPlayerInputSource
    {
        public PlayerInput Sample()
        {
            var input = new PlayerInput();

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                input.MoveX = Axis(keyboard.aKey.isPressed, keyboard.dKey.isPressed);
                input.MoveY = Axis(keyboard.sKey.isPressed, keyboard.wKey.isPressed);
                if (keyboard.spaceKey.isPressed)
                {
                    input.Buttons |= (byte)InputButtons.Jump;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                var delta = mouse.delta.ReadValue();
                input.LookYaw = (short)Mathf.Clamp(delta.x * 100f, short.MinValue, short.MaxValue);
                input.LookPitch = (short)Mathf.Clamp(delta.y * 100f, short.MinValue, short.MaxValue);
                if (mouse.leftButton.isPressed)
                {
                    input.Buttons |= (byte)InputButtons.Fire;
                }
            }

            return input;
        }

        private static sbyte Axis(bool negative, bool positive)
        {
            return (sbyte)((positive ? 127 : 0) - (negative ? 127 : 0));
        }
    }
}

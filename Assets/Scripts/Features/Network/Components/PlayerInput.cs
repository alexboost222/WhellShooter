using System;

namespace Features.Network.Components
{
    /// <summary>One tick of a player's input — what travels in every <c>Input</c> packet (Docs/05).</summary>
    public struct PlayerInput : IEquatable<PlayerInput>
    {
        public const int Size = 8;

        /// <summary>-127..127 strafe / forward.</summary>
        public sbyte MoveX;
        public sbyte MoveY;

        /// <summary>Absolute look: yaw <c>(yaw + π) / 2π × 65536</c>, pitch radians × 10000.</summary>
        public ushort LookYaw;
        public short LookPitch;

        /// <summary>Bit flags, see <see cref="Enums.InputButtons"/>.</summary>
        public byte Buttons;

        /// <summary>Ticks between this input's tick and the snapshot tick the client was drawing.</summary>
        public byte ViewDelay;

        public bool Equals(PlayerInput other)
        {
            return MoveX == other.MoveX && MoveY == other.MoveY && LookYaw == other.LookYaw &&
                   LookPitch == other.LookPitch && Buttons == other.Buttons && ViewDelay == other.ViewDelay;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerInput other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MoveX, MoveY, LookYaw, LookPitch, Buttons, ViewDelay);
        }

        public override string ToString() =>
            $"move({MoveX},{MoveY}) look({LookYaw},{LookPitch}) btn {Buttons:X2} delay {ViewDelay}";
    }
}

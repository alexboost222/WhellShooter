using System;

namespace Features.Network.Enums
{
    /// <summary><c>PlayerInput.Buttons</c> bits. Wire format — never renumber.</summary>
    [Flags]
    public enum InputButtons : byte
    {
        None = 0,
        Fire = 1 << 0,
        /// <summary>Latch: pressed since the last tick sample.</summary>
        Jump = 1 << 1,
        Sprint = 1 << 2,
        Aim = 1 << 3,
        /// <summary>Latch: fire pressed since the last tick sample (semi-auto).</summary>
        FirePressed = 1 << 4,

        Latches = Jump | FirePressed,
    }
}

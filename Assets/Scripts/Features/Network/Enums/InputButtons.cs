using System;

namespace Features.Network.Enums
{
    [Flags]
    public enum InputButtons : byte
    {
        None = 0,
        Fire = 1 << 0,
        Jump = 1 << 1,
    }
}

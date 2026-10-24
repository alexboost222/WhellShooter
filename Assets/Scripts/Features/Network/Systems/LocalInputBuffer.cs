using Features.Network.Components;
using Features.Network.Enums;
using Features.Network.Providers;

namespace Features.Network.Systems
{
    /// <summary>
    /// The production <see cref="IPlayerInputSource"/>: the player feature writes the local input
    /// every frame, the client tick samples it (Docs/05 §Input). Held fields are replaced, latches
    /// accumulate until a sample takes them.
    /// </summary>
    public sealed class LocalInputBuffer : IPlayerInputSource
    {
        private PlayerInput _current;
        private byte _viewDelay;

        /// <summary>Move and buttons; the look fields are left as they are.</summary>
        public void WriteMove(sbyte moveX, sbyte moveY, InputButtons held, InputButtons pressed)
        {
            _current.MoveX = moveX;
            _current.MoveY = moveY;
            var latches = (byte)(_current.Buttons & (byte)InputButtons.Latches);
            _current.Buttons = (byte)((byte)(held & ~InputButtons.Latches) | latches |
                                      (byte)(pressed & InputButtons.Latches));
        }

        public void WriteLook(ushort yaw, short pitch)
        {
            _current.LookYaw = yaw;
            _current.LookPitch = pitch;
        }

        /// <summary>Written by replication: how far behind its own tick the client is drawing.</summary>
        public void WriteViewDelay(byte ticks)
        {
            _viewDelay = ticks;
        }

        public PlayerInput Sample()
        {
            var input = _current;
            input.ViewDelay = _viewDelay;
            _current.Buttons = (byte)(_current.Buttons & ~(byte)InputButtons.Latches);
            return input;
        }
    }
}

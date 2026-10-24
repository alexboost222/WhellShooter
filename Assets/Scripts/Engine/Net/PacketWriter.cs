using System;

namespace Engine.Net
{
    /// <summary>Little-endian writer over a growable buffer. Reused: <see cref="Reset"/> per packet.</summary>
    public sealed class PacketWriter
    {
        private byte[] _buffer;

        public PacketWriter(int capacity = 1024)
        {
            _buffer = new byte[capacity];
        }

        public byte[] Buffer => _buffer;

        public int Length { get; private set; }

        public void Reset()
        {
            Length = 0;
        }

        public void WriteByte(byte value)
        {
            Ensure(1);
            _buffer[Length++] = value;
        }

        public void WriteSByte(sbyte value)
        {
            WriteByte((byte)value);
        }

        public void WriteShort(short value)
        {
            Ensure(2);
            _buffer[Length++] = (byte)value;
            _buffer[Length++] = (byte)(value >> 8);
        }

        public void WriteInt(int value)
        {
            Ensure(4);
            _buffer[Length++] = (byte)value;
            _buffer[Length++] = (byte)(value >> 8);
            _buffer[Length++] = (byte)(value >> 16);
            _buffer[Length++] = (byte)(value >> 24);
        }

        public void WriteUInt(uint value)
        {
            WriteInt((int)value);
        }

        private void Ensure(int bytes)
        {
            if (Length + bytes > _buffer.Length)
            {
                Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, Length + bytes));
            }
        }
    }
}

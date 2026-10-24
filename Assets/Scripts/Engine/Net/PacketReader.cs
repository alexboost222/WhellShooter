using System;

namespace Engine.Net
{
    /// <summary>
    /// Little-endian reader. Never throws: reading past the end yields zeros and clears
    /// <see cref="IsValid"/>, which the caller checks once after parsing — a malformed datagram is
    /// dropped, not an exception.
    /// </summary>
    public struct PacketReader
    {
        private readonly byte[] _data;
        private readonly int _length;
        private int _position;

        public PacketReader(byte[] data, int length)
        {
            _data = data;
            _length = data == null ? 0 : length;
            _position = 0;
            IsValid = true;
        }

        public PacketReader(byte[] data) : this(data, data == null ? 0 : data.Length)
        {
        }

        public bool IsValid { get; private set; }

        public int Remaining => _length - _position;

        public byte ReadByte()
        {
            if (!Take(1))
            {
                return 0;
            }

            return _data[_position++];
        }

        public sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        public short ReadShort()
        {
            if (!Take(2))
            {
                return 0;
            }

            var value = (short)(_data[_position] | (_data[_position + 1] << 8));
            _position += 2;
            return value;
        }

        public int ReadInt()
        {
            if (!Take(4))
            {
                return 0;
            }

            var value = _data[_position]
                        | (_data[_position + 1] << 8)
                        | (_data[_position + 2] << 16)
                        | (_data[_position + 3] << 24);
            _position += 4;
            return value;
        }

        public uint ReadUInt()
        {
            return (uint)ReadInt();
        }

        public ushort ReadUShort()
        {
            return (ushort)ReadShort();
        }

        public float ReadFloat()
        {
            return BitConverter.Int32BitsToSingle(ReadInt());
        }

        private bool Take(int bytes)
        {
            if (!IsValid || _position + bytes > _length)
            {
                IsValid = false;
                return false;
            }

            return true;
        }
    }
}

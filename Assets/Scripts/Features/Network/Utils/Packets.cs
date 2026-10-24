using Engine.Net;
using Features.Network.Components;
using Features.Network.Enums;

namespace Features.Network.Utils
{
    /// <summary>Wire format of the two packets (Docs/05). Little-endian, first byte <see cref="PacketType"/>.</summary>
    public static class Packets
    {
        public const int InputHeaderSize = 1 + 4 + 4 + 1;

        public static void WriteSync(PacketWriter writer, in SyncPacket packet)
        {
            writer.Reset();
            writer.WriteByte((byte)PacketType.Sync);
            writer.WriteInt(packet.ServerTick);
            writer.WriteUInt(packet.EchoFrame);
            writer.WriteByte(packet.EchoAge);
            writer.WriteInt(packet.AckTick);
            writer.WriteInt(packet.SampleTick);
            writer.WriteSByte(packet.Slack);
            writer.WriteByte(packet.LossHalfPercent);
            writer.WriteInt(packet.LastMissedTick);
        }

        /// <summary>Reads a Sync body; the type byte has already been read.</summary>
        public static bool TryReadSync(ref PacketReader reader, out SyncPacket packet)
        {
            packet = new SyncPacket
            {
                ServerTick = reader.ReadInt(),
                EchoFrame = reader.ReadUInt(),
                EchoAge = reader.ReadByte(),
                AckTick = reader.ReadInt(),
                SampleTick = reader.ReadInt(),
                Slack = reader.ReadSByte(),
                LossHalfPercent = reader.ReadByte(),
                LastMissedTick = reader.ReadInt(),
            };

            return reader.IsValid;
        }

        public static void WriteInputHeader(PacketWriter writer, uint frame, int firstTick, int count)
        {
            writer.Reset();
            writer.WriteByte((byte)PacketType.Input);
            writer.WriteUInt(frame);
            writer.WriteInt(firstTick);
            writer.WriteByte((byte)count);
        }

        public static void WriteInput(PacketWriter writer, in PlayerInput input)
        {
            writer.WriteSByte(input.MoveX);
            writer.WriteSByte(input.MoveY);
            writer.WriteShort(input.LookYaw);
            writer.WriteShort(input.LookPitch);
            writer.WriteByte(input.Buttons);
        }

        public static PlayerInput ReadInput(ref PacketReader reader)
        {
            return new PlayerInput
            {
                MoveX = reader.ReadSByte(),
                MoveY = reader.ReadSByte(),
                LookYaw = reader.ReadShort(),
                LookPitch = reader.ReadShort(),
                Buttons = reader.ReadByte(),
            };
        }
    }
}

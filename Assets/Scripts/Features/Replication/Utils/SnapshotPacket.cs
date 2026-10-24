using Engine.Net;
using Features.Network.Enums;
using Features.Network.Utils;
using Features.Replication.Enums;
using Unity.Mathematics;

namespace Features.Replication.Utils
{
    /// <summary>Wire format of <c>Snapshot</c> (Docs/05 §Snapshot). Little-endian, first byte <see cref="PacketType"/>.</summary>
    public static class SnapshotPacket
    {
        public const int HeaderSize = 1 + 4 + 4 + 4 + 1;
        public const int BodySize = 4 + 1 + 1 + 12 + 12 + 2 + 2 + 4 + 2 + 2 + 4;

        /// <summary>Bodies per packet: one unreliable datagram stays under ~1.2 KB.</summary>
        public const int MaxBodies = 24;

        public static void Write(PacketWriter writer, int tick, int seed, int you, BodySnapshot[] bodies, int count)
        {
            count = math.min(count, MaxBodies);
            writer.Reset();
            writer.WriteByte((byte)PacketType.Snapshot);
            writer.WriteInt(tick);
            writer.WriteInt(seed);
            writer.WriteInt(you);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                WriteBody(writer, in bodies[i]);
            }
        }

        /// <summary>Reads a Snapshot body into <paramref name="frame"/>; the type byte has already been read.</summary>
        public static bool TryRead(ref PacketReader reader, SnapshotFrame frame)
        {
            frame.Tick = reader.ReadInt();
            frame.Seed = reader.ReadInt();
            frame.You = reader.ReadInt();
            var count = reader.ReadByte();
            if (!reader.IsValid || count > MaxBodies || reader.Remaining != count * BodySize)
            {
                frame.Count = 0;
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                frame.Bodies[i] = ReadBody(ref reader);
            }

            frame.Count = count;
            return reader.IsValid;
        }

        private static void WriteBody(PacketWriter writer, in BodySnapshot body)
        {
            writer.WriteInt(body.Id);
            writer.WriteByte((byte)body.Flags);
            writer.WriteSByte(body.Team);
            WriteFloat3(writer, body.Feet);
            WriteFloat3(writer, body.Velocity);
            writer.WriteUShort(LookCodec.EncodeYaw(body.Yaw));
            writer.WriteShort(LookCodec.EncodePitch(body.Pitch));
            writer.WriteFloat(body.Health);
            writer.WriteUShort(body.Hits);
            writer.WriteUShort(body.Shots);
            writer.WriteInt(body.Killer);
        }

        private static BodySnapshot ReadBody(ref PacketReader reader)
        {
            return new BodySnapshot
            {
                Id = reader.ReadInt(),
                Flags = (SnapshotFlags)reader.ReadByte(),
                Team = reader.ReadSByte(),
                Feet = ReadFloat3(ref reader),
                Velocity = ReadFloat3(ref reader),
                Yaw = LookCodec.DecodeYaw(reader.ReadUShort()),
                Pitch = LookCodec.DecodePitch(reader.ReadShort()),
                Health = reader.ReadFloat(),
                Hits = reader.ReadUShort(),
                Shots = reader.ReadUShort(),
                Killer = reader.ReadInt(),
            };
        }

        private static void WriteFloat3(PacketWriter writer, float3 value)
        {
            writer.WriteFloat(value.x);
            writer.WriteFloat(value.y);
            writer.WriteFloat(value.z);
        }

        private static float3 ReadFloat3(ref PacketReader reader)
        {
            return new float3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
        }
    }
}

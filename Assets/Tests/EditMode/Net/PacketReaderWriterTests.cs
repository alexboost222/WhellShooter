using Engine.Net;
using NUnit.Framework;

namespace Engine.Tests.Net
{
    public sealed class PacketReaderWriterTests
    {
        [Test]
        public void RoundTrip()
        {
            var writer = new PacketWriter(2);
            writer.WriteByte(200);
            writer.WriteSByte(-5);
            writer.WriteShort(-12345);
            writer.WriteInt(int.MinValue + 7);
            writer.WriteUInt(uint.MaxValue - 3);
            writer.WriteUShort(65000);
            writer.WriteFloat(-1.25e-3f);

            var reader = new PacketReader(writer.Buffer, writer.Length);

            Assert.That(reader.ReadByte(), Is.EqualTo(200));
            Assert.That(reader.ReadSByte(), Is.EqualTo(-5));
            Assert.That(reader.ReadShort(), Is.EqualTo(-12345));
            Assert.That(reader.ReadInt(), Is.EqualTo(int.MinValue + 7));
            Assert.That(reader.ReadUInt(), Is.EqualTo(uint.MaxValue - 3));
            Assert.That(reader.ReadUShort(), Is.EqualTo(65000));
            Assert.That(reader.ReadFloat(), Is.EqualTo(-1.25e-3f));
            Assert.That(reader.IsValid, Is.True);
            Assert.That(reader.Remaining, Is.Zero);
        }

        [Test]
        public void ReadingPastTheEnd_InvalidatesInsteadOfThrowing()
        {
            var reader = new PacketReader(new byte[] { 1, 2, 3 });

            Assert.That(reader.ReadInt(), Is.Zero);
            Assert.That(reader.IsValid, Is.False);
            Assert.That(reader.ReadByte(), Is.Zero, "stays invalid");
        }
    }
}

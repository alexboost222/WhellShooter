namespace Features.Network.Enums
{
    /// <summary>First byte of every datagram. Values are wire format — never renumber.</summary>
    public enum PacketType : byte
    {
        Input = 1,
        Sync = 2,
    }
}

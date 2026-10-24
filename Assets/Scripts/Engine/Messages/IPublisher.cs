namespace Engine.Messages
{
    /// <summary>
    /// Sends messages of one type. <b>Publishing never executes foreign code</b> — it appends
    /// to buffers and returns, so nothing you call can re-enter you (Docs/02).
    /// </summary>
    /// <remarks>
    /// <c>where T : struct</c> enforces the half of the message contract a compiler can:
    /// messages are plain structs. The rest of the rule — no object references, ids rather
    /// than <c>Entity</c> refs — stays a convention, and it exists so these types can go on a
    /// wire later unchanged.
    /// </remarks>
    public interface IPublisher<T> where T : struct
    {
        void Publish(in T message);
    }
}

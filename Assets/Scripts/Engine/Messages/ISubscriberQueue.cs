namespace Engine.Messages
{
    /// <summary>
    /// Pull delivery — the ECS side. A system reads everything pending in its own update.
    /// </summary>
    /// <remarks>
    /// Deliberately <b>not</b> <c>IEnumerable&lt;T&gt;</c>: a <c>foreach</c> over the interface
    /// would box an enumerator on every system, every tick. Systems inject the concrete
    /// <see cref="SubscriberQueue{T}"/> and iterate its struct enumerator instead — this
    /// interface exists for the rare consumer that only needs to read a count or index.
    /// </remarks>
    public interface ISubscriberQueue<T> where T : struct
    {
        int Count { get; }

        T this[int index] { get; }
    }
}

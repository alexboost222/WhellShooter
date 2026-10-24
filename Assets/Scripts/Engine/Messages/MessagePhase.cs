namespace Engine.Messages
{
    /// <summary>
    /// Which of the Runner's phases a <see cref="SubscriberQueue{T}"/> belongs to. The Runner
    /// swaps a queue's buffers at the start of its phase, which is what makes the contents
    /// stable for every system running in it.
    /// </summary>
    /// <remarks>
    /// The members mirror <c>RunnerPhase</c> name for name, deliberately: they are the same
    /// phases seen from the bus's side rather than the Runner's. <c>LateUpdate</c> has no member of
    /// its own — it is the tail of the same frame and shares <see cref="Update"/>.
    /// </remarks>
    public enum MessagePhase : byte
    {
        /// <summary>Fixed step: simulation systems. The default for anything ECS-side.</summary>
        FixedUpdate = 0,

        /// <summary>Every frame: view and presentation systems.</summary>
        Update = 1
    }
}

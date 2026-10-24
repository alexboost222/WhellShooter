namespace Engine.Runtime
{
    /// <summary>
    /// Where a feature's systems run. Game code never sees more than these three.
    /// </summary>
    public enum RunnerPhase : byte
    {
        /// <summary>Every fixed tick: simulation systems, followed by the physics step (phys-r, Docs/11).</summary>
        FixedUpdate = 0,

        /// <summary>Every frame: view and presentation systems.</summary>
        Update = 1,

        /// <summary>
        /// Every frame, <b>after Unity has evaluated its Animators</b>. For view systems that write
        /// a Transform which an animation would otherwise overwrite.
        /// </summary>
        /// <remarks>
        /// Unity poses animated hierarchies between <c>Update</c> and <c>LateUpdate</c> by writing
        /// <i>local</i> transforms, so anything placed in world space during <c>Update</c> on an
        /// object parented under a bone is dragged off that spot the same frame. <b>Put a system
        /// here only if an animation is fighting it</b> — input, simulation and deciding what to
        /// draw all belong earlier. It shares <c>MessagePhase.Update</c> with the phase before it:
        /// it is the tail of the same frame.
        /// </remarks>
        LateUpdate = 2
    }
}

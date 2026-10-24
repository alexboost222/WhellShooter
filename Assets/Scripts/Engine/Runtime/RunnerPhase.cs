namespace Engine.Runtime
{
    /// <summary>
    /// Where a feature's systems run. Game code never sees more than these four.
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
        LateUpdate = 2,

        /// <summary>
        /// Every fixed tick, <b>right after the physics step</b> (Docs/11): systems that turn this
        /// tick's physics result into gameplay state, so views and the next tick see it without a
        /// tick of lag. Same bus phase as <see cref="FixedUpdate"/>. Nothing here should move bodies —
        /// that waits for the next step anyway.
        /// </summary>
        AfterPhysics = 3,
    }
}

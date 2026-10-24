using Scellecs.Morpeh;

namespace Features.ViewmodelFeature.Components
{
    /// <summary>
    /// The viewmodel's memory between frames: blends, oscillator phases, and the last values of the
    /// simulation counters it reacts to.
    /// </summary>
    /// <remarks>
    /// <b>Phases are accumulated, not derived from a clock</b> — bob advances with distance walked,
    /// breathing with time — so nothing here needs elapsed time and the systems need only their
    /// frame delta.
    /// </remarks>
    public struct ViewmodelState : IComponent
    {
        /// <summary>0 hip … 1 aimed. Linear; eased when posed.</summary>
        public float AimBlend;

        /// <summary>0 … 1 sprint pose.</summary>
        public float SprintBlend;

        /// <summary>Radians, wrapped. π per footstep.</summary>
        public float BobPhase;

        /// <summary>0 … 1: how much bob is showing (speed, on the ground).</summary>
        public float BobWeight;

        /// <summary>Radians, wrapped.</summary>
        public float BreathPhase;

        /// <summary>Look the previous frame, for turn rate. Valid once <see cref="Primed"/>.</summary>
        public float LastYaw;
        public float LastPitch;
        public bool Primed;

        public uint SeenShots;
        public uint SeenJumps;
        public uint SeenLandings;
    }
}

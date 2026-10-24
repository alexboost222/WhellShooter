using Engine.DI;
using Features.Bodies.Components;
using Features.CharacterFeature.Components;
using Scellecs.Morpeh;

namespace Features.CharacterFeature.Systems
{
    /// <summary>
    /// Right after the physics step (<c>RunnerPhase.AfterPhysics</c>): phys-r's
    /// <see cref="PhysicsCharacterState"/> → <see cref="CharacterMotion"/>, the gameplay contract.
    /// Take-offs and landings bump the counters.
    /// </summary>
    /// <remarks>
    /// In the same tick as the step, so views interpolate towards this tick's result and the next
    /// tick's drivers read it — no tick of lag.
    /// </remarks>
    public sealed class CharacterMotionSystem : ISystem
    {
        [Inject] private Stash<PhysicsCharacterState> _physicsCharacterState;
        [Inject] private Stash<CharacterMotion> _characterMotion;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Character>()
                .With<CharacterMotion>()
                .With<PhysicsCharacterState>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                var state = _physicsCharacterState.Get(entity);
                ref var motion = ref _characterMotion.Get(entity);

                motion.PreviousPosition = state.PreviousFeet;
                motion.Position = state.Feet;
                motion.Velocity = state.Velocity;
                motion.Grounded = state.Grounded;

                if (state.Jumped)
                {
                    motion.Jumps++;
                }

                if (state.Landed)
                {
                    motion.Landings++;
                    motion.LandingSpeed = state.LandingSpeed;
                }
            }
        }
    }
}

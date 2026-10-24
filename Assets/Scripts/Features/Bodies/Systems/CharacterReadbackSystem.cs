using Engine.DI;
using Engine.Physics;
using Features.Bodies.Components;
using PhysR;
using Scellecs.Morpeh;

namespace Features.Bodies.Systems
{
    /// <summary>Right after the step: native character state → <see cref="PhysicsCharacterState"/>.</summary>
    public sealed class CharacterReadbackSystem : ISystem
    {
        [Inject] private Stash<PhysicsCharacter> _physicsCharacter;
        [Inject] private Stash<PhysicsCharacterState> _physicsCharacterState;

        [Inject] private PhysicsWorld _physics;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PhysicsCharacter>()
                .With<PhysicsCharacterState>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var native = _physics.Native;
            foreach (var entity in _filter)
            {
                var handle = new CharacterHandle(_physicsCharacter.Get(entity).Handle);
                if (!native.TryGetState(handle, out CharacterState state))
                {
                    continue;
                }

                ref var current = ref _physicsCharacterState.Get(entity);
                current.PreviousFeet = current.Feet;
                current.Feet = PhysicsMath.ToFloat3(state.Feet);
                current.Velocity = PhysicsMath.ToFloat3(state.Velocity);
                current.Grounded = state.IsGrounded;
                current.Crouched = state.IsCrouched;
                current.Landed = (state.Collision & CollisionFlags.Landed) != 0;
                current.Jumped = (state.Collision & CollisionFlags.Jumped) != 0;
                current.Height = state.Height.ToFloat();
                current.LandingSpeed = state.LandingSpeed.ToFloat();
            }
        }
    }
}

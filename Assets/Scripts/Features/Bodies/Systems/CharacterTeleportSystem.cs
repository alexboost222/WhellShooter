using Engine.DI;
using Engine.Physics;
using Features.Bodies.Components;
using PhysR;
using Scellecs.Morpeh;

namespace Features.Bodies.Systems
{
    /// <summary>
    /// Consumes <see cref="PhysicsTeleportRequest"/> (read-then-remove) right before the step and
    /// snaps the character there. The previous feet are snapped too, so nothing interpolates
    /// across the jump.
    /// </summary>
    public sealed class CharacterTeleportSystem : ISystem
    {
        [Inject] private Stash<PhysicsTeleportRequest> _physicsTeleportRequest;
        [Inject] private Stash<PhysicsCharacter> _physicsCharacter;
        [Inject] private Stash<PhysicsCharacterState> _physicsCharacterState;

        [Inject] private PhysicsWorld _physics;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PhysicsTeleportRequest>()
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
                var feet = _physicsTeleportRequest.Get(entity).Feet;
                _physicsTeleportRequest.Remove(entity);

                if (!_physicsCharacter.Has(entity))
                {
                    continue;
                }

                var handle = new CharacterHandle(_physicsCharacter.Get(entity).Handle);
                if (!native.TryGetState(handle, out CharacterState state))
                {
                    continue;
                }

                native.Teleport(handle, PhysicsMath.ToFP(feet) + FPVector3.Up * (state.Height / 2));
                native.SetVelocity(handle, FPVector3.Zero);

                if (_physicsCharacterState.Has(entity))
                {
                    ref var current = ref _physicsCharacterState.Get(entity);
                    current.Feet = feet;
                    current.PreviousFeet = feet;
                    current.Velocity = default;
                }
            }
        }
    }
}

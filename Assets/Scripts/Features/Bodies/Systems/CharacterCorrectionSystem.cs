using Engine.DI;
using Engine.Physics;
using Features.Bodies.Components;
using PhysR;
using Scellecs.Morpeh;

namespace Features.Bodies.Systems
{
    /// <summary>
    /// Consumes <see cref="PhysicsCharacterCorrection"/> (read-then-remove) right before the step:
    /// the character's capsule and velocity move by the deltas.
    /// </summary>
    public sealed class CharacterCorrectionSystem : ISystem
    {
        [Inject] private Stash<PhysicsCharacterCorrection> _physicsCharacterCorrection;
        [Inject] private Stash<PhysicsCharacter> _physicsCharacter;

        [Inject] private PhysicsWorld _physics;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PhysicsCharacterCorrection>()
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
                var correction = _physicsCharacterCorrection.Get(entity);
                _physicsCharacterCorrection.Remove(entity);

                if (!_physicsCharacter.Has(entity))
                {
                    continue;
                }

                var handle = new CharacterHandle(_physicsCharacter.Get(entity).Handle);
                if (!native.TryGetState(handle, out CharacterState state))
                {
                    continue;
                }

                native.Teleport(handle, state.Position + PhysicsMath.ToFP(correction.FeetDelta));
                native.SetVelocity(handle, state.Velocity + PhysicsMath.ToFP(correction.VelocityDelta));
            }
        }
    }
}

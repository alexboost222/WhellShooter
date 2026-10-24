using Engine.DI;
using Engine.Physics;
using Features.Bodies.Components;
using PhysR;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bodies.Systems
{
    /// <summary>
    /// Pushes every character's <see cref="PhysicsCharacterInput"/> into phys-r right before the step.
    /// A character without one is given a still input, so last tick's never repeats.
    /// </summary>
    public sealed class CharacterInputSystem : ISystem
    {
        [Inject] private Stash<PhysicsCharacter> _physicsCharacter;
        [Inject] private Stash<PhysicsCharacterInput> _physicsCharacterInput;

        [Inject] private PhysicsWorld _physics;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PhysicsCharacter>()
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
                var input = _physicsCharacterInput.Has(entity) ? _physicsCharacterInput.Get(entity) : default;

                var buttons = CharacterButtons.None;
                if (input.Jump)
                {
                    buttons |= CharacterButtons.Jump;
                }

                if (input.Sprint)
                {
                    buttons |= CharacterButtons.Sprint;
                }

                if (input.Crouch)
                {
                    buttons |= CharacterButtons.Crouch;
                }

                // phys-r scales the top speed by the wish vector's length (clamped to 1), so a
                // speed cap is a shorter wish vector.
                var move = math.clamp(input.Move, -1f, 1f);
                if (math.lengthsq(move) > 1f)
                {
                    move = math.normalize(move);
                }

                if (input.MaxSpeedScale > 0f)
                {
                    move *= math.min(input.MaxSpeedScale, 1f);
                }

                native.SetInput(handle, new CharacterInput
                {
                    MoveX = PhysicsMath.ToFP(move.x),
                    MoveZ = PhysicsMath.ToFP(move.y),
                    Yaw = PhysicsMath.ToFP(input.Yaw),
                    Buttons = buttons,
                });
            }
        }
    }
}

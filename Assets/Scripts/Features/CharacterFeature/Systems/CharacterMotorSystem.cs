using Engine.DI;
using Features.Bodies.Components;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.CharacterFeature.Utils;
using Scellecs.Morpeh;

namespace Features.CharacterFeature.Systems
{
    /// <summary>
    /// Turns every body's <see cref="CharacterIntent"/> and <see cref="Look"/> into this tick's
    /// <see cref="PhysicsCharacterInput"/> for phys-r (Docs/11): move, yaw, sprint, aim speed, jump.
    /// </summary>
    /// <remarks>
    /// <b>phys-r is authoritative for position</b> — it knows the walls. It moves the capsule in the
    /// physics step right after this tick's FixedUpdate, and <see cref="CharacterMotionSystem"/>
    /// copies the result into <see cref="CharacterMotion"/>. Steered by <b>yaw only</b>: looking at
    /// the sky and walking forward must not lift anyone off the ground.
    /// </remarks>
    public sealed class CharacterMotorSystem : ISystem
    {
        [Inject] private Stash<CharacterIntent> _characterIntent;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<PhysicsCharacterInput> _physicsCharacterInput;

        [Inject] private CharacterConfig _config;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Character>()
                .With<CharacterIntent>()
                .With<Look>()
                .With<CharacterMotion>()
                .With<PhysicsCharacter>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref var intent = ref _characterIntent.Get(entity);
                ref var motion = ref _characterMotion.Get(entity);

                motion.Sprinting = CharacterMovement.WantsSprint(intent.Sprint, intent.Move, intent.Aim, intent.Fire);

                _physicsCharacterInput.Set(entity, new PhysicsCharacterInput
                {
                    Move = intent.Move,
                    Yaw = _look.Get(entity).Yaw,
                    Sprint = motion.Sprinting,
                    MaxSpeedScale = !motion.Sprinting && intent.Aim
                        ? CharacterMovement.AimSpeedScale(_config.AimSpeed, _config.WalkSpeed)
                        : 0f,
                    Jump = intent.JumpPressed && motion.Grounded,
                });

                intent.JumpPressed = false;
            }
        }
    }
}

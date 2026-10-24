using Engine.DI;
using Features.Bodies.Components;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.CharacterFeature.Utils;
using Scellecs.Morpeh;

namespace Features.CharacterFeature.Systems
{
    /// <summary>
    /// Gives every new body its phys-r capsule: a <see cref="CharacterCreateRequest"/> at its feet,
    /// sized and tuned from <see cref="CharacterConfig"/>, so the player and bots move by the same
    /// numbers. Whoever spawns a body only adds <see cref="Character"/> and <see cref="CharacterMotion"/>.
    /// </summary>
    public sealed class CharacterBodySpawnSystem : ISystem
    {
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<CharacterCreateRequest> _characterCreateRequest;

        [Inject] private CharacterConfig _config;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Character>()
                .With<CharacterMotion>()
                .Without<PhysicsCharacter>()
                .Without<CharacterCreateRequest>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                _characterCreateRequest.Set(entity, new CharacterCreateRequest
                {
                    Feet = _characterMotion.Get(entity).Position,
                    Radius = _config.Radius,
                    Height = _config.Height,
                    WalkSpeed = _config.WalkSpeed,
                    SprintSpeed = _config.SprintSpeed,
                    GroundAccel = CharacterMovement.QuakeAccel(_config.GroundAcceleration, _config.WalkSpeed),
                    AirAccel = CharacterMovement.QuakeAccel(_config.AirAcceleration, _config.WalkSpeed),
                    Gravity = _config.Gravity,
                    JumpHeight = _config.JumpHeight,
                });
            }
        }
    }
}

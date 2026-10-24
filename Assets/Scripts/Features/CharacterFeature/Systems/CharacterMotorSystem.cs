using Engine.DI;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.CharacterFeature.Utils;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace Features.CharacterFeature.Systems
{
    /// <summary>
    /// Moves every body on the fixed tick: walk/sprint/aim speed, jump, gravity, and the
    /// <c>CharacterController</c> sweep. Writes everything it learns back into
    /// <see cref="CharacterMotion"/>.
    /// </summary>
    /// <remarks>
    /// <b>The controller is authoritative for position</b> — it is the thing that knows about walls —
    /// so the pose is read back from it after the move, the same sanctioned read-back a physics body
    /// gets (Docs/03). A placeholder until the phys-r character controller replaces it; nothing
    /// outside this system knows a <c>CharacterController</c> exists.
    /// <para>
    /// Steered by <b>yaw only</b>: looking at the sky and walking forward must not lift anyone off the
    /// ground. A body without a view yet does not move — there is nothing to collide with.
    /// </para>
    /// </remarks>
    public sealed class CharacterMotorSystem : ISystem
    {
        [Inject] private Stash<CharacterIntent> _characterIntent;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<CharacterViewLink> _characterViewLink;

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
                .With<CharacterViewLink>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            foreach (var entity in _filter)
            {
                ref var intent = ref _characterIntent.Get(entity);
                ref var motion = ref _characterMotion.Get(entity);
                var heading = _look.Get(entity).Heading;
                var controller = _characterViewLink.Get(entity).View.Controller;

                motion.Sprinting = CharacterMovement.WantsSprint(intent.Sprint, intent.Move, intent.Aim, intent.Fire);
                var speed = motion.Sprinting
                    ? _config.SprintSpeed
                    : intent.Aim ? _config.AimSpeed : _config.WalkSpeed;
                var acceleration = motion.Grounded ? _config.GroundAcceleration : _config.AirAcceleration;

                var velocity = CharacterMovement.Horizontal(motion.Velocity, intent.Move, heading, speed,
                    acceleration, deltaTime);
                velocity.y = motion.Velocity.y;

                if (intent.JumpPressed && motion.Grounded)
                {
                    velocity.y = CharacterMovement.JumpSpeed(_config.JumpHeight, _config.Gravity);
                    motion.Jumps++;
                }

                intent.JumpPressed = false;
                velocity.y -= _config.Gravity * deltaTime;

                var wasGrounded = motion.Grounded;
                motion.PreviousPosition = motion.Position;

                var flags = controller.Move((Vector3)(velocity * deltaTime));

                motion.Position = controller.transform.position;
                motion.Grounded = controller.isGrounded;

                // Horizontal velocity is what actually happened, so pushing into a wall does not bank
                // speed that is spent the moment the wall ends.
                var moved = (motion.Position - motion.PreviousPosition) / deltaTime;
                velocity.x = moved.x;
                velocity.z = moved.z;

                if (motion.Grounded && !wasGrounded)
                {
                    motion.Landings++;
                    motion.LandingSpeed = math.max(0f, -velocity.y);
                }

                if (motion.Grounded && velocity.y < 0f)
                {
                    velocity.y = -CharacterMovement.GroundStickSpeed;
                }

                if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f)
                {
                    velocity.y = 0f;
                }

                motion.Velocity = velocity;
            }
        }
    }
}

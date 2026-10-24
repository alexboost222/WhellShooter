using Engine.Configs;
using UnityEngine;

namespace Features.CharacterFeature.Configs
{
    /// <summary>
    /// How a body moves — the same numbers for the player and for bots, so neither has an edge the
    /// other cannot have. One asset, in <c>Resources/Configs</c>. Reasons: Docs/08 §Character.
    /// </summary>
    /// <remarks>
    /// <b>Every number that is meaningless at zero falls back to its default.</b> A field added after
    /// the asset was last written deserialises as zero — the initialiser never runs for an existing
    /// asset — and a zero speed reads like a broken motor rather than a missing value.
    /// </remarks>
    [CreateAssetMenu(menuName = "WheelShooter/Character Config", fileName = "CharacterConfig")]
    public sealed class CharacterConfig : Config
    {
        private const float DefaultHeight = 1.8f;
        private const float DefaultRadius = 0.35f;
        private const float DefaultEyeHeight = 1.6f;
        private const float DefaultWalkSpeed = 4.5f;
        private const float DefaultSprintSpeed = 7f;
        private const float DefaultAimSpeed = 2.8f;
        private const float DefaultGroundAcceleration = 45f;
        private const float DefaultAirAcceleration = 8f;
        private const float DefaultJumpHeight = 1.1f;
        private const float DefaultGravity = 22f;

        [Tooltip("Capsule height, feet to crown, metres. The CharacterController is set to it when the view is built, and hitscan tests against it.")]
        [SerializeField] private float height = DefaultHeight;

        [Tooltip("Capsule radius, metres.")]
        [SerializeField] private float radius = DefaultRadius;

        [Tooltip("Head height above the feet, metres. The camera and the shot origin are here.")]
        [SerializeField] private float eyeHeight = DefaultEyeHeight;

        [Tooltip("Ground speed walking, m/s.")]
        [SerializeField] private float walkSpeed = DefaultWalkSpeed;

        [Tooltip("Ground speed sprinting, m/s. Sprint needs forward input and is dropped by aiming or firing.")]
        [SerializeField] private float sprintSpeed = DefaultSprintSpeed;

        [Tooltip("Ground speed while aiming down sights, m/s.")]
        [SerializeField] private float aimSpeed = DefaultAimSpeed;

        [Tooltip("How fast velocity reaches the wanted speed on the ground, m/s².")]
        [SerializeField] private float groundAcceleration = DefaultGroundAcceleration;

        [Tooltip("The same in the air — low, so a jump commits you.")]
        [SerializeField] private float airAcceleration = DefaultAirAcceleration;

        [Tooltip("Apex of a standing jump, metres.")]
        [SerializeField] private float jumpHeight = DefaultJumpHeight;

        [Tooltip("m/s². Higher than 9.81 on purpose: real gravity feels floaty in a shooter.")]
        [SerializeField] private float gravity = DefaultGravity;

        public float Height => Positive(height, DefaultHeight);
        public float Radius => Positive(radius, DefaultRadius);
        public float EyeHeight => Positive(eyeHeight, DefaultEyeHeight);
        public float WalkSpeed => Positive(walkSpeed, DefaultWalkSpeed);
        public float SprintSpeed => Positive(sprintSpeed, DefaultSprintSpeed);
        public float AimSpeed => Positive(aimSpeed, DefaultAimSpeed);
        public float GroundAcceleration => Positive(groundAcceleration, DefaultGroundAcceleration);
        public float AirAcceleration => Positive(airAcceleration, DefaultAirAcceleration);
        public float JumpHeight => Positive(jumpHeight, DefaultJumpHeight);
        public float Gravity => Positive(gravity, DefaultGravity);

        private static float Positive(float value, float fallback)
        {
            return value > 0f ? value : fallback;
        }
    }
}

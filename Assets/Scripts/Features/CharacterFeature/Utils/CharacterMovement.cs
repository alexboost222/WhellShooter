using Engine.Motion;
using Unity.Mathematics;

namespace Features.CharacterFeature.Utils
{
    /// <summary>
    /// The motor's decisions as pure functions, so they are tested without a physics world.
    /// </summary>
    public static class CharacterMovement
    {
        /// <summary>Forward input above this counts as "running forwards" for sprint.</summary>
        public const float SprintForwardThreshold = 0.5f;

        /// <summary>Downward speed kept while grounded so the capsule stays on slopes and steps.</summary>
        public const float GroundStickSpeed = 2f;

        /// <summary>
        /// Sprint needs the button, forward input, and neither aiming nor firing — pulling the trigger
        /// drops you out of a sprint rather than shooting from the hip at a run.
        /// </summary>
        public static bool WantsSprint(bool sprint, float2 move, bool aim, bool fire)
        {
            return sprint && move.y > SprintForwardThreshold && !aim && !fire;
        }

        /// <summary>
        /// Horizontal velocity after one step: towards <paramref name="move"/> (strafe, forward)
        /// rotated by <paramref name="heading"/>, at <paramref name="speed"/>, changing by at most
        /// <paramref name="acceleration"/>·dt. The result has no vertical component.
        /// </summary>
        public static float3 Horizontal(float3 velocity, float2 move, quaternion heading, float speed,
            float acceleration, float deltaTime)
        {
            var wish = math.mul(heading, new float3(move.x, 0f, move.y)) * speed;
            var current = new float3(velocity.x, 0f, velocity.z);

            return Springs.MoveTowards(current, new float3(wish.x, 0f, wish.z), acceleration * deltaTime);
        }

        /// <summary>
        /// phys-r's speed cap while aiming: the fraction of walk speed (phys-r shortens the wish vector).
        /// </summary>
        public static float AimSpeedScale(float aimSpeed, float walkSpeed)
        {
            return walkSpeed > 0f ? math.saturate(aimSpeed / walkSpeed) : 1f;
        }

        /// <summary>
        /// A linear acceleration (m/s², how fast velocity reaches walk speed) as phys-r's Quake-style
        /// one (1/s, which phys-r multiplies by the wanted speed): the same time to reach walk speed.
        /// </summary>
        public static float QuakeAccel(float acceleration, float walkSpeed)
        {
            return walkSpeed > 0f ? acceleration / walkSpeed : acceleration;
        }

        /// <summary>Take-off speed for a jump that peaks at <paramref name="height"/>.</summary>
        public static float JumpSpeed(float height, float gravity)
        {
            return math.sqrt(2f * gravity * height);
        }
    }
}

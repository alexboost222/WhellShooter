using Engine.Motion;
using Unity.Mathematics;

namespace Features.CharacterFeature.Utils
{
    /// <summary>
    /// The motor's decisions as pure functions, so they are tested without a CharacterController.
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

        /// <summary>Take-off speed for a jump that peaks at <paramref name="height"/>.</summary>
        public static float JumpSpeed(float height, float gravity)
        {
            return math.sqrt(2f * gravity * height);
        }
    }
}

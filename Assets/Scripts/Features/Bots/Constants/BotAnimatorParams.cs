using UnityEngine;

namespace Features.Bots.Constants
{
    /// <summary>
    /// The bot Animator's parameters — the contract between the bot view system and the
    /// controllers in <c>Content/Bots/Animators</c> (built by <c>Game.Editor.Bots.BotAnimatorBuilder</c>
    /// from these same names). Docs/08 → Bots → Animation.
    /// </summary>
    public static class BotAnimatorParams
    {
        public const string MoveXName = "MoveX";
        public const string MoveZName = "MoveZ";
        public const string GroundedName = "Grounded";
        public const string CrouchName = "Crouch";
        public const string AimName = "Aim";
        public const string DeadName = "Dead";
        public const string DeathVariantName = "DeathVariant";

        /// <summary>How many death clips every controller has; <see cref="DeathVariant"/> is 0..Count-1.</summary>
        public const int DeathVariantCount = 6;

        /// <summary>Planar velocity in the bot's local frame, m/s, x = right.</summary>
        public static readonly int MoveX = Animator.StringToHash(MoveXName);

        /// <summary>Planar velocity in the bot's local frame, m/s, z = forward.</summary>
        public static readonly int MoveZ = Animator.StringToHash(MoveZName);

        /// <summary>False → jump up and hang; true again → land.</summary>
        public static readonly int Grounded = Animator.StringToHash(GroundedName);

        public static readonly int Crouch = Animator.StringToHash(CrouchName);

        /// <summary>0..1: standing idle → aiming idle.</summary>
        public static readonly int Aim = Animator.StringToHash(AimName);

        /// <summary>True → the death picked by <see cref="DeathVariant"/>; false → back to locomotion.</summary>
        public static readonly int Dead = Animator.StringToHash(DeadName);

        public static readonly int DeathVariant = Animator.StringToHash(DeathVariantName);
    }
}

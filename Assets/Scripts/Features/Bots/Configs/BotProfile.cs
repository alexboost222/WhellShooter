using System;
using UnityEngine;

namespace Features.Bots.Configs
{
    /// <summary>
    /// One skill level. A bot stores the hash of <see cref="Name"/>; the numbers live here only.
    /// Defaults are the "Regular" row of Docs/08 §Bots.
    /// </summary>
    [Serializable]
    public sealed class BotProfile
    {
        [Tooltip("Stable name — its hash is what a bot stores. Renaming is a content break.")]
        public string Name = "Regular";

        [Header("Senses")]
        [Tooltip("How far the bot sees, metres.")]
        public float ViewDistance = 45f;

        [Tooltip("Field of view, degrees, centred on where it looks.")]
        [Range(30f, 360f)] public float FieldOfView = 120f;

        [Tooltip("Closer than this, an enemy is noticed whatever the bot faces, metres.")]
        public float AwarenessRadius = 4f;

        [Tooltip("Seconds a lost target is remembered and hunted.")]
        public float Memory = 6f;

        [Header("Aim")]
        [Tooltip("Seconds an enemy must be in sight before the first shot.")]
        public float ReactionTime = 0.35f;

        [Tooltip("Aim wobble when first acquiring a target, degrees.")]
        public float AimError = 3f;

        [Tooltip("Share of the wobble left once settled on a target.")]
        [Range(0f, 1f)] public float SettledAimError = 0.3f;

        [Tooltip("Seconds of tracking to settle.")]
        public float AimSettleTime = 1.2f;

        [Tooltip("Fastest turn, degrees per second.")]
        public float TurnSpeed = 300f;

        [Tooltip("How snappily the look closes the gap — higher = more robotic.")]
        public float TurnSharpness = 10f;

        [Tooltip("Fire once the look is within this many degrees of the (wobbling) aim point.")]
        public float FireTolerance = 2.5f;

        [Header("Fighting")]
        [Tooltip("Distance the bot likes to fight at, metres.")]
        public float PreferredRange = 14f;

        [Tooltip("Seconds between strafe moves while fighting (min, max).")]
        public Vector2 StrafeInterval = new Vector2(0.8f, 2f);

        [Tooltip("Strafe distance, metres (min, max).")]
        public Vector2 StrafeDistance = new Vector2(2f, 4f);
    }
}

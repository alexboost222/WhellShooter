namespace Game.Editor.Bots
{
    /// <summary>
    /// Which imported clips one bot controller is built from. Clip names are the FBX file names
    /// under <see cref="BotAnimationImporter.Root"/> (<c>Rifle_WalkForward</c> → <c>Rifle/Rifle_WalkForward.fbx</c>).
    /// </summary>
    /// <remarks>
    /// Locomotion clips carry no position of their own: the builder places each one in the blend
    /// tree at its measured average velocity, so the tree is exact for whatever speed ECS moves at.
    /// </remarks>
    public sealed class BotAnimationSet
    {
        public string Name;
        public string Idle;
        public string IdleAim;
        public string[] Locomotion;
        public string CrouchIdle;
        public string CrouchIdleAim;
        public string[] CrouchLocomotion;
        public string JumpUp;
        public string JumpLoop;
        public string JumpDown;
        public string[] Deaths;

        private static readonly string[] Directions =
        {
            "Forward", "ForwardRight", "Right", "BackwardRight",
            "Backward", "BackwardLeft", "Left", "ForwardLeft",
        };

        private static readonly string[] RifleDeaths =
        {
            "Rifle_DeathFromTheFront",
            "Rifle_DeathFromTheBack",
            "Rifle_DeathFromRight",
            "Rifle_DeathFromFrontHeadshot",
            "Rifle_DeathFromBackHeadshot",
            "Rifle_DeathCrouchingHeadshotFront",
        };

        /// <summary>The full Mixamo Rifle 8-Way pack: walk / run / sprint and crouch-walk in 8 directions.</summary>
        public static readonly BotAnimationSet Rifle = new BotAnimationSet
        {
            Name = "Bot_Rifle",
            Idle = "Rifle_Idle",
            IdleAim = "Rifle_IdleAiming",
            Locomotion = Concat(EightWay("Rifle_Walk"), EightWay("Rifle_Run"), EightWay("Rifle_Sprint")),
            CrouchIdle = "Rifle_IdleCrouching",
            CrouchIdleAim = "Rifle_IdleCrouchingAiming",
            CrouchLocomotion = EightWay("Rifle_WalkCrouching"),
            JumpUp = "Rifle_JumpUp",
            JumpLoop = "Rifle_JumpLoop",
            JumpDown = "Rifle_JumpDown",
            Deaths = RifleDeaths,
        };

        /// <summary>
        /// The Mixamo Pistol pack: 4-way walk, forward/back run, kneel instead of crouch-walk. It has
        /// no airborne loop and no deaths, so those come from the rifle pack (retargeted, same rig).
        /// The pack's "arc" clips turn the body mid-cycle and are not used.
        /// </summary>
        public static readonly BotAnimationSet Pistol = new BotAnimationSet
        {
            Name = "Bot_Pistol",
            Idle = "Pistol_Idle",
            IdleAim = null,
            Locomotion = new[]
            {
                "Pistol_Walk", "Pistol_WalkBackward", "Pistol_Strafe", "Pistol_Strafe2",
                "Pistol_Run", "Pistol_RunBackward",
            },
            CrouchIdle = "Pistol_KneelingIdle",
            CrouchIdleAim = null,
            CrouchLocomotion = new string[0],
            JumpUp = "Rifle_JumpUp",
            JumpLoop = "Rifle_JumpLoop",
            JumpDown = "Rifle_JumpDown",
            Deaths = RifleDeaths,
        };

        public static readonly BotAnimationSet[] All = { Rifle, Pistol };

        private static string[] EightWay(string prefix)
        {
            var names = new string[Directions.Length];

            for (var i = 0; i < Directions.Length; i++)
            {
                names[i] = prefix + Directions[i];
            }

            return names;
        }

        private static string[] Concat(params string[][] parts)
        {
            var list = new System.Collections.Generic.List<string>();

            foreach (var part in parts)
            {
                list.AddRange(part);
            }

            return list.ToArray();
        }
    }
}

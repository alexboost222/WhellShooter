using System.Collections.Generic;
using Features.Bots.Constants;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Editor.Bots
{
    /// <summary>
    /// Generates the bot Animator controllers from <see cref="BotAnimationSet"/> — never hand-edit
    /// them, rebuild instead (<b>WheelShooter → Bots → Rebuild Animators</b>). A rebuild keeps the
    /// asset and its GUID, so prefabs referencing it stay wired.
    /// </summary>
    /// <remarks>
    /// One layer (IK Pass on), driven only by <see cref="BotAnimatorParams"/>:
    /// <code>
    /// Locomotion ⇄ CrouchLocomotion        (Crouch)
    /// both → JumpUp → JumpLoop → JumpDown   (!Grounded … Grounded)
    /// AnyState → Death_i                    (Dead &amp;&amp; DeathVariant == i), back on !Dead
    /// </code>
    /// The locomotion trees are 2D freeform-directional on (MoveX, MoveZ), every clip at its own
    /// measured average velocity; the idle at the centre is itself a 1D tree on Aim.
    /// </remarks>
    public static class BotAnimatorBuilder
    {
        public const string Folder = "Assets/Content/Bots/Animators/";

        private const float BlendDuration = 0.15f;

        [MenuItem("WheelShooter/Bots/Rebuild Animators")]
        public static void BuildAll()
        {
            foreach (var set in BotAnimationSet.All)
            {
                Build(set);
            }

            AssetDatabase.SaveAssets();
        }

        public static string PathOf(BotAnimationSet set)
        {
            return Folder + set.Name + ".controller";
        }

        public static AnimatorController Build(BotAnimationSet set)
        {
            if (!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))
            {
                AssetDatabase.CreateFolder("Assets/Content/Bots", "Animators");
            }

            var controller = LoadEmptied(PathOf(set));
            AddParameters(controller);

            // The support hand is IK'd onto the weapon (WeaponHandIK).
            var layers = controller.layers;
            layers[0].iKPass = true;
            controller.layers = layers;

            var machine = controller.layers[0].stateMachine;

            var locomotion = machine.AddState("Locomotion", new Vector3(300, 0));
            locomotion.motion = Locomotion(controller, "Locomotion", set.Idle, set.IdleAim, set.Locomotion);
            machine.defaultState = locomotion;

            var crouch = machine.AddState("CrouchLocomotion", new Vector3(300, 120));
            crouch.motion = Locomotion(controller, "CrouchLocomotion", set.CrouchIdle, set.CrouchIdleAim, set.CrouchLocomotion);

            var jumpUp = machine.AddState("JumpUp", new Vector3(600, -120));
            jumpUp.motion = Clip(set.JumpUp);
            var jumpLoop = machine.AddState("JumpLoop", new Vector3(850, -120));
            jumpLoop.motion = Clip(set.JumpLoop);
            var jumpDown = machine.AddState("JumpDown", new Vector3(850, 0));
            jumpDown.motion = Clip(set.JumpDown);

            Connect(locomotion, crouch, BlendDuration, If(BotAnimatorParams.CrouchName, true));
            Connect(crouch, locomotion, BlendDuration, If(BotAnimatorParams.CrouchName, false));

            Connect(locomotion, jumpUp, 0.1f, If(BotAnimatorParams.GroundedName, false));
            Connect(crouch, jumpUp, 0.1f, If(BotAnimatorParams.GroundedName, false));

            ConnectOnExit(jumpUp, jumpLoop, 0.9f, 0.1f);
            Connect(jumpUp, jumpDown, 0.1f, If(BotAnimatorParams.GroundedName, true));
            Connect(jumpLoop, jumpDown, 0.1f, If(BotAnimatorParams.GroundedName, true));
            ConnectOnExit(jumpDown, locomotion, 0.5f, 0.25f);

            for (var i = 0; i < set.Deaths.Length; i++)
            {
                var death = machine.AddState("Death_" + i, new Vector3(0, 250 + i * 60));
                death.motion = Clip(set.Deaths[i]);

                var enter = machine.AddAnyStateTransition(death);
                enter.canTransitionToSelf = false;
                enter.duration = BlendDuration;
                enter.AddCondition(AnimatorConditionMode.If, 0, BotAnimatorParams.DeadName);
                enter.AddCondition(AnimatorConditionMode.Equals, i, BotAnimatorParams.DeathVariantName);

                Connect(death, locomotion, 0.25f, If(BotAnimatorParams.DeadName, false));
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>Loads the controller and strips it to a bare base layer, or creates it.</summary>
        private static AnimatorController LoadEmptied(string path)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

            if (controller == null)
            {
                return AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset != null && asset != controller)
                {
                    Object.DestroyImmediate(asset, true);
                }
            }

            var machine = new AnimatorStateMachine
            {
                name = "Base Layer",
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(machine, controller);

            controller.parameters = new AnimatorControllerParameter[0];
            controller.layers = new[]
            {
                new AnimatorControllerLayer
                {
                    name = "Base Layer",
                    defaultWeight = 1f,
                    stateMachine = machine,
                },
            };

            return controller;
        }

        private static void AddParameters(AnimatorController controller)
        {
            controller.AddParameter(BotAnimatorParams.MoveXName, AnimatorControllerParameterType.Float);
            controller.AddParameter(BotAnimatorParams.MoveZName, AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = BotAnimatorParams.GroundedName,
                type = AnimatorControllerParameterType.Bool,
                defaultBool = true,
            });
            controller.AddParameter(BotAnimatorParams.CrouchName, AnimatorControllerParameterType.Bool);
            controller.AddParameter(BotAnimatorParams.AimName, AnimatorControllerParameterType.Float);
            controller.AddParameter(BotAnimatorParams.DeadName, AnimatorControllerParameterType.Bool);
            controller.AddParameter(BotAnimatorParams.DeathVariantName, AnimatorControllerParameterType.Int);
        }

        private static Motion Locomotion(AnimatorController controller, string name, string idle, string idleAim, string[] moves)
        {
            var idleMotion = Idle(controller, name + "Idle", idle, idleAim);

            if (moves.Length == 0)
            {
                return idleMotion;
            }

            var tree = NewTree(controller, name);
            tree.blendType = BlendTreeType.FreeformDirectional2D;
            tree.blendParameter = BotAnimatorParams.MoveXName;
            tree.blendParameterY = BotAnimatorParams.MoveZName;
            tree.AddChild(idleMotion, Vector2.zero);

            foreach (var move in moves)
            {
                var clip = Clip(move);
                tree.AddChild(clip, new Vector2(clip.averageSpeed.x, clip.averageSpeed.z));
            }

            return tree;
        }

        private static Motion Idle(AnimatorController controller, string name, string idle, string idleAim)
        {
            if (idleAim == null)
            {
                return Clip(idle);
            }

            var tree = NewTree(controller, name);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = BotAnimatorParams.AimName;
            tree.useAutomaticThresholds = false;
            tree.AddChild(Clip(idle), 0f);
            tree.AddChild(Clip(idleAim), 1f);
            return tree;
        }

        private static BlendTree NewTree(AnimatorController controller, string name)
        {
            var tree = new BlendTree
            {
                name = name,
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            return tree;
        }

        /// <summary>The clip of an imported Mixamo file, by file name.</summary>
        public static AnimationClip Clip(string name)
        {
            var pack = name.Substring(0, name.IndexOf('_'));
            var path = BotAnimationImporter.Root + pack + "/" + name + ".fbx";

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }

            throw new System.IO.FileNotFoundException($"No animation clip in '{path}'.", path);
        }

        private static KeyValuePair<string, bool> If(string parameter, bool value)
        {
            return new KeyValuePair<string, bool>(parameter, value);
        }

        private static void Connect(AnimatorState from, AnimatorState to, float duration, KeyValuePair<string, bool> condition)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(condition.Value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, condition.Key);
        }

        private static void ConnectOnExit(AnimatorState from, AnimatorState to, float exitTime, float duration)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = duration;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Features.Bots.Constants;
using Features.Bots.Providers;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Bots
{
    /// <summary>
    /// The imported Mixamo clips, the generated controllers and the visual prefabs agree with the
    /// <see cref="BotAnimatorParams"/> contract (Docs/08 → Bots → Animation).
    /// </summary>
    public sealed class BotAnimationAssetsTests
    {
        private const string AnimationsRoot = "Assets/Content/Bots/Animations";

        private static readonly string[] Controllers =
        {
            "Assets/Content/Bots/Animators/Bot_Rifle.controller",
            "Assets/Content/Bots/Animators/Bot_Pistol.controller",
        };

        private static readonly string[] Prefabs =
        {
            "Assets/Content/Bots/Prefabs/Bot_Rifle.prefab",
            "Assets/Content/Bots/Prefabs/Bot_Pistol.prefab",
        };

        private static IEnumerable<string> AnimationFiles()
        {
            return AssetDatabase.FindAssets("t:Model", new[] { AnimationsRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path);
        }

        [Test]
        public void Packs_AreImported()
        {
            Assert.That(AnimationFiles().Count(), Is.EqualTo(69));
        }

        [TestCaseSource(nameof(AnimationFiles))]
        public void Clip_IsHumanoid_NamedAfterFile_LoopedUnlessOneShot(string path)
        {
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
            var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__"))
                .ToArray();
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            var oneShot = name.Contains("Death")
                || name.Contains("Turn90")
                || (name.Contains("Jump") && !name.Contains("JumpLoop"))
                || (name.Contains("Kneel") && !name.Contains("KneelingIdle"));

            Assert.That(avatar != null && avatar.isValid && avatar.isHuman, Is.True, "humanoid avatar");
            Assert.That(clips.Length, Is.EqualTo(1));
            Assert.That(clips[0].name, Is.EqualTo(name));
            Assert.That(clips[0].isHumanMotion, Is.True);
            Assert.That(clips[0].isLooping, Is.EqualTo(!oneShot));
        }

        [TestCaseSource(nameof(Controllers))]
        public void Controller_HasEveryContractParameter(string path)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            var parameters = controller.parameters.ToDictionary(parameter => parameter.name);

            AssertParameter(parameters, BotAnimatorParams.MoveXName, AnimatorControllerParameterType.Float);
            AssertParameter(parameters, BotAnimatorParams.MoveZName, AnimatorControllerParameterType.Float);
            AssertParameter(parameters, BotAnimatorParams.GroundedName, AnimatorControllerParameterType.Bool);
            AssertParameter(parameters, BotAnimatorParams.CrouchName, AnimatorControllerParameterType.Bool);
            AssertParameter(parameters, BotAnimatorParams.AimName, AnimatorControllerParameterType.Float);
            AssertParameter(parameters, BotAnimatorParams.DeadName, AnimatorControllerParameterType.Bool);
            AssertParameter(parameters, BotAnimatorParams.DeathVariantName, AnimatorControllerParameterType.Int);
            Assert.That(parameters[BotAnimatorParams.GroundedName].defaultBool, Is.True, "a fresh bot stands on the ground");
            Assert.That(controller.parameters.Length, Is.EqualTo(7));
        }

        [TestCaseSource(nameof(Controllers))]
        public void Controller_EveryStateHasMotion_AndOneDeathPerVariant(string path)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            var states = controller.layers[0].stateMachine.states.Select(child => child.state).ToArray();

            foreach (var state in states)
            {
                AssertNoMissingClips(state.motion, state.name);
            }

            Assert.That(states.Count(state => state.name.StartsWith("Death_")), Is.EqualTo(BotAnimatorParams.DeathVariantCount));
            Assert.That(controller.layers[0].iKPass, Is.True, "WeaponHandIK needs the IK pass");
        }

        [TestCaseSource(nameof(Prefabs))]
        public void Prefab_IsWiredForEcsDrivenMovement(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var animator = prefab.GetComponentInChildren<Animator>();

            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            Assert.That(animator.applyRootMotion, Is.False, "ECS owns the bot's movement");
            Assert.That(animator.avatar.isHuman, Is.True);
            Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty, "visual only — hitboxes are gameplay's");

            var behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.That(behaviours.Length, Is.EqualTo(1), "visual only — nothing but the weapon hold");
            var hold = behaviours[0] as WeaponHold;
            Assert.That(hold, Is.Not.Null);
            Assert.That(hold.gameObject, Is.SameAs(animator.gameObject), "OnAnimatorIK only reaches the Animator's own GameObject");
            Assert.That(hold.Weapon, Is.Not.Null);
            Assert.That(hold.Weapon.GetComponentInChildren<MeshRenderer>(), Is.Not.Null);
            Assert.That(hold.Weapon.IsChildOf(animator.GetBoneTransform(HumanBodyBones.RightHand)), Is.False,
                "the hold poses the weapon itself; under a hand bone the hand would drag it");
            Assert.That(hold.TwoHanded, Is.EqualTo(path.Contains("Rifle")));
        }

        private static void AssertParameter(Dictionary<string, AnimatorControllerParameter> parameters, string name, AnimatorControllerParameterType type)
        {
            Assert.That(parameters.ContainsKey(name), Is.True, name);
            Assert.That(parameters[name].type, Is.EqualTo(type), name);
        }

        private static void AssertNoMissingClips(Motion motion, string where)
        {
            Assert.That(motion, Is.Not.Null, where);

            if (motion is BlendTree tree)
            {
                Assert.That(tree.children, Is.Not.Empty, where);

                foreach (var child in tree.children)
                {
                    AssertNoMissingClips(child.motion, where + "/" + tree.name);
                }
            }
        }
    }
}

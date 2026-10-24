using System.Collections.Generic;
using System.Linq;
using Features.Bots.Providers;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Bots
{
    /// <summary>
    /// Generates the bot visual prefabs (<b>WheelShooter → Bots → Rebuild Prefabs</b>): a Synty
    /// humanoid with the bot controller, root motion off, and the weapon prop held by
    /// <see cref="WeaponHold"/>. No gameplay components; the gameplay view prefab nests it.
    /// </summary>
    /// <remarks>
    /// The grip is solved, not hand-tuned, from every frame of every held-weapon clip of the set,
    /// in the right hand's frame:
    /// <list type="bullet">
    /// <item>the weapon's pistol grip (found on its mesh, just behind and below the trigger) sits in
    /// the right palm;</item>
    /// <item>the grip runs along the knuckle line (index → outermost finger) — that fixes the roll;</item>
    /// <item>the barrel points from the right palm to the left hand's average position (rifle), or
    /// along the back of the right hand (pistol, where the left hand wraps the right);</item>
    /// <item>the wrists' average places on the weapon are what <see cref="WeaponHold"/> lays the
    /// weapon between at runtime; the averaged pose itself is the rigid (dead / one-handed) hold.</item>
    /// </list>
    /// Poses are sampled on a throwaway instance, so no bone overrides end up in the prefab.
    /// </remarks>
    public static class BotPrefabBuilder
    {
        public const string Folder = "Assets/Content/Bots/Prefabs/";

        private const string Character = "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Robot_01.prefab";
        private const string Rifle = "Assets/Synty/PolygonPrototype/Prefabs/Props/SM_Prop_Rifle_01.prefab";
        private const string Pistol = "Assets/Synty/PolygonPrototype/Prefabs/Props/SM_Prop_Pistol_01.prefab";

        private const int FramesPerClip = 8;

        [MenuItem("WheelShooter/Bots/Rebuild Prefabs")]
        public static void BuildAll()
        {
            Build(BotAnimationSet.Rifle, Rifle, true);
            Build(BotAnimationSet.Pistol, Pistol, false);
            AssetDatabase.SaveAssets();
        }

        public static string PathOf(BotAnimationSet set)
        {
            return Folder + set.Name + ".prefab";
        }

        private struct Grip
        {
            public Vector3 SocketPosition;
            public Quaternion SocketRotation;
            public Vector3 LeftHandPosition;
        }

        private static void Build(BotAnimationSet set, string weaponPath, bool supportHandOnBarrel)
        {
            if (!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))
            {
                AssetDatabase.CreateFolder("Assets/Content/Bots", "Prefabs");
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BotAnimatorBuilder.PathOf(set));
            var character = AssetDatabase.LoadAssetAtPath<GameObject>(Character);
            var weapon = AssetDatabase.LoadAssetAtPath<GameObject>(weaponPath);

            var grip = Solve(character, HeldClips(set), PistolGrip(weapon), supportHandOnBarrel);

            var root = new GameObject(set.Name);

            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(character, root.transform);
                model.name = "Model";

                var animator = model.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                // Not under the hand bone: WeaponHold poses it every frame from both hands.
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var pivot = new GameObject("Weapon").transform;
                pivot.SetParent(model.transform, false);
                pivot.SetPositionAndRotation(hand.TransformPoint(grip.SocketPosition), hand.rotation * grip.SocketRotation);

                var prop = (GameObject)PrefabUtility.InstantiatePrefab(weapon, pivot);
                prop.name = weapon.name;

                foreach (var collider in prop.GetComponentsInChildren<Collider>(true))
                {
                    Object.DestroyImmediate(collider);
                }

                var rightWrist = Quaternion.Inverse(grip.SocketRotation) * -grip.SocketPosition;
                model.AddComponent<WeaponHold>().Configure(
                    pivot, supportHandOnBarrel, grip.SocketPosition, grip.SocketRotation, rightWrist, grip.LeftHandPosition);

                PrefabUtility.SaveAsPrefabAsset(root, PathOf(set));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Every clip of the set in which the bot holds its weapon (deaths drop it).</summary>
        private static IEnumerable<AnimationClip> HeldClips(BotAnimationSet set)
        {
            var names = new List<string> { set.Idle, set.CrouchIdle };
            names.AddRange(set.Locomotion);
            names.AddRange(set.CrouchLocomotion);

            if (set.IdleAim != null)
            {
                names.Add(set.IdleAim);
            }

            if (set.CrouchIdleAim != null)
            {
                names.Add(set.CrouchIdleAim);
            }

            return names.Select(BotAnimatorBuilder.Clip);
        }

        /// <summary>
        /// The middle of the weapon's pistol grip, in the weapon's space: the vertices of its main
        /// mesh just behind and below the trigger.
        /// </summary>
        public static Vector3 PistolGrip(GameObject weapon)
        {
            var trigger = weapon.GetComponentsInChildren<Transform>().First(child => child.name.Contains("Trigger"));
            var anchor = weapon.transform.InverseTransformPoint(trigger.position);
            var vertices = weapon.GetComponent<MeshFilter>().sharedMesh.vertices
                .Where(vertex => vertex.z < anchor.z && vertex.z > anchor.z - 0.1f && vertex.y < anchor.y - 0.02f)
                .ToArray();

            var min = vertices.Aggregate(Vector3.Min);
            var max = vertices.Aggregate(Vector3.Max);
            var centre = (min + max) * 0.5f;

            // The palm closes around the upper half of the grip, under the trigger guard.
            return new Vector3(0f, Mathf.Lerp(min.y, max.y, 0.65f), centre.z);
        }

        private static Grip Solve(GameObject character, IEnumerable<AnimationClip> clips, Vector3 gripInWeapon, bool supportHandOnBarrel)
        {
            var probe = (GameObject)PrefabUtility.InstantiatePrefab(character);

            try
            {
                probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                var animator = probe.GetComponent<Animator>();
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var index = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
                // The outermost finger the rig has: Synty hands are index + one merged finger.
                var little = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal)
                    ?? animator.GetBoneTransform(HumanBodyBones.RightRingProximal)
                    ?? animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
                var support = animator.GetBoneTransform(HumanBodyBones.LeftHand);

                // Everything in the right hand's frame, averaged over the samples.
                var palm = Vector3.zero;
                var gripAxis = Vector3.zero;
                var backOfHand = Vector3.zero;
                var supportPosition = Vector3.zero;
                var count = 0;

                foreach (var clip in clips)
                {
                    for (var i = 0; i < FramesPerClip; i++)
                    {
                        clip.SampleAnimation(probe, clip.length * i / FramesPerClip);

                        var knuckles = (index.position + little.position) * 0.5f;
                        palm += hand.InverseTransformPoint((hand.position + knuckles) * 0.5f);
                        gripAxis += hand.InverseTransformDirection(little.position - index.position).normalized;
                        backOfHand += hand.InverseTransformDirection(knuckles - hand.position).normalized;
                        supportPosition += hand.InverseTransformPoint(support.position);
                        count++;
                    }
                }

                palm /= count;
                supportPosition /= count;

                var down = gripAxis.normalized;
                var barrel = supportHandOnBarrel ? supportPosition - palm : backOfHand;
                barrel = Vector3.ProjectOnPlane(barrel, down).normalized;
                down = Vector3.ProjectOnPlane(down, barrel).normalized;

                var socketRotation = Quaternion.LookRotation(barrel, -down);
                var socketPosition = palm - socketRotation * gripInWeapon;
                var toSocket = Quaternion.Inverse(socketRotation);

                return new Grip
                {
                    SocketPosition = socketPosition,
                    SocketRotation = socketRotation,
                    LeftHandPosition = toSocket * (supportPosition - socketPosition),
                };
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
        }
    }
}

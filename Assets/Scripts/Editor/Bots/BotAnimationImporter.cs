using UnityEditor;

namespace Game.Editor.Bots
{
    /// <summary>
    /// Import rules for the Mixamo clips under <see cref="Root"/>: Humanoid, one clip per file named
    /// after the file, locomotion looped, and root motion split so the body never drifts away from
    /// the transform the simulation moves.
    /// </summary>
    /// <remarks>
    /// Rotation and height are baked into the pose (the bot faces where ECS points it; a jump lifts
    /// the body visibly). Horizontal travel stays root motion, which the bot Animator never applies —
    /// so walk cycles play in place and the movement speed is ECS's alone. Deaths are the exception:
    /// their stumble is baked too, so the body falls where the animation puts it.
    /// <para>
    /// Mixamo "without skin" downloads carry only the skeleton, so each file builds its own avatar
    /// from that skeleton; Humanoid retargeting maps it onto any character rig.
    /// </para>
    /// </remarks>
    public sealed class BotAnimationImporter : AssetPostprocessor
    {
        public const string Root = "Assets/Content/Bots/Animations/";

        private void OnPreprocessModel()
        {
            if (!IsBotAnimation(assetPath))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importAnimation = true;
            importer.resampleCurves = true;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
        }

        private void OnPreprocessAnimation()
        {
            if (!IsBotAnimation(assetPath))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            var clips = importer.defaultClipAnimations;

            for (var i = 0; i < clips.Length; i++)
            {
                Configure(clips[i], name);
            }

            importer.clipAnimations = clips;
        }

        public static bool IsBotAnimation(string path)
        {
            return path.StartsWith(Root) && path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Clips that play once and hold their last frame.</summary>
        public static bool IsOneShot(string clipName)
        {
            return clipName.Contains("Death")
                || clipName.Contains("Turn90")
                || (clipName.Contains("Jump") && !clipName.Contains("JumpLoop"))
                || (clipName.Contains("Kneel") && !clipName.Contains("KneelingIdle"));
        }

        private static void Configure(ModelImporterClipAnimation clip, string name)
        {
            var isDeath = name.Contains("Death");
            var loops = !IsOneShot(name);

            clip.name = name;
            clip.loopTime = loops;
            clip.loopPose = loops;

            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = true;

            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = isDeath;
            clip.heightFromFeet = !isDeath;

            clip.lockRootPositionXZ = isDeath;
            clip.keepOriginalPositionXZ = true;
        }
    }
}

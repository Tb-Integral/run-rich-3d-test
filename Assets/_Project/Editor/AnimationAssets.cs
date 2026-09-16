using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RunRich.Editor
{
    public static class AnimationAssets
    {
        public const string PlayerPath = "Assets/Visual/Mesh/LowPoly/player.fbx";
        public const string PhotographerPath = "Assets/Visual/Mesh/LowPoly/photographer.fbx";
        public static readonly string[] Names = { "Look Around", "Sad Walk", "Walking", "Happy Walk", "Hip Hop Dancing", "Stomping", "Arm Stretching" };

        public static void Prepare()
        {
            Configure(PlayerPath, false);
            Configure(PhotographerPath, false);
            foreach (string name in Names)
                Configure("Assets/Animations/" + name + ".fbx", true, name != "Stomping");
        }

        public static AnimationClip Clip(string name) => AssetDatabase.LoadAllAssetsAtPath("Assets/Animations/" + name + ".fbx")
            .OfType<AnimationClip>().First(clip => !clip.name.StartsWith("__preview__"));

        private static void Configure(string path, bool animation, bool loop = false)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            if (animation)
            {
                ApplyReferencePose(importer, path);
                var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    clip.loopTime = loop;
                    clip.loopPose = loop;
                    clip.lockRootRotation = true;
                    clip.lockRootPositionXZ = true;
                    clip.lockRootHeightY = true;
                    clip.keepOriginalOrientation = true;
                    clip.keepOriginalPositionXZ = true;
                    clip.keepOriginalPositionY = false;
                    clip.heightFromFeet = true;
                }
                importer.clipAnimations = clips;
            }
            importer.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("Не настроен Humanoid Avatar: " + path);
        }

        private static void ApplyReferencePose(ModelImporter importer, string path)
        {
            if (importer.humanDescription.skeleton.Length == 0)
                importer.SaveAndReimport();
            string referencePath = Path.GetFileNameWithoutExtension(path) == "Arm Stretching"
                ? PhotographerPath : PlayerPath;
            var reference = ((ModelImporter)AssetImporter.GetAtPath(referencePath)).humanDescription;
            var description = importer.humanDescription;
            var restBones = reference.skeleton.ToDictionary(bone => bone.name);
            var skeleton = description.skeleton;
            // В этих FBX тот же скелет, но отсутствует родитель Armature.
            // Сохранена их иерархия, а опорная поза костей взята из модели:
            // первая согнутая поза клипа не должна становиться его T-позой.
            for (int i = 0; i < skeleton.Length; i++)
            {
                if (!restBones.TryGetValue(skeleton[i].name, out var rest)) continue;
                skeleton[i].position = rest.position;
                skeleton[i].rotation = rest.rotation;
                skeleton[i].scale = rest.scale;
            }
            description.skeleton = skeleton;
            description.human = reference.human;
            importer.sourceAvatar = null;
            importer.humanDescription = description;
        }

        public static void RepairReferencePoses()
        {
            Prepare();
            CharacterAuthoring.RefreshUpgradePose();
            AssetDatabase.SaveAssets();
            ProjectValidation.Run();
        }

        public static void PrepareAndReport()
        {
            Prepare();
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/animation-import.txt", Names.Select(name =>
            {
                var clip = Clip(name);
                return $"{name}: {clip.length:F3}s; human={clip.humanMotion}; " +
                    string.Join(", ", AnimationUtility.GetCurveBindings(clip).Select(binding => binding.propertyName));
            }));
        }
    }
}

using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RunRich.Editor
{
    public static class CharacterAuthoring
    {
        private const string Folder = "Assets/_Project/Animations/";

        public static GameObject CreatePlayer(Material material)
        {
            AnimationAssets.Prepare();
            var controller = Controller("Runner");
            controller.AddParameter("Happiness", AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            machine.defaultState = State(machine, "Idle", AnimationAssets.Clip("Look Around"));
            var blend = new BlendTree { name = "Happiness", blendParameter = "Happiness", blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(blend, controller);
            blend.AddChild(AnimationAssets.Clip("Sad Walk"), 0);
            blend.AddChild(AnimationAssets.Clip("Walking"), 0.5f);
            blend.AddChild(AnimationAssets.Clip("Happy Walk"), 1);
            State(machine, "Locomotion", blend);
            State(machine, "Victory", AnimationAssets.Clip("Hip Hop Dancing"));
            State(machine, "Defeat", AnimationAssets.Clip("Stomping"));
            State(machine, "Upgrade", CreateUpgradeClip());
            var photographer = Controller("Photographer");
            var photographerMachine = photographer.layers[0].stateMachine;
            photographerMachine.defaultState = State(photographerMachine, "Idle", AnimationAssets.Clip("Arm Stretching"));

            var runner = new GameObject("Runner");
            var pivot = new GameObject("VisualPivot").transform;
            pivot.SetParent(runner.transform, false);
            // Сохранена связь с model prefab: меши и скелет принадлежат исходному FBX.
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AnimationAssets.PlayerPath), pivot);
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMesh.subMeshCount).ToArray();
                renderer.gameObject.SetActive(renderer.name == "casual");
                renderer.updateWhenOffscreen = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.gameObject);
            }
            var animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            var view = runner.AddComponent<PlayerPresentation>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.FindProperty("visualPivot").objectReferenceValue = pivot;
            string[] outfitNames = { "poor", "casual", "middle", "bling", "cocktail" };
            var outfits = serialized.FindProperty("outfits");
            outfits.arraySize = outfitNames.Length;
            var transforms = model.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < outfitNames.Length; i++)
                outfits.GetArrayElementAtIndex(i).objectReferenceValue = transforms.Single(t => t.name == outfitNames[i]).gameObject;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AnimationAssets.Clip("Look Around").SampleAnimation(model, 0);
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
                PrefabUtility.RecordPrefabInstancePropertyModifications(bone);
            AssetDatabase.SaveAssets();
            return runner;
        }

        private static AnimatorController Controller(string name)
        {
            string path = Folder + name + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach (var state in controller.layers[0].stateMachine.states)
                controller.layers[0].stateMachine.RemoveState(state.state);
            foreach (var tree in AssetDatabase.LoadAllAssetsAtPath(path).OfType<BlendTree>())
                Object.DestroyImmediate(tree, true);
            controller.parameters = new AnimatorControllerParameter[0];
            return controller;
        }

        private static AnimatorState State(AnimatorStateMachine machine, string name, Motion clip)
        {
            var state = machine.AddState(name);
            state.motion = clip;
            state.writeDefaultValues = true;
            return state;
        }

        public static void RefreshUpgradePose() => CreateUpgradeClip();

        private static AnimationClip CreateUpgradeClip()
        {
            var source = AnimationAssets.Clip("Walking");
            var clip = new AnimationClip { name = "Upgrade Pose", frameRate = 60 };
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                // IK-цели исходной ходьбы не фиксируются поверх созданной позы.
                if (binding.propertyName.Contains("FootT.") || binding.propertyName.Contains("FootQ.") ||
                    binding.propertyName.Contains("HandT.") || binding.propertyName.Contains("HandQ.")) continue;
                float value = AnimationUtility.GetEditorCurve(source, binding).Evaluate(0);
                float pose = binding.propertyName switch
                {
                    "Left Arm Down-Up" or "Right Arm Down-Up" => 0.3f,
                    "Left Arm Front-Back" or "Right Arm Front-Back" => 0,
                    "Left Forearm Stretch" or "Right Forearm Stretch" => 0.85f,
                    "Right Upper Leg Front-Back" => -0.25f,
                    "Right Lower Leg Stretch" => -0.75f,
                    "Left Upper Leg Front-Back" => 0,
                    "Left Lower Leg Stretch" => 0.85f,
                    "Right Foot Up-Down" => -0.3f,
                    "Left Foot Up-Down" => 0,
                    _ => value
                };
                if (binding.propertyName.StartsWith("RootT."))
                    pose = value = binding.propertyName == "RootT.y" ? value : 0;
                if (binding.propertyName.StartsWith("RootQ."))
                    pose = value = binding.propertyName == "RootQ.w" ? 1 : 0;
                var curve = new AnimationCurve(new Keyframe(0, value), new Keyframe(0.12f, pose),
                    new Keyframe(0.52f, pose), new Keyframe(0.65f, value));
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.startTime = 0;
            settings.stopTime = 0.65f;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            string path = Folder + "UpgradePose.anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null) { AssetDatabase.CreateAsset(clip, path); return clip; }
            EditorUtility.CopySerialized(clip, existing);
            Object.DestroyImmediate(clip);
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}

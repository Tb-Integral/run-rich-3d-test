using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RunRich.Editor
{
    public static class ProjectValidation
    {
        [Serializable]
        private sealed class Audit
        {
            public string unityVersion;
            public int missingScripts;
            public int materialsWithoutShader;
            public string[] animationClips;
            public string[] modelDetails;
            public string[] surfaceShaders;
        }

        [MenuItem("Tools/Run Rich/Validate Project")]
        public static void Run()
        {
            var result = new Audit { unityVersion = Application.unityVersion };
            var models = new List<string>();
            var clips = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null)
                    continue;
                models.Add($"{path}: transforms={model.GetComponentsInChildren<Transform>(true).Length}, skinnedMeshes={model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length}");
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                    if (!clip.name.StartsWith("__preview__"))
                        clips.Add($"{path}: {clip.name}, {clip.length:F3}s");
            }
            result.modelDetails = models.ToArray();
            result.animationClips = clips.ToArray();
            result.surfaceShaders = Directory.GetFiles("Assets", "*.shader", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("#pragma surface")).ToArray();

            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material != null && (material.shader == null || material.shader.name == "Hidden/InternalErrorShader"))
                    result.materialsWithoutShader++;
            }

            foreach (var sceneEntry in EditorBuildSettings.scenes.Where(scene => scene.enabled))
            {
                var scene = EditorSceneManager.OpenPreviewScene(sceneEntry.path);
                try
                {
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var child in root.GetComponentsInChildren<Transform>(true))
                            result.missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/project-validation.json", JsonUtility.ToJson(result, true));
            Debug.Log("Проверка проекта сохранена в Logs/project-validation.json");
            if (result.missingScripts > 0)
                throw new InvalidOperationException("В сценах найдены отсутствующие компоненты.");
        }

        public static void ValidateAndBuild()
        {
            Run();
            Directory.CreateDirectory("Builds/Validation");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = "Builds/Validation/RunRichValidation.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            File.WriteAllText("Logs/build-result.txt", $"{report.summary.result}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}");
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Проверочная сборка завершилась с ошибкой.");
        }
    }
}

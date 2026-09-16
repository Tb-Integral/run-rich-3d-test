using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace RunRich.Editor
{
    public static class VisualFoundationBuilder
    {
        private const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/Gameplay.unity";
        private static double _captureAt;

        [MenuItem("Tools/Run Rich/Rebuild Visual Foundation")]
        private static void RebuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog("РџРµСЂРµСЃРѕР·РґР°С‚СЊ РІРёР·СѓР°Р»СЊРЅСѓСЋ РѕСЃРЅРѕРІСѓ?", "Gameplay, РїСЂРµС„Р°Р± РїРµСЂСЃРѕРЅР°Р¶Р° Рё СЃРѕР·РґР°РЅРЅС‹Рµ РєР»РёРїС‹ Р±СѓРґСѓС‚ Р·Р°РјРµРЅРµРЅС‹ РёСЃС…РѕРґРЅРѕР№ РІРµСЂСЃРёРµР№ СЌС‚Р°РїР° 1.", "РџРµСЂРµСЃРѕР·РґР°С‚СЊ", "РћС‚РјРµРЅР°"))
                return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                Build();
        }

        public static void BuildAndCapture()
        {
            try
            {
                Build();
                _captureAt = EditorApplication.timeSinceStartup + 4;
                EditorApplication.update += CaptureWhenReady;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void CaptureWhenReady()
        {
            if (EditorApplication.timeSinceStartup < _captureAt || EditorApplication.isCompiling)
                return;
            EditorApplication.update -= CaptureWhenReady;
            try
            {
                FoundationCapture.Run(() =>
                {
                    ProjectValidation.Run();
                    EditorApplication.Exit(0);
                }, exception =>
                {
                    Debug.LogException(exception);
                    EditorApplication.Exit(1);
                });
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void Build()
        {
            foreach (string folder in new[] { "Scenes", "Prefabs", "Materials", "Animations", "Settings" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();

            Material avatar = Material("Avatar", "RunRich/Reference Lit", Color.white, "atlas.png");
            avatar.SetColor("_ShadeColor", new Color(0.60f, 0.53f, 0.48f));
            avatar.SetFloat("_ShadeStrength", 0.26f);
            avatar.SetFloat("_Highlight", 0.12f);
            avatar.SetColor("_BaseColor", new Color(1.08f, 1.08f, 1.08f));
            Material track = Material("Track", "RunRich/Reference Lit", new Color(0.91f, 0.95f, 1));
            track.SetFloat("_ShadeStrength", 0.12f);
            EditorUtility.SetDirty(avatar);
            EditorUtility.SetDirty(track);
            Material water = Material("Ocean", "RunRich/Reference Water", Color.white, "Water.png");
            Material sky = Material("Sky", "RunRich/Reference Sky", Color.white);
            ConfigurePipeline();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;
            RenderSettings.fog = false;

            var environment = new GameObject("Environment");
            Cube("First straight", new Vector3(0, -0.12f, 22), new Vector3(4.2f, 0.24f, 64), track, environment.transform);
            var ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ocean.name = "Ocean";
            ocean.transform.SetParent(environment.transform);
            ocean.transform.position = new Vector3(0, -0.32f, 60);
            ocean.transform.localScale = new Vector3(100, 1, 100);
            Object.DestroyImmediate(ocean.GetComponent<Collider>());
            var oceanRenderer = ocean.GetComponent<MeshRenderer>();
            oceanRenderer.sharedMaterial = water;
            oceanRenderer.shadowCastingMode = ShadowCastingMode.Off;
            oceanRenderer.receiveShadows = false;

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(48, 45, 0);
            sun.color = Color.white;
            sun.intensity = 1;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 1;
            sun.shadowBias = 0.01f;
            sun.shadowNormalBias = 0.08f;
            RenderSettings.sun = sun;

            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0, 1.72f, -5.8f), Quaternion.Euler(9.4f, 0, 0));
            camera.fieldOfView = 40;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 700;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.gameObject.AddComponent<AudioListener>();
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

            CharacterAuthoring.CreatePlayer(avatar);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.defaultScreenWidth = 440;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            AssetDatabase.SaveAssets();
        }

        private static void ConfigurePipeline()
        {
            var source = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            var pipeline = Save(Object.Instantiate(source), Root + "/Settings/RunRichPipeline.asset");
            pipeline.name = "RunRichPipeline";
            pipeline.msaaSampleCount = 4;
            pipeline.supportsHDR = false;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.shadowDistance = 28;
            pipeline.shadowCascadeCount = 4;
            pipeline.cascade4Split = new Vector3(0.3f, 0.5f, 0.75f);
            pipeline.mainLightShadowmapResolution = 4096;
            var shadowSettings = new SerializedObject(pipeline);
            shadowSettings.FindProperty("m_SoftShadowsSupported").boolValue = true;
            shadowSettings.FindProperty("m_SoftShadowQuality").intValue = 3;
            shadowSettings.ApplyModifiedPropertiesWithoutUndo();
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
            EditorUtility.SetDirty(pipeline);
        }

        private static Material Material(string name, string shaderName, Color color, string textureName = null)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("РќРµ РЅР°Р№РґРµРЅ С€РµР№РґРµСЂ " + shaderName);
            var material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (textureName != null)
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Visual/Texture2D/" + textureName));
            return Save(material, Root + "/Materials/" + name + ".mat");
        }

        private static T Save<T>(T asset, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null) { AssetDatabase.CreateAsset(asset, path); return asset; }
            EditorUtility.CopySerialized(asset, existing);
            Object.DestroyImmediate(asset);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}

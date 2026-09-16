using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace RunRich.Editor
{
    public static class RunStageCapture
    {
        public static void RebuildAndCapture()
        {
            RunStageBuilder.Build();
            BuildAndCapture();
        }

        public static void BuildAndCapture()
        {
            ProjectValidation.ValidateAndBuild();
            Run();
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene(VisualFoundationBuilder.ScenePath);
            Object.FindFirstObjectByType<RunnerCamera>().Snap();
            AnimationAssets.Clip("Look Around").SampleAnimation(GameObject.Find("Runner").GetComponentInChildren<Animator>().gameObject, 0);
            var camera = Camera.main;
            var target = new RenderTexture(880, 1920, 24);
            target.Create();
            camera.targetTexture = target;
            camera.aspect = 880f / 1920;
            var canvas = Object.FindFirstObjectByType<Canvas>();
            canvas.GetComponent<CanvasScaler>().enabled = false;
            canvas.scaleFactor = 1;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            double at = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += Capture;
            void Capture()
            {
                if (EditorApplication.timeSinceStartup < at) return;
                EditorApplication.update -= Capture;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var image = new Texture2D(880, 1920, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 880, 1920), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Logs/Stage3");
                File.WriteAllBytes("Logs/Stage3/ready.png", image.EncodeToPNG());
                RenderTexture.active = previous;
                camera.targetTexture = null;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
                EditorApplication.Exit(0);
            }
        }
    }
}

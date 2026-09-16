using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RunRich.Editor
{
    public static class MovementCapture
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene(VisualFoundationBuilder.ScenePath);
            var path = UnityEngine.Object.FindFirstObjectByType<TrackPath>();
            var runner = GameObject.Find("Runner");
            var model = runner.GetComponentInChildren<Animator>().gameObject;
            var camera = Camera.main;
            float[] distances = { 0, 26.8f, 35, 55.3f, 65 };
            Directory.CreateDirectory("Logs/Stage2");
            int index = 0;
            bool posed = false;
            double renderAt = 0;
            EditorApplication.update += Tick;
            void Tick()
            {
                try
                {
                    if (!posed)
                    {
                        Pose pose = path.Evaluate(distances[index]);
                        runner.transform.SetPositionAndRotation(pose.position, pose.rotation);
                        AnimationAssets.Clip("Walking").SampleAnimation(model, 0.3f);
                        Quaternion heading = path.Evaluate(Mathf.Max(0, distances[index] - 0.55f)).rotation;
                        camera.transform.SetPositionAndRotation(pose.position + heading * new Vector3(0, 1.72f, -5.8f), heading * Quaternion.Euler(9.4f, 0, 0));
                        camera.aspect = 880f / 1920;
                        posed = true;
                        renderAt = EditorApplication.timeSinceStartup + 0.3;
                        EditorApplication.QueuePlayerLoopUpdate();
                        return;
                    }
                    if (EditorApplication.timeSinceStartup < renderAt) return;
                    var target = new RenderTexture(880, 1920, 24);
                    target.Create();
                    var previous = RenderTexture.active;
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target;
                    var image = new Texture2D(880, 1920, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, 880, 1920), 0, 0);
                    image.Apply();
                    File.WriteAllBytes($"Logs/Stage2/frame-{index}.png", image.EncodeToPNG());
                    RenderTexture.active = previous;
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                    UnityEngine.Object.DestroyImmediate(image);
                    posed = false;
                    if (++index < distances.Length) return;
                    EditorApplication.update -= Tick;
                    EditorApplication.Exit(0);
                }
                catch (Exception exception)
                {
                    EditorApplication.update -= Tick;
                    Debug.LogException(exception);
                    EditorApplication.Exit(1);
                }
            }
        }
    }
}

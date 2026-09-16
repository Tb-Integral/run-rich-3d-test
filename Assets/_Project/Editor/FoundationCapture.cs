using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RunRich.Editor
{
    public static class FoundationCapture
    {
        public static void Run(Action complete, Action<Exception> failed)
        {
            EditorSceneManager.OpenScene(VisualFoundationBuilder.ScenePath);
            var camera = Camera.main;
            var model = GameObject.Find("Runner").GetComponentInChildren<Animator>().gameObject;
            var pivot = model.transform.parent;
            Directory.CreateDirectory("Logs/Stage1");
            var target = new RenderTexture(880, 1920, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var previous = RenderTexture.active;
            string[] names = { "idle", "sad", "walking", "happy", "upgrade", "defeat", "victory" };
            string[] clips = { "Look Around", "Sad Walk", "Walking", "Happy Walk", "UpgradePose", "Stomping", "Hip Hop Dancing" };
            int index = 0;
            bool posed = false;
            double renderAt = 0;
            EditorApplication.update += Tick;

            void Cleanup()
            {
                EditorApplication.update -= Tick;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            void Tick()
            {
                try
                {
                    if (!posed)
                    {
                        Pose(clips[index], index == 4 ? 0.32f : 0.3f);
                        pivot.localRotation = Quaternion.Euler(0, index == 4 ? 180 : 0, 0);
                        posed = true;
                        renderAt = EditorApplication.timeSinceStartup + 0.2;
                        EditorApplication.QueuePlayerLoopUpdate();
                        return;
                    }
                    if (EditorApplication.timeSinceStartup < renderAt) return;
                    Capture(names[index]);
                    posed = false;
                    if (++index < names.Length) return;
                    Cleanup();
                    complete();
                }
                catch (Exception exception)
                {
                    Cleanup();
                    failed(exception);
                }
            }
            void Pose(string clipName, float time)
            {
                var clip = clipName == "UpgradePose"
                    ? AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/UpgradePose.anim")
                    : AnimationAssets.Clip(clipName);
                clip.SampleAnimation(model, time);
            }
            void Capture(string name)
            {
                camera.aspect = 880f / 1920;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                var texture = new Texture2D(880, 1920, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 880, 1920), 0, 0);
                texture.Apply();
                File.WriteAllBytes("Logs/Stage1/" + name + ".png", texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace RunRich.Editor
{
    [InitializeOnLoad]
    public static class WealthStageCapture
    {
        private const string Flag = "RunRich.WealthCapture";
        private static double _next;
        private static int _step;
        private static RenderTexture _target;
        private static Camera _camera;
        private static RunSession _session;
        private static RunnerMotor _motor;
        private static readonly string[] AuditOutfits = { "bling", "cocktail", "cocktail_EndLevel", "bling_EndLevel" };
        private static int _auditIndex;
        static WealthStageCapture() => EditorApplication.playModeStateChanged += OnPlayMode;

        public static void Run()
        {
            EditorSceneManager.OpenScene(VisualFoundationBuilder.ScenePath);
            SessionState.SetBool(Flag, true);
            EditorApplication.EnterPlaymode();
        }
        public static void RebuildAndRun()
        {
            WealthStageBuilder.Build();
            Run();
        }
        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Flag, false)) return;
            SessionState.SetBool(Flag, false);
            _step = 0; _next = EditorApplication.timeSinceStartup + 1;
            EditorApplication.update += Tick;
        }
        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            try
            {
                switch (_step++)
                {
                    case 0:
                        _session = Object.FindFirstObjectByType<RunSession>();
                        _motor = Object.FindFirstObjectByType<RunnerMotor>();
                        _camera = Camera.main;
                        _target = new RenderTexture(880, 1920, 24); _target.Create();
                        _camera.targetTexture = _target; _camera.aspect = 880f / 1920;
                        var canvas = Object.FindFirstObjectByType<RunHud>().GetComponent<Canvas>();
                        canvas.GetComponent<CanvasScaler>().enabled = false; canvas.scaleFactor = 1;
                        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = _camera; canvas.planeDistance = 1;
                        _next = EditorApplication.timeSinceStartup + 1;
                        break;
                    case 1:
                        Capture("ready");
                        _session.StartRun();
                        _motor.Tick(15 / _motor.Settings.ForwardSpeed, 0);
                        _motor.Stop(); _motor.GetComponent<PlayerPresentation>().SetWalking(true);
                        _next = EditorApplication.timeSinceStartup + 0.15;
                        break;
                    case 2:
                        Capture("upgrade");
                        _next = EditorApplication.timeSinceStartup + 0.8;
                        break;
                    case 3:
                        Capture("decent");
                        _session.TryChangeScore(-60);
                        _next = EditorApplication.timeSinceStartup + 0.15;
                        break;
                    case 4:
                        Capture("loss");
                        _session.TryChangeScore(100);
                        _next = EditorApplication.timeSinceStartup + 0.85;
                        break;
                    case 5:
                        Capture("rich");
                        _motor.GetComponent<PlayerPresentation>().enabled = false;
                        _auditIndex = 0;
                        SetAuditOutfit();
                        _next = EditorApplication.timeSinceStartup + 0.3;
                        break;
                    default:
                        Capture("outfit-" + AuditOutfits[_auditIndex]);
                        if (++_auditIndex < AuditOutfits.Length)
                        {
                            SetAuditOutfit();
                            _next = EditorApplication.timeSinceStartup + 0.3;
                        }
                        else { EditorApplication.update -= Tick; EditorApplication.Exit(0); }
                        break;
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.update -= Tick;
                EditorApplication.Exit(1);
            }
        }
        private static void SetAuditOutfit()
        {
            foreach (var mesh in _motor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                mesh.gameObject.SetActive(mesh.name == AuditOutfits[_auditIndex]);
        }
        private static void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(_camera, new UniversalRenderPipeline.SingleCameraRequest { destination = _target });
            var previous = RenderTexture.active; RenderTexture.active = _target;
            var image = new Texture2D(880, 1920, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 880, 1920), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs/Stage4");
            File.WriteAllBytes("Logs/Stage4/" + name + ".png", image.EncodeToPNG());
            RenderTexture.active = previous; Object.Destroy(image);
        }
    }
}

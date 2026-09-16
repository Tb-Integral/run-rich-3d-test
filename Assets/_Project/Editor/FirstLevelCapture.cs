using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace RunRich.Editor
{
    [InitializeOnLoad]
    public static class FirstLevelCapture
    {
        private const string Flag = "RunRich.FirstLevelCapture";
        private static double _next;
        private static int _step;
        private static RenderTexture _target;
        private static Camera _camera;
        private static RunSession _session;
        private static RunnerMotor _motor;
        static FirstLevelCapture() => EditorApplication.playModeStateChanged += OnPlayMode;
        public static void Run()
        {
            EditorSceneManager.OpenScene(VisualFoundationBuilder.ScenePath);
            SessionState.SetBool(Flag, true); EditorApplication.EnterPlaymode();
        }
        public static void RebuildAndRun() { FirstLevelBuilder.Build(); Run(); }
        public static void ValidateAndRun()
        {
            FirstLevelBuilder.Build();
            ProjectValidation.ValidateAndBuild();
            Run();
        }
        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Flag, false)) return;
            SessionState.SetBool(Flag, false); _step = 0; _next = EditorApplication.timeSinceStartup + 1;
            EditorApplication.update += Tick;
        }
        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 0.5;
            try
            {
                switch (_step++)
                {
                    case 0:
                        _session = Object.FindFirstObjectByType<RunSession>(); _motor = Object.FindFirstObjectByType<RunnerMotor>();
                        _camera = Camera.main; _target = new RenderTexture(880, 1920, 24); _target.Create();
                        _camera.targetTexture = _target; _camera.aspect = 880f / 1920;
                        var canvas = Object.FindFirstObjectByType<RunHud>().GetComponent<Canvas>();
                        canvas.GetComponent<CanvasScaler>().enabled = false; canvas.scaleFactor = 1;
                        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = _camera; canvas.planeDistance = 1;
                        break;
                    case 1:
                        Capture("ready");
                        _motor.GetComponent<PickupCollector>().enabled = false;
                        _session.StartRun(); Move(28, 1.3f);
                        break;
                    case 2:
                        Capture("flags-down");
                        Move(33.3f, 1.3f);
                        Raise(0);
                        break;
                    case 3:
                        Capture("flags-raised"); Move(47.5f, 1.3f);
                        break;
                    case 4:
                        Capture("choice");
                        _session.TryChangeScore(10);
                        _motor.Path.GetComponentInChildren<ChoiceGate>().TryActivate(_session, _motor.GetComponent<PlayerWealth>().Settings, 1.3f);
                        Move(53, 1.3f);
                        _next = EditorApplication.timeSinceStartup + 0.2;
                        break;
                    case 5:
                        Capture("school-upgrade"); Move(66, -1.3f); Raise(1);
                        break;
                    case 6:
                        Capture("bottle-rows"); Move(94.5f, 1.3f); Raise(2);
                        break;
                    case 7:
                        Capture("final-flags"); Move(104, 1.3f);
                        break;
                    default:
                        Capture("finish-approach"); EditorApplication.update -= Tick; EditorApplication.Exit(0);
                        break;
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception); EditorApplication.update -= Tick; EditorApplication.Exit(1);
            }
        }
        private static void Move(float distance, float offset)
        {
            _motor.BeginRun();
            _motor.Tick((distance - _motor.Distance) / _motor.Settings.ForwardSpeed,
                (offset - _motor.LateralOffset) / (_motor.Path.Width * _motor.Settings.DragSensitivity));
            _motor.Stop(); _motor.GetComponent<PlayerPresentation>().SetWalking(true);
            _camera.GetComponent<RunnerCamera>().Snap();
        }
        private static void Raise(int index)
        {
            _motor.Path.GetComponentsInChildren<FlagZone>().OrderBy(zone => zone.Distance).ElementAt(index)
                .TryActivate(_session, _motor.GetComponent<PlayerWealth>().Settings, _motor.LateralOffset);
        }
        private static void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(_camera, new UniversalRenderPipeline.SingleCameraRequest { destination = _target });
            var previous = RenderTexture.active; RenderTexture.active = _target;
            var image = new Texture2D(880, 1920, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 880, 1920), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs/Stage5"); File.WriteAllBytes("Logs/Stage5/" + name + ".png", image.EncodeToPNG());
            RenderTexture.active = previous; Object.Destroy(image);
        }
    }
}

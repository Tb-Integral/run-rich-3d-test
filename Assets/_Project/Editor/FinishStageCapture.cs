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
    public static class FinishStageCapture
    {
        private const string Flag = "RunRich.FinishStageCapture";
        private static double _next;
        private static int _step;
        private static Camera _camera;
        private static RenderTexture _target;
        private static RunnerMotor _motor;
        private static RunSession _session;
        private static float _start;
        private static int _savedLevel;
        private static int _savedAttempt;
        static FinishStageCapture() => EditorApplication.playModeStateChanged += OnPlayMode;
        public static void Run()
        {
            EditorSceneManager.OpenScene(VisualFoundationBuilder.ScenePath);
            SessionState.SetBool(Flag, true); EditorApplication.EnterPlaymode();
        }
        public static void RebuildAndRun() { FinishStageBuilder.Build(); Run(); }
        public static void ValidateAndRun() { FinishStageBuilder.Build(); ProjectValidation.ValidateAndBuild(); Run(); }
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
                        _savedLevel = ButchersGames.LevelManager.CurrentLevel;
                        _savedAttempt = ButchersGames.LevelManager.CurrentAttempt;
                        _session = Object.FindFirstObjectByType<RunSession>(); _motor = Object.FindFirstObjectByType<RunnerMotor>();
                        _start = _motor.Path.GetComponentInChildren<FinishCourse>().StartDistance;
                        _camera = Camera.main; _target = new RenderTexture(880, 1920, 24); _target.Create();
                        _camera.targetTexture = _target; _camera.aspect = 880f / 1920;
                        var canvas = Object.FindFirstObjectByType<RunHud>().GetComponent<Canvas>();
                        canvas.GetComponent<CanvasScaler>().enabled = false; canvas.scaleFactor = 1;
                        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = _camera; canvas.planeDistance = 1;
                        _motor.GetComponent<PickupCollector>().enabled = false;
                        _session.StartRun(); _session.TryChangeScore(56); Move(_start - 3, -1.25f);
                        break;
                    case 1:
                        Capture("checkers-x2"); Open(0); Move(_start + 7, -1.25f); _session.TryChangeScore(10);
                        break;
                    case 2:
                        Capture("x3"); Open(1); Move(_start + 17, -1.25f);
                        break;
                    case 3:
                        Capture("x4"); Open(2); Move(_start + 28, -0.4f); _session.TryChangeScore(4);
                        break;
                    case 4:
                        Capture("x5-photographer"); Move(_start + 30.4f, 0); _session.BeginFinishing();
                        _next = EditorApplication.timeSinceStartup + 1.7;
                        break;
                    case 5:
                        Capture("won"); _session.NextLevel();
                        break;
                    case 6:
                        Capture("next-ready"); _session.StartRun(); _session.TryChangeScore(-41);
                        _next = EditorApplication.timeSinceStartup + 1;
                        break;
                    case 7:
                        Capture("lost"); _session.Restart();
                        break;
                    case 8:
                        _session.StartRun(); _session.TryChangeScore(110);
                        Open(0); Open(1); Open(2); Open(3);
                        _camera.GetComponent<RunnerCamera>().Snap();
                        _next = EditorApplication.timeSinceStartup + 1.7;
                        break;
                    default:
                        Capture("maximum-score-finish"); Finish(0); break;
                }
            }
            catch (System.Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        private static void Finish(int code)
        {
            ButchersGames.LevelManager.CurrentLevel = _savedLevel;
            ButchersGames.LevelManager.CurrentAttempt = _savedAttempt;
            PlayerPrefs.Save(); EditorApplication.update -= Tick; EditorApplication.Exit(code);
        }
        private static void Move(float distance, float offset)
        {
            _motor.BeginRun();
            _motor.Tick((distance - _motor.Distance) / _motor.Settings.ForwardSpeed,
                (offset - _motor.LateralOffset) / (_motor.Path.Width * _motor.Settings.DragSensitivity));
            _motor.Stop(); _motor.GetComponent<PlayerPresentation>().SetWalking(true);
            _camera.GetComponent<RunnerCamera>().Snap();
        }
        private static void Open(int index)
        {
            _motor.Path.GetComponentsInChildren<FinishGate>().OrderBy(g => g.Distance).ElementAt(index)
                .TryActivate(_session, _motor.GetComponent<PlayerWealth>().Settings, _motor.LateralOffset);
        }
        private static void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(_camera, new UniversalRenderPipeline.SingleCameraRequest { destination = _target });
            var previous = RenderTexture.active; RenderTexture.active = _target;
            var image = new Texture2D(880, 1920, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 880, 1920), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs/Stage6"); File.WriteAllBytes("Logs/Stage6/" + name + ".png", image.EncodeToPNG());
            RenderTexture.active = previous; Object.Destroy(image);
        }
    }
}

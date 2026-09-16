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
    public static class PolishStageCapture
    {
        private const string Flag = "RunRich.PolishCapture";
        private static readonly string[] Keys = { "Current Level", "Complete Lvl Count", "Last Level Index", "Current Attempt" };
        private static double _next;
        private static int _step;
        private static Camera _camera;
        private static RenderTexture _target;
        private static RunnerMotor _motor;
        private static RunSession _session;
        static PolishStageCapture() => EditorApplication.playModeStateChanged += OnPlayMode;
        public static void Run()
        {
            foreach (string key in Keys)
            {
                SessionState.SetBool(Flag + key + "Exists", PlayerPrefs.HasKey(key));
                SessionState.SetInt(Flag + key, PlayerPrefs.GetInt(key));
            }
            EditorSceneManager.OpenScene(VisualFoundationBuilder.ScenePath);
            SessionState.SetBool(Flag, true); EditorApplication.EnterPlaymode();
        }
        public static void RebuildAndRun() { PolishStageBuilder.Build(); Run(); }
        public static void ValidateAndRun() { ProjectValidation.ValidateAndBuild(); Run(); }
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
                        _motor.GetComponent<PickupCollector>().enabled = false;
                        break;
                    case 1:
                        Capture("01-ready"); _session.StartRun(); Move(13, 1.5f); _session.TryChangeScore(20);
                        _next = EditorApplication.timeSinceStartup + 0.18;
                        break;
                    case 2:
                        Capture("02-money"); _session.TryChangeScore(-20); _next = EditorApplication.timeSinceStartup + 0.18;
                        break;
                    case 3:
                        Capture("03-loss"); Move(51.5f, 1.3f); _session.TryChangeScore(10);
                        _motor.Path.GetComponentInChildren<ChoiceGate>().TryActivate(
                            _session, _motor.GetComponent<PlayerWealth>().Settings, 1.3f);
                        _next = EditorApplication.timeSinceStartup + 0.18;
                        break;
                    case 4:
                        Capture("04-upgrade"); Move(_motor.Path.GetComponentInChildren<FinishCourse>().StartDistance + 30.4f, 0);
                        _session.TryChangeScore(40); _session.ResolveFinishGate(105, 4, _motor.Distance, 0, true);
                        _session.CompleteWin(); _next = EditorApplication.timeSinceStartup + 0.5;
                        break;
                    case 5:
                        Capture("05-victory-flash"); break;
                    case 6:
                        Capture("06-victory"); _session.NextLevel(); break;
                    case 7:
                        Capture("07-next-ready"); _session.StartRun(); _session.TryChangeScore(-41);
                        _next = EditorApplication.timeSinceStartup + 1;
                        break;
                    default:
                        Capture("08-loss-screen"); Finish(0); break;
                }
            }
            catch (System.Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        private static void Move(float distance, float offset)
        {
            _motor.BeginRun();
            _motor.Tick((distance - _motor.Distance) / _motor.Settings.ForwardSpeed,
                (offset - _motor.LateralOffset) / (_motor.Path.Width * _motor.Settings.DragSensitivity));
            _motor.Stop(); _motor.GetComponent<PlayerPresentation>().SetWalking(true);
            _camera.GetComponent<RunnerCamera>().Snap();
        }
        private static void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(_camera, new UniversalRenderPipeline.SingleCameraRequest { destination = _target });
            var previous = RenderTexture.active; RenderTexture.active = _target;
            var image = new Texture2D(880, 1920, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 880, 1920), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs/Stage7"); File.WriteAllBytes("Logs/Stage7/" + name + ".png", image.EncodeToPNG());
            RenderTexture.active = previous; Object.Destroy(image);
        }
        private static void Finish(int code)
        {
            foreach (string key in Keys)
            {
                if (SessionState.GetBool(Flag + key + "Exists", false)) PlayerPrefs.SetInt(key, SessionState.GetInt(Flag + key, 0));
                else PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save(); EditorApplication.update -= Tick; EditorApplication.Exit(code);
        }
    }
}

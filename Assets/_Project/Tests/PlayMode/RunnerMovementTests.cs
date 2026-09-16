using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RunRich.Tests
{
    public sealed class RunnerMovementTests
    {
        [TestCase(440)]
        [TestCase(880)]
        public void DragIsRelativeAndRegrabbingDoesNotTeleport(int width)
        {
            var drag = new HorizontalDrag();
            Assert.That(drag.Sample(true, 1, width * 0.2f, width), Is.Zero);
            Assert.That(drag.Sample(true, 1, width * 0.4f, width), Is.EqualTo(0.2f).Within(0.00001f));
            Assert.That(drag.Sample(true, 1, width * 0.3f, width), Is.EqualTo(-0.1f).Within(0.00001f));
            Assert.That(drag.Sample(false, 1, width * 0.8f, width), Is.Zero);
            Assert.That(drag.Sample(true, 1, width * 0.9f, width), Is.Zero);
            Assert.That(drag.Sample(true, 2, width * 0.1f, width), Is.Zero, "Смена пальца не должна сдвигать персонажа.");
            Assert.That(drag.Sample(true, 2, width * 0.2f, width * 2), Is.Zero, "Изменение окна начинает измерение заново.");
            drag.Reset();
            Assert.That(drag.Sample(true, 2, width * 0.5f, width), Is.Zero);
        }

        [UnityTest]
        public IEnumerator MouseAndTouchUseTheSameDelta()
        {
            yield return Load();
            var input = Object.FindFirstObjectByType<RunnerDragInput>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var touch = InputSystem.AddDevice<Touchscreen>();
            try
            {
                float width = Screen.width;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(width * 0.2f, 100) }.WithButton(MouseButton.Left));
                InputSystem.Update();
                Assert.That(input.ReadDelta(), Is.Zero);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(width * 0.4f, 100) }.WithButton(MouseButton.Left));
                InputSystem.Update();
                float mouseDelta = input.ReadDelta();
                Assert.That(mouseDelta, Is.EqualTo(0.2f).Within(0.0001f));
                InputSystem.QueueStateEvent(mouse, new MouseState());
                InputSystem.Update();
                Assert.That(input.ReadDelta(), Is.Zero);
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = new Vector2(width * 0.5f, 100) });
                InputSystem.Update();
                Assert.That(input.ReadDelta(), Is.Zero);
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = new Vector2(width * 0.7f, 100) });
                InputSystem.Update();
                Assert.That(input.ReadDelta(), Is.EqualTo(mouseDelta).Within(0.0001f));
                input.enabled = false;
                Assert.That(input.ReadDelta(), Is.Zero);
                input.enabled = true;
                Assert.That(input.ReadDelta(), Is.Zero);
            }
            finally { InputSystem.RemoveDevice(touch); InputSystem.RemoveDevice(mouse); }
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator MotionIsIndependentOfFrameRateAndResolution()
        {
            yield return Load();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            Vector3? expected = null;
            foreach (int fps in new[] { 30, 60, 120 })
            foreach (int width in new[] { 440, 880 })
            {
                motor.ResetToStart(); motor.BeginRun();
                var drag = new HorizontalDrag();
                drag.Sample(true, -1, width * 0.3f, width);
                for (int frame = 1; frame <= fps * 10; frame++)
                {
                    float normalizedX = 0.3f + 0.15f * frame / (fps * 10f);
                    motor.Tick(1f / fps, drag.Sample(true, -1, normalizedX * width, width));
                }
                Assert.That(motor.Distance, Is.EqualTo(motor.Settings.ForwardSpeed * 10).Within(0.002f));
                Assert.That(motor.LateralOffset, Is.EqualTo(motor.Path.Width * motor.Settings.DragSensitivity * 0.15f).Within(0.001f));
                if (expected.HasValue) Assert.That(Vector3.Distance(expected.Value, motor.transform.position), Is.LessThan(0.003f));
                expected = motor.transform.position;
            }
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator EdgesStopAndRestartDoNotAccumulateDrag()
        {
            yield return Load();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            motor.BeginRun();
            motor.Tick(0.1f, 100);
            Assert.That(motor.LateralOffset, Is.EqualTo(motor.LateralLimit));
            motor.Tick(0.1f, -0.01f);
            Assert.That(motor.LateralOffset, Is.LessThan(motor.LateralLimit));
            motor.Tick(0.1f, -100);
            Assert.That(motor.LateralOffset, Is.EqualTo(-motor.LateralLimit));
            motor.Stop();
            Vector3 stopped = motor.transform.position;
            motor.Tick(1, 1);
            Assert.That(motor.transform.position, Is.EqualTo(stopped));
            motor.BeginRun(); motor.Tick(100, 0);
            Assert.That(motor.IsRunning, Is.False);
            Assert.That(motor.Distance, Is.EqualTo(motor.Path.Length));
            stopped = motor.transform.position;
            motor.Tick(1, -1);
            Assert.That(motor.transform.position, Is.EqualTo(stopped));
            motor.ResetToStart();
            Assert.That(motor.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(motor.LateralOffset, Is.Zero);
            motor.BeginRun(); motor.Tick(0.1f, 0);
            Assert.That(motor.Distance, Is.GreaterThan(0));
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator TurnsStayContinuousAndCameraKeepsRunnerInView()
        {
            yield return Load();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            var camera = Camera.main;
            var follow = camera.GetComponent<RunnerCamera>();
            foreach (float side in new[] { -1f, 1f })
            {
                motor.ResetToStart(); motor.BeginRun(); follow.Snap();
                Vector3 previous = motor.transform.position;
                for (int frame = 0; frame < 1100 && motor.IsRunning; frame++)
                {
                    motor.Tick(1f / 60, frame == 0 ? side : 0);
                    follow.Follow(1f / 60);
                    if (frame > 0) Assert.That(Vector3.Distance(previous, motor.transform.position), Is.LessThan(0.16f), "Разрыв на повороте.");
                    previous = motor.transform.position;
                    foreach (float aspect in new[] { 440f / 960, 9f / 16 })
                    {
                        camera.aspect = aspect;
                        Vector3 screen = camera.WorldToViewportPoint(motor.transform.position + Vector3.up);
                        Assert.That(screen.z, Is.GreaterThan(0));
                        Assert.That(screen.x, Is.InRange(0.03f, 0.97f));
                        Assert.That(screen.y, Is.InRange(0.1f, 0.9f));
                    }
                }
            }
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator MouseDragTurnsModelThroughRunningMotor()
        {
            yield return Load();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            var view = motor.GetComponent<PlayerPresentation>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var replay = motor.gameObject.AddComponent<SteeringMouseReplay>();
            replay.Mouse = mouse;
            replay.X = Screen.width * 0.3f;
            replay.Held = true;
            float previousCaptureDeltaTime = Time.captureDeltaTime;
            var previousUpdateMode = InputSystem.settings.updateMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            Time.captureDeltaTime = 1f / 60;
            try
            {
                motor.BeginRun();
                yield return null;
                for (int frame = 0; frame < 45; frame++)
                {
                    replay.X += Screen.width * 0.003f;
                    yield return null;
                }
                Debug.Log($"Steering integration: offset={motor.LateralOffset:F3}, yaw={view.SteeringAngle:F3}, state={view.State}");
                Assert.That(motor.LateralOffset, Is.GreaterThan(0.5f));
                Assert.That(view.SteeringAngle, Is.GreaterThan(20));
                Assert.That(Mathf.DeltaAngle(0, view.transform.Find("VisualPivot").localEulerAngles.y), Is.GreaterThan(20));
                replay.Held = false;
                for (int frame = 0; frame < 40; frame++) yield return null;
                Assert.That(Mathf.Abs(view.SteeringAngle), Is.LessThan(0.5f));
            }
            finally
            {
                replay.enabled = false;
                Object.Destroy(replay);
                InputSystem.settings.updateMode = previousUpdateMode;
                Time.captureDeltaTime = previousCaptureDeltaTime;
                InputSystem.RemoveDevice(mouse);
            }
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator SteeringTurnsOnlyVisualAndComposesWithUpgrade()
        {
            yield return Load();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            motor.enabled = false;
            var view = motor.GetComponent<PlayerPresentation>();
            var pivot = view.transform.Find("VisualPivot");
            var rootPosition = motor.transform.position;
            var rootRotation = motor.transform.rotation;
            view.SetWalking(true);
            yield return SteerFor(view, 20, 0.4f);
            Assert.That(view.SteeringAngle, Is.InRange(20, 25));
            Assert.That(Mathf.DeltaAngle(0, pivot.localEulerAngles.y), Is.EqualTo(view.SteeringAngle).Within(0.5f));
            Assert.That(motor.transform.position, Is.EqualTo(rootPosition));
            Assert.That(motor.transform.rotation, Is.EqualTo(rootRotation));
            yield return SteerFor(view, -20, 0.4f);
            Assert.That(view.SteeringAngle, Is.InRange(-25, -20));
            view.SetOutfit(2);
            yield return SteerFor(view, -20, 0.2f);
            Assert.That(view.IsUpgrading, Is.True);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(view.SteeringAngle, pivot.localEulerAngles.y)), Is.GreaterThan(20));
            view.SetOutfit(1);
            Assert.That(view.IsUpgrading, Is.False);
            Assert.That(Mathf.DeltaAngle(0, pivot.localEulerAngles.y), Is.EqualTo(view.SteeringAngle).Within(0.5f));
            view.SetLateralMotion(0);
            yield return new WaitForSeconds(0.5f);
            Assert.That(Mathf.Abs(view.SteeringAngle), Is.LessThan(0.5f));
            yield return SteerFor(view, 20, 0.3f);
            view.ShowDefeat();
            Assert.That(view.SteeringAngle, Is.Zero);
            Assert.That(Quaternion.Angle(pivot.localRotation, Quaternion.identity), Is.LessThan(0.01f));
            view.ResetPresentation();
            view.SetWalking(true);
            yield return SteerFor(view, -20, 0.3f);
            view.gameObject.SetActive(false);
            view.gameObject.SetActive(true);
            Assert.That(view.SteeringAngle, Is.Zero);
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator ShortMouseGestureReachesSteeringAngleAtEveryFrameRate()
        {
            yield return Load();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            var view = motor.GetComponent<PlayerPresentation>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var replay = motor.gameObject.AddComponent<SteeringMouseReplay>();
            replay.Mouse = mouse;
            var previousUpdateMode = InputSystem.settings.updateMode;
            float previousStep = Time.captureDeltaTime;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            try
            {
                foreach (int fps in new[] { 30, 60, 120 })
                foreach (int direction in new[] { -1, 1 })
                {
                    Time.captureDeltaTime = 1f / fps;
                    motor.ResetToStart();
                    motor.BeginRun();
                    replay.X = Screen.width * 0.5f;
                    replay.Held = true;
                    yield return null;
                    yield return null;
                    replay.X += direction * Screen.width * 0.04f;
                    float peak = 0;
                    for (int frame = 0; frame < Mathf.CeilToInt(fps * 0.18f); frame++)
                    {
                        yield return null;
                        peak = Mathf.Max(peak, direction * view.SteeringAngle);
                    }
                    Debug.Log($"Short gesture: fps={fps}, direction={direction}, peak={peak:F2}");
                    Assert.That(peak, Is.GreaterThan(20), $"Короткий жест при {fps} FPS должен давать заметный поворот.");
                    for (int frame = 0; frame < fps; frame++) yield return null;
                    Assert.That(Mathf.Abs(view.SteeringAngle), Is.LessThan(0.5f), "Без движения мыши модель выпрямляется даже при зажатой кнопке.");
                }
            }
            finally
            {
                replay.enabled = false;
                Object.Destroy(replay);
                InputSystem.settings.updateMode = previousUpdateMode;
                Time.captureDeltaTime = previousStep;
                InputSystem.RemoveDevice(mouse);
            }
            yield return Unload();
        }

        private static IEnumerator SteerFor(PlayerPresentation view, float speed, float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                view.SetLateralMotion(speed);
                yield return null;
            }
        }

        private static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
            yield return null;
            Object.FindFirstObjectByType<RunnerMotor>().ResetToStart();
        }
        private static IEnumerator Unload()
        {
            var empty = SceneManager.CreateScene("After movement validation");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Gameplay");
        }
    }
    // В batch Editor ввод имеет отдельный буфер. Подаём события в игровой Update до мотора.
    [DefaultExecutionOrder(-100)]
    public sealed class SteeringMouseReplay : MonoBehaviour
    {
        public Mouse Mouse;
        public float X;
        public bool Held;

        private void Update()
        {
            var state = new MouseState { position = new Vector2(X, Screen.height * 0.5f) };
            InputSystem.QueueStateEvent(Mouse, Held ? state.WithButton(MouseButton.Left) : state);
            InputSystem.Update();
        }
    }
}

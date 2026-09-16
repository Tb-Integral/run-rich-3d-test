using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RunRich.Tests
{
    public sealed class FirstLevelTests
    {
        [UnityTest]
        public IEnumerator ReferenceRouteIsPassableAtDifferentFrameRates()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            foreach (int fps in new[] { 2, 30, 60, 120 })
            {
                session.StartRun();
                var points = new[]
                {
                    new Vector2(1.3f, 0.1f), new Vector2(1.3f, 15.9f), new Vector2(-1.3f, 16.5f),
                    new Vector2(-1.3f, 20.1f), new Vector2(0, 21), new Vector2(0, 23),
                    new Vector2(1.3f, 25), new Vector2(1.3f, 53), new Vector2(-1.3f, 58),
                    new Vector2(-1.3f, 70), new Vector2(1.3f, 72), new Vector2(1.3f, motor.Path.Length)
                };
                foreach (var point in points)
                {
                    var start = new Vector2(motor.LateralOffset, motor.Distance);
                    int watchdog = 0;
                    while (motor.Distance < point.y - 0.0001f)
                    {
                        Assert.That(motor.IsRunning, Is.True, $"Маршрут прервался на {motor.Distance}, FPS={fps}, score={session.Score}");
                        Assert.That(++watchdog, Is.LessThan(5000));
                        float dt = Mathf.Min(1f / fps, (point.y - motor.Distance) / motor.Settings.ForwardSpeed);
                        float progress = (motor.Distance + motor.Settings.ForwardSpeed * dt - start.y) / (point.y - start.y);
                        float offset = Mathf.Lerp(start.x, point.x, progress);
                        motor.Tick(dt, (offset - motor.LateralOffset) / (motor.Path.Width * motor.Settings.DragSensitivity));
                    }
                }
                Assert.That(session.Score, Is.EqualTo(96), "Контрольный проход: 40 +20 −20 +10 +20 +12 +14.");
                Assert.That(session.State, Is.EqualTo(RunSession.RunState.Finishing));
                Assert.That(motor.Path.GetComponentInChildren<ChoiceGate>().Selected, Is.EqualTo(ChoiceGate.Choice.School));
                Assert.That(motor.Path.GetComponentsInChildren<FlagZone>().All(zone => zone.IsTriggered), Is.True);
                Assert.That(motor.Path.Evaluate(motor.Path.Length).rotation.eulerAngles.y, Is.EqualTo(90).Within(0.01f));
                session.Restart();
                yield return null;
            }
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator GateUsesCrossingPositionAndOnlyOneChoicePerRun()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            DisablePickups(motor);
            var wealth = motor.GetComponent<PlayerWealth>();
            var gate = motor.Path.GetComponentInChildren<ChoiceGate>();
            session.StartRun();
            motor.Tick(50 / motor.Settings.ForwardSpeed, -1 / (motor.Path.Width * motor.Settings.DragSensitivity));
            motor.Tick(5 / motor.Settings.ForwardSpeed, 2 / (motor.Path.Width * motor.Settings.DragSensitivity));
            Assert.That(gate.Selected, Is.EqualTo(ChoiceGate.Choice.Party), "В момент пересечения игрок слева, хотя заканчивает кадр справа.");
            Assert.That(session.Score, Is.EqualTo(20));
            Assert.That(gate.TryActivate(session, wealth.Settings, 1), Is.False);
            Assert.That(session.Score, Is.EqualTo(20));
            session.Restart(); yield return null;
            gate = motor.Path.GetComponentInChildren<ChoiceGate>();
            Assert.That(gate.Selected, Is.EqualTo(ChoiceGate.Choice.None));
            Assert.That(gate.IsTriggered, Is.False);
            session.StartRun();
            Assert.That(gate.TryActivate(session, wealth.Settings, 0), Is.True);
            Assert.That(gate.Selected, Is.EqualTo(ChoiceGate.Choice.School));
            Assert.That(session.Score, Is.EqualTo(60));
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator FlagsRaiseOnceWithoutChangingScoreAndRestartResetsLevel()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            int pickupCount = motor.Path.GetComponentsInChildren<TrackPickup>().Length;
            DisablePickups(motor);
            var zone = motor.Path.GetComponentsInChildren<FlagZone>().OrderBy(item => item.Distance).First();
            var wealth = motor.GetComponent<PlayerWealth>();
            float speed = motor.Settings.ForwardSpeed;
            session.StartRun();
            motor.Tick((zone.Distance + 0.1f) / speed, 0);
            Assert.That(zone.IsTriggered, Is.True);
            Assert.That(zone.TryActivate(session, wealth.Settings, 0), Is.False);
            Assert.That(session.Score, Is.EqualTo(40));
            Assert.That(motor.Settings.ForwardSpeed, Is.EqualTo(speed));
            yield return new WaitForSeconds(0.4f);
            Assert.That(zone.IsRaised, Is.True);
            session.Restart(); yield return null;
            Assert.That(motor.Path.GetComponentsInChildren<FlagZone>().All(item => !item.IsTriggered && !item.IsRaised), Is.True);
            var pickups = motor.Path.GetComponentsInChildren<TrackPickup>();
            Assert.That(pickups.Length, Is.EqualTo(pickupCount));
            Assert.That(pickups.All(item => item.enabled && !item.IsCollected), Is.True);
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator FatalPartyChoicePreventsLaterPickupInTheSameFrame()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            DisablePickups(motor);
            var go = new GameObject("Money after fatal choice"); go.transform.SetParent(motor.Path.transform);
            var money = go.AddComponent<TrackPickup>();
            JsonUtility.FromJsonOverwrite("{\"distance\":52,\"lateralOffset\":-1.3,\"kind\":0}", money);
            motor.GetComponent<PickupCollector>().Bind(motor.Path);
            session.StartRun(); session.TryChangeScore(-30);
            motor.Tick(1f / 60, -1.3f / (motor.Path.Width * motor.Settings.DragSensitivity));
            motor.Tick(53 / motor.Settings.ForwardSpeed, 0);
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Lost));
            Assert.That(session.Score, Is.Zero);
            Assert.That(money.IsCollected, Is.False);
            Assert.That(motor.IsRunning, Is.False);
            yield return Unload();
        }

        private static void DisablePickups(RunnerMotor motor)
        {
            foreach (var pickup in motor.Path.GetComponentsInChildren<TrackPickup>()) pickup.enabled = false;
        }
        private static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
            yield return null;
        }
        private static IEnumerator Unload()
        {
            var empty = SceneManager.CreateScene("After first level validation"); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Gameplay");
        }
    }
}

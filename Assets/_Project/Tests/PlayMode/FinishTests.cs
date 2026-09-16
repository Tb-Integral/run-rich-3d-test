using System.Collections;
using System.Linq;
using ButchersGames;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RunRich.Tests
{
    public sealed class FinishTests
    {
        [UnityTest]
        public IEnumerator GatesRespectScoreBoundariesAndStopLargeSteps()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            foreach (int score in new[] { 0, 19, 20, 64, 65, 104, 105, 139, 140, 150 })
            {
                DisableNonGates(motor);
                var gates = motor.Path.GetComponentsInChildren<FinishGate>().OrderBy(g => g.Distance).ToArray();
                session.StartRun(); session.TryChangeScore(score - session.Score);
                motor.Tick(100, 0);
                int multiplier = 1;
                foreach (var gate in gates)
                {
                    Assert.That(gate.IsOpen, Is.EqualTo(!gate.IsFinal && score >= gate.RequiredScore));
                    if (score >= gate.RequiredScore) multiplier = gate.Multiplier;
                }
                var blocked = gates.First(g => g.IsFinal || score < g.RequiredScore);
                Assert.That(motor.Distance, Is.EqualTo(blocked.Distance).Within(0.001f));
                Assert.That(session.State, Is.EqualTo(RunSession.RunState.Finishing));
                var position = motor.transform.position;
                motor.Tick(100, 1);
                Assert.That(motor.transform.position, Is.EqualTo(position));
                Assert.That(session.TryChangeScore(20), Is.False);
                session.CompleteWin(); session.CompleteWin();
                Assert.That(session.ResultMultiplier, Is.EqualTo(multiplier));
                Assert.That(session.ResultScore, Is.EqualTo(score * multiplier));
                session.Restart(); yield return null;
            }
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator PassedGatesStayVisibleAndFinalGateProtectsMoneyPile()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            DisableNonGates(motor);
            var course = motor.Path.GetComponentInChildren<FinishCourse>();
            var gates = motor.Path.GetComponentsInChildren<FinishGate>().OrderBy(g => g.Distance).ToArray();
            session.StartRun(); session.TryChangeScore(110);
            motor.Tick(100, 0);
            yield return null;
            foreach (var gate in gates)
            {
                var renderers = gate.GetComponentsInChildren<MeshRenderer>();
                Assert.That(renderers.Length, Is.GreaterThan(0));
                Assert.That(renderers.All(r => r.enabled && r.gameObject.activeInHierarchy), Is.True);
            }
            Assert.That(gates.Last().IsOpen, Is.False);
            Assert.That(motor.Distance, Is.EqualTo(gates.Last().Distance).Within(0.001f));
            Assert.That(session.ResultMultiplier, Is.EqualTo(5));
            var pile = course.transform.Find("Money Pile");
            Assert.That(pile, Is.Not.Null);
            Assert.That(pile.GetComponentsInChildren<TrackPickup>().Length, Is.Zero);
            Assert.That(pile.GetComponentsInChildren<MeshRenderer>().Length, Is.GreaterThan(100));
            Assert.That(pile.localPosition.z, Is.GreaterThan(gates.Last().transform.localPosition.z));
            Assert.That(motor.Path.Length - gates.Last().Distance, Is.GreaterThan(10));
            var camera = Camera.main;
            camera.GetComponent<RunnerCamera>().Snap();
            Assert.That(course.transform.InverseTransformPoint(camera.transform.position).z,
                Is.GreaterThan(gates[2].transform.localPosition.z), "Предыдущие ворота остались позади камеры.");
            session.CompleteWin();
            Assert.That(session.ResultScore, Is.EqualTo(750));
            Assert.That(session.TryChangeScore(2), Is.False);
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator ReferenceBonusRouteGives440AtDifferentFrameRates()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            foreach (int fps in new[] { 2, 30, 60, 120 })
            {
                var course = motor.Path.GetComponentInChildren<FinishCourse>();
                foreach (var item in motor.Path.GetComponentsInChildren<TrackInteraction>())
                    if (item.Distance < course.StartDistance) item.gameObject.SetActive(false);
                session.StartRun(); session.TryChangeScore(96 - session.Score);
                motor.Tick(course.StartDistance / motor.Settings.ForwardSpeed, -1.25f / (motor.Path.Width * motor.Settings.DragSensitivity));
                int watchdog = 0;
                while (motor.IsRunning)
                {
                    Assert.That(++watchdog, Is.LessThan(2000));
                    motor.Tick(1f / fps, 0);
                }
                Assert.That(session.Score, Is.EqualTo(110));
                Assert.That(session.ResultMultiplier, Is.EqualTo(4));
                session.CompleteWin();
                Assert.That(session.ResultScore, Is.EqualTo(440));
                Assert.That(motor.GetComponent<PlayerPresentation>().State, Is.EqualTo(PlayerPresentation.MotionState.Victory));
                session.Restart(); yield return null;
            }
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator NextAdvancesOnceResetsAndPersistsAcrossSceneReload()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            var manager = Object.FindFirstObjectByType<LevelManager>();
            int level = session.LevelNumber;
            session.NextLevel();
            Assert.That(session.LevelNumber, Is.EqualTo(level));
            DisableNonGates(motor);
            session.StartRun(); session.TryChangeScore(110);
            motor.Tick(100, 0); session.CompleteWin();
            var camera = Camera.main;
            camera.GetComponent<RunnerCamera>().Snap();
            Assert.That(camera.fieldOfView, Is.EqualTo(motor.Path.GetComponentInChildren<FinishCourse>().CameraFieldOfView));
            yield return null;
            var results = Object.FindFirstObjectByType<RunResultHud>();
            Assert.That(results.IsVisible, Is.True);
            var animator = motor.GetComponentInChildren<Animator>();
            yield return new WaitForSeconds(0.3f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Victory"), Is.True);
            var button = results.GetComponentsInChildren<Button>().Single(b => b.name == "Next Level");
            button.onClick.Invoke(); button.onClick.Invoke(); session.Restart();
            Assert.That(session.LevelNumber, Is.EqualTo(level + 1));
            Assert.That(LevelManager.CurrentAttempt, Is.Zero);
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Ready));
            Assert.That(session.Score, Is.EqualTo(40));
            Assert.That(session.ResultMultiplier, Is.EqualTo(1));
            Assert.That(session.ResultScore, Is.Zero);
            Assert.That(results.IsVisible, Is.False);
            Assert.That(motor.Distance, Is.Zero);
            Assert.That(camera.fieldOfView, Is.EqualTo(40), "После следующего уровня восстановлена основная камера.");
            Assert.That(manager.transform.childCount, Is.EqualTo(1));
            Assert.That(motor.Path.GetComponentsInChildren<FinishGate>().All(g => !g.IsTriggered && !g.IsOpen), Is.True);
            Assert.That(motor.Path.GetComponentsInChildren<TrackPickup>().All(p => !p.IsCollected), Is.True);
            session.StartRun();
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Running));
            Assert.That(LevelManager.CurrentAttempt, Is.EqualTo(1));
            yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single); yield return null;
            session = Object.FindFirstObjectByType<RunSession>();
            Assert.That(session.LevelNumber, Is.EqualTo(level + 1));
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Ready));
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator DefeatShowsRetryAndDoesNotAdvanceProgress()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var results = Object.FindFirstObjectByType<RunResultHud>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            int level = session.LevelNumber;
            session.StartRun(); session.TryChangeScore(-41); session.NextLevel();
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Lost));
            Assert.That(results.IsVisible, Is.False);
            var position = motor.transform.position;
            yield return new WaitForSeconds(0.9f);
            Assert.That(results.IsVisible, Is.True);
            Assert.That(motor.transform.position, Is.EqualTo(position));
            Assert.That(motor.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Defeat"), Is.True);
            var buttons = results.GetComponentsInChildren<Button>();
            Assert.That(buttons.Length, Is.EqualTo(1));
            Assert.That(buttons[0].name, Is.EqualTo("Retry"));
            buttons[0].onClick.Invoke(); buttons[0].onClick.Invoke();
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Ready));
            Assert.That(session.LevelNumber, Is.EqualTo(level));
            Assert.That(session.Score, Is.EqualTo(40));
            Assert.That(results.IsVisible, Is.False);
            yield return Unload();
        }

        private static void DisableNonGates(RunnerMotor motor)
        {
            foreach (var item in motor.Path.GetComponentsInChildren<TrackInteraction>())
                if (item is not FinishGate) item.gameObject.SetActive(false);
        }
        private static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single); yield return null;
        }
        private static IEnumerator Unload()
        {
            var scene = SceneManager.CreateScene("After finish tests"); SceneManager.SetActiveScene(scene);
            yield return SceneManager.UnloadSceneAsync("Gameplay");
        }
    }
}

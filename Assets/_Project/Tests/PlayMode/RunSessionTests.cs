using System.Collections;
using ButchersGames;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RunRich.Tests
{
    public sealed class RunSessionTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator ReadyWaitsAndFirstPressStartsOneAttempt()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            var tutorial = GameObject.Find("Tutorial");
            yield return new WaitForSeconds(0.2f);
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Ready));
            Assert.That(motor.Distance, Is.Zero);
            Assert.That(motor.GetComponent<PlayerPresentation>().State, Is.EqualTo(PlayerPresentation.MotionState.Idle));
            Assert.That(tutorial.activeSelf, Is.True);
            var mouse = InputSystem.AddDevice<Mouse>();
            var replay = motor.gameObject.AddComponent<SteeringMouseReplay>();
            replay.Mouse = mouse;
            replay.X = Screen.width * 0.5f;
            var previousMode = InputSystem.settings.updateMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            int attempts = LevelManager.CurrentAttempt;
            try
            {
                replay.Held = false;
                yield return null;
                var restart = Object.FindFirstObjectByType<RunHud>().GetComponentInChildren<Button>();
                var oldLevel = LevelManager.Default.CurrentLevelInstance;
                replay.X = restart.transform.position.x;
                replay.Y = restart.transform.position.y;
                replay.Held = true;
                for (int i = 0; i < 3; i++) yield return null;
                Assert.That(session.State, Is.EqualTo(RunSession.RunState.Ready), "Нажатие на UI не начинает попытку.");
                replay.Held = false;
                for (int i = 0; i < 3; i++) yield return null;
                var module = Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log($"UI click: focused={UnityEngine.EventSystems.EventSystem.current.isFocused}, active={UnityEngine.EventSystems.EventSystem.current.currentInputModule}, enabled={module.leftClick.action.enabled}, point={module.point.action.ReadValue<Vector2>()}, mouse={mouse.position.ReadValue()}, screen={Screen.width}x{Screen.height}, button={restart.transform.position}");
                Assert.That(LevelManager.Default.CurrentLevelInstance, Is.Not.SameAs(oldLevel), "Кнопка UI должна обрабатывать настоящий клик мыши.");
                replay.X = Screen.width * 0.5f;
                replay.Y = Screen.height * 0.5f;
                replay.Held = true;
                for (int i = 0; i < 5; i++) yield return null;
                Assert.That(session.State, Is.EqualTo(RunSession.RunState.Running));
                Assert.That(motor.Distance, Is.GreaterThan(0));
                Assert.That(tutorial.activeSelf, Is.False);
                session.StartRun();
                Assert.That(LevelManager.CurrentAttempt, Is.EqualTo(attempts + 1));
            }
            finally
            {
                replay.enabled = false;
                Object.Destroy(replay);
                InputSystem.settings.updateMode = previousMode;
                InputSystem.RemoveDevice(mouse);
            }
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator RestartReloadsExactlyOneLevelAndResetsPresentation()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            var view = motor.GetComponent<PlayerPresentation>();
            var manager = Object.FindFirstObjectByType<LevelManager>();
            int number = session.LevelNumber;
            var original = manager.CurrentLevelInstance;
            session.StartRun();
            motor.Tick(2, 0.1f);
            view.SetHappiness(1); view.SetOutfit(3);
            session.Restart();
            var replacement = manager.CurrentLevelInstance;
            session.Restart();
            Assert.That(manager.CurrentLevelInstance, Is.SameAs(replacement));
            Assert.That(replacement, Is.Not.SameAs(original));
            Assert.That(original.gameObject.activeSelf, Is.False);
            yield return null;
            Assert.That(manager.transform.childCount, Is.EqualTo(1));
            Assert.That(session.LevelNumber, Is.EqualTo(number));
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Ready));
            Assert.That(session.Score, Is.EqualTo(40));
            Assert.That(motor.Distance, Is.Zero);
            Assert.That(motor.LateralOffset, Is.Zero);
            Assert.That(view.OutfitIndex, Is.EqualTo(1));
            Assert.That(view.Happiness, Is.EqualTo(0.5f));
            Assert.That(view.IsUpgrading, Is.False);
            Assert.That(view.SteeringAngle, Is.Zero);
            var hud = Object.FindFirstObjectByType<RunHud>();
            var button = hud.GetComponentInChildren<Button>();
            Assert.That(hud.BlocksStart(button.transform.position), Is.True, "Кнопка перезапуска не должна считаться жестом начала.");
            button.onClick.Invoke();
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Ready));
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator TerminalStatesBlockMovementAndCanRestart()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            var view = motor.GetComponent<PlayerPresentation>();
            session.Lose();
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Ready));
            session.StartRun(); session.Lose(); session.StartRun(); session.BeginFinishing();
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Lost));
            var position = motor.transform.position;
            motor.Tick(10, 1);
            Assert.That(motor.transform.position, Is.EqualTo(position));
            Assert.That(view.State, Is.EqualTo(PlayerPresentation.MotionState.Defeat));
            session.Restart();
            yield return null;
            session.StartRun();
            motor.Tick(100, 0);
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Finishing));
            session.Lose();
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Finishing));
            yield return new WaitForSeconds(1);
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Won));
            Assert.That(view.State, Is.EqualTo(PlayerPresentation.MotionState.Victory));
            position = motor.transform.position;
            motor.Tick(10, -1);
            Assert.That(motor.transform.position, Is.EqualTo(position));
            session.Restart();
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Ready));
            Assert.That(view.State, Is.EqualTo(PlayerPresentation.MotionState.Idle));
            yield return Unload();
        }

        private static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
            yield return null;
        }
        private static IEnumerator Unload()
        {
            var empty = SceneManager.CreateScene("After session validation");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Gameplay");
        }
    }
}

using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RunRich.Tests
{
    public sealed class WealthPickupTests
    {
        [UnityTest]
        public IEnumerator SweptCollectionMatchesAtLowAndHighFrameRates()
        {
            yield return Load();
            var session = UnityEngine.Object.FindFirstObjectByType<RunSession>();
            var motor = UnityEngine.Object.FindFirstObjectByType<RunnerMotor>();
            var view = motor.GetComponent<PlayerPresentation>();
            foreach (int fps in new[] { 2, 30, 60, 120 })
            {
                session.StartRun();
                while (motor.Distance < 18)
                    motor.Tick(Mathf.Min(1f / fps, (18 - motor.Distance) / motor.Settings.ForwardSpeed), 0);
                Assert.That(session.Score, Is.EqualTo(74), "Все 17 пачек на центральном ряду должны сработать один раз, FPS=" + fps);
                Assert.That(view.OutfitIndex, Is.EqualTo(2));
                var pickups = motor.Path.GetComponentsInChildren<TrackPickup>(true);
                Assert.That(pickups.Count(p => p.IsCollected), Is.EqualTo(17));
                foreach (var pickup in pickups.Where(p => p.IsCollected))
                    Assert.That(pickup.TryCollect(session, motor.GetComponent<PlayerWealth>().Settings), Is.False);
                motor.Tick((23 - motor.Distance) / motor.Settings.ForwardSpeed, 0);
                Assert.That(session.Score, Is.EqualTo(34), "Две бутылки уменьшают счёт на 40.");
                Assert.That(view.OutfitIndex, Is.EqualTo(1));
                Assert.That(view.IsUpgrading, Is.False, "Ухудшение отменяет вращение.");
                session.Restart();
                SetupPickups(motor);
                yield return null;
            }
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator LeftRowCausesDefeatAndRestartRestoresPickups()
        {
            yield return Load();
            var session = UnityEngine.Object.FindFirstObjectByType<RunSession>();
            var motor = UnityEngine.Object.FindFirstObjectByType<RunnerMotor>();
            var wealth = motor.GetComponent<PlayerWealth>();
            var old = motor.Path.GetComponentsInChildren<TrackPickup>();
            session.StartRun();
            motor.Tick(1f / 60, -1.3f / (motor.Path.Width * motor.Settings.DragSensitivity));
            motor.Tick(3, 0);
            Assert.That(session.Score, Is.Zero);
            Assert.That(wealth.Tier.outfitIndex, Is.Zero);
            Assert.That(old.Count(p => p.IsCollected), Is.EqualTo(3));
            Assert.That(old.Where(p => p.Kind == TrackPickup.PickupKind.Money).All(p => !p.IsCollected), Is.True);
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Lost));
            Assert.That(motor.IsRunning, Is.False);
            var position = motor.transform.position;
            motor.Tick(10, 1);
            yield return new WaitForSeconds(0.3f);
            Assert.That(motor.transform.position, Is.EqualTo(position));
            var view = motor.GetComponent<PlayerPresentation>();
            Assert.That(view.State, Is.EqualTo(PlayerPresentation.MotionState.Defeat));
            Assert.That(view.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).fullPathHash,
                Is.EqualTo(Animator.StringToHash("Base Layer.Defeat")), "После потери счастья проигрывается состояние Stomping.");
            session.Restart();
            Assert.That(wealth.Score, Is.EqualTo(40));
            Assert.That(motor.GetComponent<PlayerPresentation>().IsUpgrading, Is.False);
            SetupPickups(motor);
            var fresh = motor.Path.GetComponentsInChildren<TrackPickup>();
            Assert.That(fresh.Length, Is.EqualTo(old.Length));
            Assert.That(fresh.All(p => !p.IsCollected), Is.True);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<WealthHud>().transform.Find("Pickup Amount").gameObject.activeSelf, Is.False);
            foreach (var particles in motor.GetComponentsInChildren<ParticleSystem>()) Assert.That(particles.particleCount, Is.Zero);
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator ExactZeroAllowsRecoveryButBelowZeroStopsTheRun()
        {
            yield return Load();
            var session = UnityEngine.Object.FindFirstObjectByType<RunSession>();
            var motor = UnityEngine.Object.FindFirstObjectByType<RunnerMotor>();
            session.StartRun();
            Assert.That(session.TryChangeScore(-40), Is.True);
            Assert.That(session.Score, Is.Zero);
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Running), "Поражение наступает строго ниже нуля.");
            Assert.That(session.TryChangeScore(2), Is.True);
            Assert.That(session.Score, Is.EqualTo(2));
            Assert.That(session.TryChangeScore(-3), Is.True);
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Lost));
            Assert.That(session.Score, Is.Zero);
            Assert.That(motor.IsRunning, Is.False);
            Assert.That(session.TryChangeScore(20), Is.False, "Подбор после поражения не возобновляет забег.");
            session.StartRun(); session.BeginFinishing(); session.CompleteWin();
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Lost));
            session.Restart();
            yield return null;
            session.StartRun();
            session.TryChangeScore(-40);
            session.TryChangeScore(-1);
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Lost), "Потеря при уже пустой шкале тоже вызывает поражение.");
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator TierBoundariesClampAndNeverMoveRunner()
        {
            yield return Load();
            var session = UnityEngine.Object.FindFirstObjectByType<RunSession>();
            var motor = UnityEngine.Object.FindFirstObjectByType<RunnerMotor>();
            var wealth = motor.GetComponent<PlayerWealth>();
            var view = motor.GetComponent<PlayerPresentation>();
            session.StartRun();
            var position = motor.transform.position;
            foreach (var threshold in new[] { (20, 1), (65, 2), (105, 3), (140, 4) })
            {
                session.TryChangeScore(threshold.Item1 - 1 - wealth.Score);
                Assert.That(view.OutfitIndex, Is.EqualTo(threshold.Item2 - 1));
                session.TryChangeScore(1);
                Assert.That(view.OutfitIndex, Is.EqualTo(threshold.Item2));
                string[] expectedMeshes = { "poor", "casual", "middle", "bling", "cocktail" };
                Assert.That(motor.GetComponentsInChildren<SkinnedMeshRenderer>().Single().name,
                    Is.EqualTo(expectedMeshes[threshold.Item2]), "Статус должен включать костюм из референса.");
                Assert.That(view.IsUpgrading, Is.True);
                session.TryChangeScore(-1);
                Assert.That(view.OutfitIndex, Is.EqualTo(threshold.Item2 - 1));
                Assert.That(view.IsUpgrading, Is.False);
            }
            session.TryChangeScore(int.MaxValue);
            Assert.That(wealth.Score, Is.EqualTo(150));
            Assert.That(view.Happiness, Is.EqualTo(1));
            session.TryChangeScore(int.MinValue);
            Assert.That(wealth.Score, Is.Zero);
            Assert.That(view.Happiness, Is.Zero);
            Assert.That(view.OutfitIndex, Is.Zero);
            Assert.That(motor.transform.position, Is.EqualTo(position));
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator PickupIsRejectedOutsideRunningAndCannotReenter()
        {
            yield return Load();
            var session = UnityEngine.Object.FindFirstObjectByType<RunSession>();
            var motor = UnityEngine.Object.FindFirstObjectByType<RunnerMotor>();
            var wealth = motor.GetComponent<PlayerWealth>();
            var pickup = motor.Path.GetComponentsInChildren<TrackPickup>().First(p => p.Kind == TrackPickup.PickupKind.Money);
            Assert.That(pickup.TryCollect(session, wealth.Settings), Is.False);
            Assert.That(pickup.IsCollected, Is.False);
            bool reentered = true;
            System.Action onChanged = () => reentered = pickup.TryCollect(session, wealth.Settings);
            session.StartRun();
            wealth.Changed += onChanged;
            Assert.That(pickup.TryCollect(session, wealth.Settings), Is.True);
            wealth.Changed -= onChanged;
            Assert.That(reentered, Is.False);
            Assert.That(wealth.Score, Is.EqualTo(42));
            Assert.That(pickup.TryCollect(session, wealth.Settings), Is.False);
            session.Lose();
            Assert.That(session.TryChangeScore(20), Is.False);
            session.Restart();
            yield return null;
            session.StartRun(); session.BeginFinishing();
            Assert.That(session.TryChangeScore(20), Is.False);
            session.CompleteWin();
            Assert.That(session.TryChangeScore(-20), Is.False);
            Assert.That(wealth.Score, Is.EqualTo(40));
            yield return Unload();
        }

        private static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
            yield return null;
            SetupPickups(UnityEngine.Object.FindFirstObjectByType<RunnerMotor>());
        }
        private static void SetupPickups(RunnerMotor motor)
        {
            // Геометрия проверки подбора независима от текущей расстановки уровня.
            foreach (var interaction in motor.Path.GetComponentsInChildren<TrackInteraction>()) interaction.gameObject.SetActive(false);
            for (int i = 0; i < 17; i++) Add(6 + i * 0.65f, 0, TrackPickup.PickupKind.Money);
            Add(20, 0, TrackPickup.PickupKind.Alcohol); Add(22, 0, TrackPickup.PickupKind.Alcohol);
            for (int i = 0; i < 3; i++) Add(7 + i * 3, -1.3f, TrackPickup.PickupKind.Alcohol);
            motor.GetComponent<PickupCollector>().Bind(motor.Path);
            void Add(float distance, float offset, TrackPickup.PickupKind kind)
            {
                var go = new GameObject("Pickup test fixture"); go.transform.SetParent(motor.Path.transform);
                var pickup = go.AddComponent<TrackPickup>();
                JsonUtility.FromJsonOverwrite(FormattableString.Invariant($"{{\"distance\":{distance},\"lateralOffset\":{offset},\"kind\":{(int)kind}}}"), pickup);
            }
        }
        private static IEnumerator Unload()
        {
            var empty = SceneManager.CreateScene("After wealth validation");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Gameplay");
        }
    }
}

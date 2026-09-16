using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RunRich.Tests
{
    public sealed class PolishTests
    {
        [UnityTest]
        public IEnumerator OnlyOpenedFinishGatesPlayOpeningSound()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var audio = Object.FindFirstObjectByType<RunAudio>();
            var effects = audio.transform.Find("effects").GetComponent<AudioSource>();
            session.StartRun(); session.TryChangeScore(110);
            foreach (var source in audio.GetComponentsInChildren<AudioSource>()) source.Stop();
            Assert.That(session.ResolveFinishGate(20, 2, 114, 0), Is.True);
            yield return null;
            Assert.That(effects.isPlaying, Is.True, "Открытые промежуточные ворота воспроизводят звук.");
            effects.Stop();
            Assert.That(session.ResolveFinishGate(140, 5, 142, 0, true), Is.False);
            yield return null;
            Assert.That(session.ResultMultiplier, Is.EqualTo(5));
            Assert.That(effects.isPlaying, Is.False, "Последние закрытые ворота не воспроизводят звук, даже при получении x5.");
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator UpgradeFlashDoesNotPlayOnDowngradeAndRestartClearsEffects()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var feedback = Object.FindFirstObjectByType<PickupFeedback>();
            var flash = feedback.transform.Find("Upgrade Flash").GetComponent<ParticleSystem>();
            var loss = feedback.transform.Find("Red Loss").GetComponent<ParticleSystem>();
            session.StartRun(); session.TryChangeScore(25);
            Assert.That(flash.particleCount, Is.GreaterThan(0));
            yield return new WaitForSeconds(0.6f);
            Assert.That(flash.particleCount, Is.Zero);
            session.TryChangeScore(-2);
            Assert.That(flash.particleCount, Is.Zero);
            Assert.That(loss.particleCount, Is.GreaterThan(0));
            session.Restart();
            Assert.That(feedback.GetComponentsInChildren<ParticleSystem>().All(p => p.particleCount == 0), Is.True);
            Assert.That(Object.FindFirstObjectByType<RunAudio>().GetComponentsInChildren<AudioSource>().All(a => !a.isPlaying), Is.True);
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator NextCancelsDelayedPhotographAndVictoryFeedback()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            var feedback = Object.FindFirstObjectByType<PickupFeedback>();
            session.StartRun(); session.TryChangeScore(110);
            session.ResolveFinishGate(140, 5, motor.Path.GetComponentInChildren<FinishGate>().Distance, 0, true);
            session.CompleteWin();
            var dollars = feedback.transform.Find("Victory Dollars").GetComponent<ParticleSystem>();
            Assert.That(dollars.particleCount, Is.GreaterThan(0));
            session.NextLevel();
            yield return new WaitForSeconds(0.7f);
            Assert.That(session.State, Is.EqualTo(RunSession.RunState.Ready));
            Assert.That(feedback.GetComponentsInChildren<ParticleSystem>().All(p => p.particleCount == 0), Is.True);
            Assert.That(motor.Path.GetComponentInChildren<FinishCourse>().PhotographFlash.particleCount, Is.Zero);
            var audio = Object.FindFirstObjectByType<RunAudio>();
            Assert.That(audio.Settings.Collect, Is.Not.Null);
            Assert.That(audio.Settings.Victory, Is.Not.Null);
            Assert.That(audio.Settings.Footsteps.All(c => c != null), Is.True);
            Assert.That(audio.GetComponentsInChildren<AudioSource>().All(a => !a.isPlaying), Is.True);
            yield return Unload();
        }

        [UnityTest]
        public IEnumerator SteadyMovementAndCameraDoNotAllocateManagedMemory()
        {
            yield return Load();
            var session = Object.FindFirstObjectByType<RunSession>();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            var camera = Camera.main.GetComponent<RunnerCamera>();
            session.StartRun();
            for (int i = 0; i < 100; i++) { motor.Tick(0.001f, 0); camera.Follow(0.001f); }
            var watch = System.Diagnostics.Stopwatch.StartNew();
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) { motor.Tick(0.001f, 0.00001f); camera.Follow(0.001f); }
            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            watch.Stop();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/stage7-core-performance.txt", $"1000 warmed motor+camera ticks; managed bytes={allocated}; elapsedMs={watch.Elapsed.TotalMilliseconds:F3}. Excludes rendering, UI and Editor overhead.");
            Assert.That(allocated, Is.Zero, "В установившемся движении и слежении камеры не нужны выделения managed-памяти.");
            yield return Unload();
        }
        private static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single); yield return null;
            Object.FindFirstObjectByType<PickupCollector>().enabled = false;
        }
        private static IEnumerator Unload()
        {
            var scene = SceneManager.CreateScene("After polish tests"); SceneManager.SetActiveScene(scene);
            yield return SceneManager.UnloadSceneAsync("Gameplay");
        }
    }
}

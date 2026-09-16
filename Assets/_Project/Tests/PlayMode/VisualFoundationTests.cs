using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RunRich.Tests
{
    public sealed class VisualFoundationTests
    {
        [UnityTest]
        public IEnumerator WalkCyclesAndBlendsDoNotBendKneesBackwards()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
            yield return null;
            Object.FindFirstObjectByType<RunSession>().enabled = false;
            Object.FindFirstObjectByType<RunnerMotor>().ResetToStart();
            Object.FindFirstObjectByType<RunnerMotor>().enabled = false;
            var view = Object.FindFirstObjectByType<PlayerPresentation>();
            var animator = view.GetComponentInChildren<Animator>();
            view.SetWalking(true);
            yield return null;
            for (int blend = 0; blend <= 4; blend++)
            {
                animator.SetFloat("Happiness", blend / 4f);
                for (int frame = 0; frame < 120; frame++)
                {
                    animator.Play("Base Layer.Locomotion", 0, frame / 120f);
                    animator.Update(0);
                    AssertKnee(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
                    AssertKnee(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);
                }
            }
            var empty = SceneManager.CreateScene("After gait validation");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Gameplay");

            void AssertKnee(HumanBodyBones hip, HumanBodyBones knee, HumanBodyBones ankle)
            {
                Vector3 upper = (animator.GetBoneTransform(knee).position - animator.GetBoneTransform(hip).position).normalized;
                Vector3 lower = (animator.GetBoneTransform(ankle).position - animator.GetBoneTransform(knee).position).normalized;
                float bend = Mathf.Atan2(Vector3.Dot(Vector3.Cross(upper, lower), animator.transform.right), Vector3.Dot(upper, lower)) * Mathf.Rad2Deg;
                Assert.That(bend, Is.GreaterThanOrEqualTo(-3f), $"{knee}: колено выгнулось назад при счастье {animator.GetFloat("Happiness"):F2}.");
            }
        }

        [UnityTest]
        public IEnumerator GameplayAnimatesOneOutfitWithoutMovingRunner()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
            yield return null;
            Object.FindFirstObjectByType<RunSession>().enabled = false;
            Object.FindFirstObjectByType<RunnerMotor>().ResetToStart();
            Object.FindFirstObjectByType<RunnerMotor>().enabled = false;
            var runner = GameObject.Find("Runner");
            Assert.That(runner, Is.Not.Null);
            var animator = runner.GetComponentInChildren<Animator>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
            var view = runner.GetComponent<PlayerPresentation>();
            Assert.That(view.State, Is.EqualTo(PlayerPresentation.MotionState.Idle));
            view.SetWalking(true);
            yield return new WaitForSeconds(0.2f);
            var renderers = runner.GetComponentsInChildren<SkinnedMeshRenderer>();
            Assert.That(renderers.Length, Is.EqualTo(1), "Костюмы не должны накладываться друг на друга.");
            Assert.That(renderers[0].name, Is.EqualTo("casual"));
            Assert.That(renderers[0].sharedMaterial.GetTexture("_BaseMap"), Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
            var leg = runner.GetComponentsInChildren<Transform>().Single(t => t.name == "mixamorig:LeftUpLeg");
            for (int i = 0; i < 3; i++) yield return null;
            var rotation = leg.localRotation;
            var position = runner.transform.position;
            yield return new WaitForSeconds(0.24f);
            Assert.That(Quaternion.Angle(rotation, leg.localRotation), Is.GreaterThan(3), "Animator должен проигрывать созданный цикл ходьбы.");
            Assert.That(Vector3.Distance(position, runner.transform.position), Is.LessThan(0.001f), "Root motion не должен двигать персонажа при остановленном моторе.");
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.That(material, Is.Not.Null, renderer.name);
                    Assert.That(material.shader.name, Is.Not.EqualTo("Hidden/InternalErrorShader"), renderer.name);
                }
            var empty = SceneManager.CreateScene("After foundation validation");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Gameplay");
        }

        [UnityTest]
        public IEnumerator HappinessBlendsAndDowngradeDoesNotSpin()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
            yield return null;
            Object.FindFirstObjectByType<RunSession>().enabled = false;
            Object.FindFirstObjectByType<RunnerMotor>().ResetToStart();
            Object.FindFirstObjectByType<RunnerMotor>().enabled = false;
            var view = Object.FindFirstObjectByType<PlayerPresentation>();
            var animator = view.GetComponentInChildren<Animator>();
            view.SetWalking(true);
            view.SetHappiness(0);
            yield return new WaitForSeconds(0.6f);
            view.SetHappiness(1);
            yield return null;
            float blend = animator.GetFloat("Happiness");
            Assert.That(blend, Is.GreaterThan(0).And.LessThan(1));
            view.SetHappiness(-10);
            Assert.That(view.Happiness, Is.Zero);
            float phase = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            view.SetOutfit(0);
            yield return null;
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThanOrEqualTo(phase), "Ухудшение одежды не перезапускает шаг.");
            Assert.That(view.SetOutfit(2), Is.True);
            Assert.That(view.IsUpgrading, Is.True);
            yield return new WaitForSeconds(0.25f);
            Assert.That(view.SetOutfit(1), Is.True);
            Assert.That(view.IsUpgrading, Is.False);
            Assert.That(Quaternion.Angle(view.transform.Find("VisualPivot").localRotation, Quaternion.identity), Is.LessThan(0.01f));
            Assert.That(view.GetComponentsInChildren<SkinnedMeshRenderer>().Length, Is.EqualTo(1));
            view.SetOutfit(2);
            yield return new WaitForSeconds(0.8f);
            Assert.That(view.IsUpgrading, Is.False);
            yield return WaitForLocomotion(animator);
            view.SetOutfit(3);
            yield return new WaitForSeconds(0.2f);
            view.gameObject.SetActive(false);
            view.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            Assert.That(view.IsUpgrading, Is.False);
            yield return WaitForLocomotion(animator);
            view.SetOutfit(4);
            yield return new WaitForSeconds(0.2f);
            view.ShowDefeat();
            yield return new WaitForSeconds(0.2f);
            Assert.That(view.IsUpgrading, Is.False);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Defeat"), Is.True);
            view.SetWalking(true);
            Assert.That(view.State, Is.EqualTo(PlayerPresentation.MotionState.Defeat));
            view.ResetPresentation();
            Assert.That(view.OutfitIndex, Is.EqualTo(1));
            Assert.That(view.State, Is.EqualTo(PlayerPresentation.MotionState.Idle));
            Assert.That(view.transform.position, Is.EqualTo(Vector3.zero));
            var empty = SceneManager.CreateScene("After presentation validation");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Gameplay");
        }
        private static IEnumerator WaitForLocomotion(Animator animator)
        {
            // Поворот длится 0.65 с, затем идёт crossfade: ждём состояние, а не точную границу кадра.
            float deadline = Time.realtimeSinceStartup + 2;
            while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion") && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
        }
    }
}

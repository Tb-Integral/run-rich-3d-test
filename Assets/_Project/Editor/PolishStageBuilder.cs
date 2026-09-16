using System.IO;
using System.Linq;
using ButchersGames;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace RunRich.Editor
{
    public static class PolishStageBuilder
    {
        private const string Root = "Assets/_Project/";
        [MenuItem("Tools/Run Rich/Prepare Polish Stage")]
        private static void FromMenu()
        {
            if (!EditorUtility.DisplayDialog("Подготовить полишинг?", "Звуки, эффекты и вспышка фотографа будут настроены в Gameplay. Маршрут и управление сохранятся.", "Подготовить", "Отмена")) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build();
        }

        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(VisualFoundationBuilder.ScenePath);
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            var session = Object.FindFirstObjectByType<RunSession>();
            var wealth = motor.GetComponent<PlayerWealth>();
            var audio = BuildAudio(session, wealth, motor);
            BuildResultBackdrop();
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var feedback = Get<ButtonAudio>(button.gameObject);
                Bind(feedback, "audioFeedback", audio);
            }

            var effects = motor.GetComponentInChildren<PickupFeedback>();
            var data = new SerializedObject(effects);
            var bills = (ParticleSystem)data.FindProperty("bills").objectReferenceValue;
            var loss = (ParticleSystem)data.FindProperty("loss").objectReferenceValue;
            var billMesh = BillMesh();
            bills.GetComponent<ParticleSystemRenderer>().mesh = billMesh;
            var main = bills.main; main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.2f); main.maxParticles = 64;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.05f);
            data.FindProperty("billCount").intValue = 4;
            data.FindProperty("victoryBillCount").intValue = 12;
            data.FindProperty("victoryDollarCount").intValue = 24;
            data.ApplyModifiedPropertiesWithoutUndo();
            var lossMain = loss.main; lossMain.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.2f);
            var halo = ParticleMaterial("UpgradeFlash", "halo_splash");
            var dollars = ParticleMaterial("VictoryDollar", "Dollar_Green");
            var flashMaterial = ParticleMaterial("PhotographFlash", "halo_splash");
            var upgrade = Particles("Upgrade Flash", effects.transform, halo, 2.1f, 0.5f, 0);
            var upgradeMain = upgrade.main; upgradeMain.startColor = new Color(0.22f, 1, 0.04f, 0.8f); upgradeMain.maxParticles = 2;
            var shape = upgrade.shape; shape.enabled = false;
            var size = upgrade.sizeOverLifetime; size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
                new Keyframe(0, 0.2f), new Keyframe(0.2f, 1), new Keyframe(1, 1.3f)));
            var winDollars = Particles("Victory Dollars", effects.transform, dollars, 0.3f, 1.9f, 2.1f);
            ConfigureFountain(winDollars);
            var winBills = Particles("Victory Bills", effects.transform, bills.GetComponent<ParticleSystemRenderer>().sharedMaterial, 1, 2.2f, 2.4f);
            ConfigureFountain(winBills);
            var renderer = winBills.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = billMesh;
            var rotation = winBills.rotationOverLifetime; rotation.enabled = true; rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-4, 4); rotation.z = new ParticleSystem.MinMaxCurve(-5, 5);
            Bind(effects, "upgradeFlash", upgrade); Bind(effects, "victoryBills", winBills); Bind(effects, "victoryDollars", winDollars);
            Bind(effects, "audioFeedback", audio); Bind(effects, "motor", motor);

            const string levelPath = Root + "Prefabs/Level01.prefab";
            var level = PrefabUtility.LoadPrefabContents(levelPath);
            try
            {
                var course = level.GetComponentInChildren<FinishCourse>();
                var photographer = course.transform.Find("Photographer");
                var flash = Particles("Photograph Flash", photographer, flashMaterial, 0.8f, 0.18f, 0);
                flash.transform.localPosition = new Vector3(0, 1.1f, 0.3f);
                var flashMain = flash.main; flashMain.maxParticles = 2;
                var flashShape = flash.shape; flashShape.enabled = false;
                Bind(course, "photographFlash", flash);
                PrefabUtility.SaveAsPrefabAsset(level, levelPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(level); }
            var manager = Object.FindFirstObjectByType<LevelManager>(); manager.Init();
            Bind(motor, "path", manager.CurrentLevelInstance.GetComponent<TrackPath>());
            Object.FindFirstObjectByType<RunnerCamera>().Snap();
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/audio-clips.txt", AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Sounds" })
                .Select(AssetDatabase.GUIDToAssetPath).Select(path => {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    return $"{path}: {clip.length:F3}s, {clip.channels}ch, {clip.frequency}Hz";
                }));
        }

        private static void BuildResultBackdrop()
        {
            var hud = Object.FindFirstObjectByType<RunResultHud>();
            var screen = hud.transform.Find("Safe Area/Result Screen");
            var backdrop = screen.Find("Victory Backdrop") as RectTransform;
            if (backdrop == null)
            {
                backdrop = new GameObject("Victory Backdrop", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                backdrop.SetParent(screen, false);
            }
            backdrop.SetAsFirstSibling();
            backdrop.anchorMin = new Vector2(0, 1); backdrop.anchorMax = Vector2.one;
            backdrop.pivot = new Vector2(0.5f, 1); backdrop.anchoredPosition = Vector2.zero;
            backdrop.sizeDelta = new Vector2(0, 405);
            var image = backdrop.GetComponent<Image>(); image.color = new Color(0.12f, 0.63f, 0.97f, 1);
            image.raycastTarget = false;
            Bind(hud, "victoryBackdrop", backdrop.gameObject);
        }

        private static Mesh BillMesh()
        {
            var source = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Visual/Mesh/bill.asset");
            var mesh = Object.Instantiate(source); mesh.name = "EffectBill";
            var vertices = mesh.vertices;
            float scale = 0.35f / Mathf.Max(source.bounds.size.x, source.bounds.size.y, source.bounds.size.z);
            var rotation = Quaternion.Euler(0, 0, 90);
            for (int i = 0; i < vertices.Length; i++) vertices[i] = rotation * ((vertices[i] - source.bounds.center) * scale);
            mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            const string path = Root + "Settings/EffectBill.asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); return saved;
        }

        private static RunAudio BuildAudio(RunSession session, PlayerWealth wealth, RunnerMotor motor)
        {
            const string path = Root + "Settings/RunAudioSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<RunAudioSettings>(path);
            if (settings == null) { settings = ScriptableObject.CreateInstance<RunAudioSettings>(); AssetDatabase.CreateAsset(settings, path); }
            var data = new SerializedObject(settings);
            string[] fields = { "Collect", "LoseMoney", "Upgrade", "Gate", "Victory", "Defeat", "Click", "Photograph" };
            string[] files = { "collect_coin", "RemoveMoney", "shortcutrun_sfx_envir_boost_champignon_01", "shortcutrun_sfx_joueur_bonus_multiplier_x01_to_x05_variation01",
                "shortcutrun_sfx_jingle_victory", "App Error", "click", "photograph" };
            for (int i = 0; i < fields.Length; i++)
                data.FindProperty("<" + fields[i] + ">k__BackingField").objectReferenceValue = Clip(files[i]);
            Clips(data, "Footsteps", new[] { "SFX_Footstep_1", "SFX_Footstep_2", "SFX_Footstep_3", "SFX_Footstep_4", "SFX_Footstep_5" });
            Clips(data, "Heels", new[] { "HighHeels_1", "HighHeels_2" });
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(settings);
            var host = GameObject.Find("Run Audio") ?? new GameObject("Run Audio");
            var audio = Get<RunAudio>(host);
            Bind(audio, "session", session); Bind(audio, "wealth", wealth); Bind(audio, "motor", motor);
            Bind(audio, "presentation", motor.GetComponent<PlayerPresentation>()); Bind(audio, "animator", motor.GetComponentInChildren<Animator>());
            Bind(audio, "settings", settings);
            foreach (string channel in new[] { "steps", "effects", "result", "ui" })
            {
                var child = host.transform.Find(channel);
                if (child == null) { child = new GameObject(channel).transform; child.SetParent(host.transform, false); }
                var source = Get<AudioSource>(child.gameObject);
                source.playOnAwake = false; source.loop = false; source.spatialBlend = 0; source.volume = 1;
                Bind(audio, channel, source);
            }
            return audio;
        }
        private static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/AudioClip/" + name + ".ogg");
        private static void Clips(SerializedObject data, string name, string[] names)
        {
            var list = data.FindProperty("<" + name + ">k__BackingField"); list.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = Clip(names[i]);
        }
        private static void ConfigureFountain(ParticleSystem system)
        {
            var shape = system.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 25; shape.radius = 0.5f;
            shape.rotation = new Vector3(-90, 0, 0);
            var main = system.main; main.gravityModifier = 0.13f;
        }
        private static ParticleSystem Particles(string name, Transform parent, Material material, float size, float lifetime, float speed)
        {
            var old = parent.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject);
            var host = new GameObject(name); host.transform.SetParent(parent, false);
            var system = host.AddComponent<ParticleSystem>(); system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main; main.playOnAwake = false; main.loop = false; main.duration = 3;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startLifetime = lifetime;
            main.startSize = size; main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.6f, speed); main.maxParticles = 64;
            var emission = system.emission; emission.enabled = false;
            var sizeOverLife = system.sizeOverLifetime; sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.65f));
            var colors = system.colorOverLifetime; colors.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.45f), new GradientAlphaKey(0, 1) });
            colors.color = gradient;
            var renderer = system.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return system;
        }
        private static Material ParticleMaterial(string name, string texture)
        {
            string path = Root + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(material, path); }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Visual/Texture2D/" + texture + ".png"));
            material.SetColor("_BaseColor", Color.white); material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000; EditorUtility.SetDirty(material); return material;
        }
        private static T Get<T>(GameObject host) where T : Component
        { return host.TryGetComponent<T>(out var component) ? component : host.AddComponent<T>(); }
        private static void Bind(Object target, string name, Object value)
        { var data = new SerializedObject(target); data.FindProperty(name).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
    }
}

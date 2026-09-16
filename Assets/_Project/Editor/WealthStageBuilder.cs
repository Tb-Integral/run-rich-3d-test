using System.Linq;
using ButchersGames;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace RunRich.Editor
{
    public static class WealthStageBuilder
    {
        private const string Root = "Assets/_Project/";
        [MenuItem("Tools/Run Rich/Prepare Wealth Stage")]
        private static void FromMenu()
        {
            if (!EditorUtility.DisplayDialog("Подготовить пикапы?", "Проверочная расстановка пикапов, шкала и эффекты будут восстановлены в Gameplay и Level01.", "Подготовить", "Отмена")) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build();
        }

        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(VisualFoundationBuilder.ScenePath);
            var settings = AssetDatabase.LoadAssetAtPath<WealthSettings>(Root + "Settings/WealthSettings.asset");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<WealthSettings>();
                AssetDatabase.CreateAsset(settings, Root + "Settings/WealthSettings.asset");
            }
            var surface = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Avatar.mat");
            var money = MakePickup("MoneyPickup", "bills", TrackPickup.PickupKind.Money, surface, new Vector3(1.7f, 1.3f, 1.4f));
            var alcohol = MakePickup("AlcoholPickup", "bottle", TrackPickup.PickupKind.Alcohol, surface, Vector3.one * 1.25f);
            const string levelPath = Root + "Prefabs/Level01.prefab";
            var root = PrefabUtility.LoadPrefabContents(levelPath);
            try
            {
                var previous = root.transform.Find("Pickups");
                if (previous != null) Object.DestroyImmediate(previous.gameObject);
                var container = new GameObject("Pickups").transform;
                container.SetParent(root.transform, false);
                var track = root.GetComponent<TrackPath>();
                for (int i = 0; i < 17; i++) Place(money, track, container, 6 + i * 0.65f, 0);
                Place(alcohol, track, container, 20, 0);
                Place(alcohol, track, container, 22, 0);
                for (int i = 0; i < 3; i++) Place(alcohol, track, container, 7 + i * 3, -1.3f);
                for (int i = 0; i < 20; i++) Place(money, track, container, 33 + i * 0.65f, 0);
                for (int i = 0; i < 20; i++) Place(money, track, container, 61 + i * 0.7f, 0);
                for (int i = 0; i < 8; i++) Place(money, track, container, 77 + i * 0.75f, 1.25f);
                Place(alcohol, track, container, 68, -1.3f);
                Place(alcohol, track, container, 72, -1.3f);
                PrefabUtility.SaveAsPrefabAsset(root, levelPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            var manager = Object.FindFirstObjectByType<LevelManager>();
            manager.Init();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            var presentation = motor.GetComponent<PlayerPresentation>();
            var presentationData = new SerializedObject(presentation);
            var outfits = presentationData.FindProperty("outfits");
            string[] names = { "poor", "casual", "middle", "bling", "cocktail" };
            var meshes = motor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            outfits.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
                outfits.GetArrayElementAtIndex(i).objectReferenceValue = meshes.Single(mesh => mesh.name == names[i]).gameObject;
            presentationData.ApplyModifiedPropertiesWithoutUndo();
            Bind(motor, "path", manager.CurrentLevelInstance.GetComponent<TrackPath>());
            var session = Object.FindFirstObjectByType<RunSession>();
            var wealth = Get<PlayerWealth>(motor.gameObject);
            Bind(wealth, "settings", settings); Bind(wealth, "presentation", motor.GetComponent<PlayerPresentation>());
            var collector = Get<PickupCollector>(motor.gameObject);
            Bind(collector, "motor", motor); Bind(collector, "session", session); Bind(collector, "wealth", wealth);
            Bind(session, "wealth", wealth); Bind(session, "pickups", collector);
            BuildHud(wealth, session, motor.transform);
            BuildEffects(wealth, session, motor.transform, surface);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static GameObject MakePickup(string name, string modelName, TrackPickup.PickupKind kind, Material surface, Vector3 scale)
        {
            var host = new GameObject(name);
            var pickup = host.AddComponent<TrackPickup>();
            var data = new SerializedObject(pickup);
            data.FindProperty("kind").enumValueIndex = (int)kind;
            data.ApplyModifiedPropertiesWithoutUndo();
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Visual/Mesh/LowPoly/" + modelName + ".fbx"));
            model.transform.SetParent(host.transform, false);
            model.transform.localScale = scale;
            model.transform.localRotation = Quaternion.Euler(0, kind == TrackPickup.PickupKind.Money ? 90 : 180, 0);
            model.transform.localPosition = Vector3.up * 0.13f;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = Enumerable.Repeat(surface, renderer.sharedMaterials.Length).ToArray();
            var prefab = PrefabUtility.SaveAsPrefabAsset(host, Root + "Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(host);
            return prefab;
        }

        private static void Place(GameObject prefab, TrackPath track, Transform parent, float distance, float offset)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = prefab.name + " " + distance.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            var pose = track.Evaluate(distance);
            go.transform.SetPositionAndRotation(pose.position + pose.rotation * Vector3.right * offset, pose.rotation);
            var data = new SerializedObject(go.GetComponent<TrackPickup>());
            data.FindProperty("distance").floatValue = distance;
            data.FindProperty("lateralOffset").floatValue = offset;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildHud(PlayerWealth wealth, RunSession session, Transform runner)
        {
            var canvas = Object.FindFirstObjectByType<RunHud>().GetComponent<Canvas>();
            var old = canvas.transform.Find("Wealth HUD");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var host = Rect("Wealth HUD", canvas.transform, Vector2.zero, Vector2.zero);
            host.anchorMin = Vector2.zero; host.anchorMax = Vector2.one; host.offsetMin = host.offsetMax = Vector2.zero;
            var bar = Rect("Status Bar", host, Vector2.zero, new Vector2(224, 32));
            Panel(bar, Color.white);
            var background = Rect("Background", bar, Vector2.zero, new Vector2(218, 26));
            Panel(background, new Color(0.76f, 0.76f, 0.76f));
            var fill = Rect("Fill", background, Vector2.zero, Vector2.zero);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(40f / 150, 1); fill.offsetMin = fill.offsetMax = Vector2.zero;
            var graphic = Panel(fill, settingsColor());
            var status = Label("Status", bar, new Vector2(0, 40), new Vector2(390, 45), 27);
            status.text = "БЕДНЫЙ"; status.color = settingsColor();
            var popup = Label("Pickup Amount", host, Vector2.zero, new Vector2(330, 100), 56);
            popup.gameObject.SetActive(false);
            var hud = host.gameObject.AddComponent<WealthHud>();
            Bind(hud, "wealth", wealth); Bind(hud, "session", session); Bind(hud, "target", runner);
            Bind(hud, "worldCamera", Camera.main); Bind(hud, "canvasRect", canvas.transform);
            Bind(hud, "bar", bar); Bind(hud, "fill", fill); Bind(hud, "fillGraphic", graphic);
            Bind(hud, "status", status); Bind(hud, "popup", popup);
            bar.gameObject.SetActive(false);
            Color settingsColor() => wealth.Settings.TierFor(wealth.Settings.InitialScore).color;
        }

        private static void BuildEffects(PlayerWealth wealth, RunSession session, Transform runner, Material surface)
        {
            var old = runner.Find("Pickup Effects");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var host = new GameObject("Pickup Effects");
            host.transform.SetParent(runner, false);
            host.transform.localPosition = Vector3.up * 0.85f;
            var bills = Particles("Bills", host.transform, 0.38f, 1.3f);
            var billsRenderer = bills.GetComponent<ParticleSystemRenderer>();
            billsRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            billsRenderer.mesh = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Visual/Mesh/LowPoly/bills.fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
            billsRenderer.sharedMaterial = surface;
            var rotation = bills.rotationOverLifetime; rotation.enabled = true; rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-3, 3); rotation.z = new ParticleSystem.MinMaxCurve(-4, 4);
            var stars = Particles("Green Sparkles", host.transform, 0.18f, 1.1f);
            var starMaterial = Material("PickupSparkle", Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            starMaterial.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Visual/Texture2D/stars.png"));
            starMaterial.SetFloat("_Surface", 1); starMaterial.SetFloat("_Blend", 0);
            starMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); starMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            starMaterial.SetFloat("_ZWrite", 0); starMaterial.SetFloat("_Cull", 0);
            starMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); starMaterial.renderQueue = 3000;
            EditorUtility.SetDirty(starMaterial);
            stars.GetComponent<ParticleSystemRenderer>().sharedMaterial = starMaterial;
            var starMain = stars.main; starMain.startColor = new Color(0.3f, 1, 0.05f);
            var loss = Particles("Red Loss", host.transform, 0.11f, 1.5f);
            var redMaterial = Material("PickupLoss", Shader.Find("Universal Render Pipeline/Unlit"));
            redMaterial.SetColor("_BaseColor", new Color(1, 0.02f, 0.02f)); EditorUtility.SetDirty(redMaterial);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var lossRenderer = loss.GetComponent<ParticleSystemRenderer>();
            lossRenderer.renderMode = ParticleSystemRenderMode.Mesh; lossRenderer.mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            lossRenderer.sharedMaterial = redMaterial; Object.DestroyImmediate(sphere);
            var feedback = host.AddComponent<PickupFeedback>();
            Bind(feedback, "wealth", wealth); Bind(feedback, "session", session);
            Bind(feedback, "bills", bills); Bind(feedback, "stars", stars); Bind(feedback, "loss", loss);
        }

        private static ParticleSystem Particles(string name, Transform parent, float size, float speed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main; main.playOnAwake = false; main.loop = false; main.duration = 1;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed); main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size);
            main.maxParticles = 32; main.gravityModifier = 0.15f;
            var emission = system.emission; emission.enabled = false;
            var shape = system.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.3f;
            var sizeOverLife = system.sizeOverLifetime; sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
            var renderer = system.GetComponent<ParticleSystemRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return system;
        }

        private static Material Material(string name, Shader shader)
        {
            string path = Root + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            return material;
        }
        private static T Get<T>(GameObject go) where T : Component => go.TryGetComponent<T>(out var component) ? component : go.AddComponent<T>();
        private static void Bind(Object target, string property, Object value)
        {
            var data = new SerializedObject(target); data.FindProperty(property).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static RoundedPanel Panel(RectTransform rect, Color color)
        {
            var panel = rect.gameObject.AddComponent<RoundedPanel>(); panel.color = color; panel.raycastTarget = false; return panel;
        }
        private static Text Label(string name, Transform parent, Vector2 position, Vector2 size, int fontSize)
        {
            var text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Visual/Fonts/Inter-SemiBold.ttf");
            text.fontSize = fontSize; text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            var outline = text.gameObject.AddComponent<Outline>(); outline.effectColor = Color.white; outline.effectDistance = new Vector2(2, -2);
            return text;
        }
    }
}

using System.Linq;
using ButchersGames;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace RunRich.Editor
{
    public static class FirstLevelBuilder
    {
        private const string Root = "Assets/_Project/";
        [MenuItem("Tools/Run Rich/Prepare First Level")]
        private static void FromMenu()
        {
            if (!EditorUtility.DisplayDialog("Собрать первый уровень?", "Маршрут и расстановка Level01 будут восстановлены по разбору видео. Изменения расстановки в префабе будут заменены.", "Собрать", "Отмена")) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build();
        }

        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(VisualFoundationBuilder.ScenePath);
            const string alcoholPath = Root + "Prefabs/AlcoholPickup.prefab";
            var alcoholRoot = PrefabUtility.LoadPrefabContents(alcoholPath);
            try
            {
                alcoholRoot.transform.GetChild(0).localRotation = Quaternion.Euler(0, 180, 0);
                PrefabUtility.SaveAsPrefabAsset(alcoholRoot, alcoholPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(alcoholRoot); }
            var gate = BuildGate();
            var flags = BuildFlags();
            var money = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/MoneyPickup.prefab");
            var alcohol = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/AlcoholPickup.prefab");
            const string path = Root + "Prefabs/Level01.prefab";
            var level = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // Level01 содержит только подготовленную геометрию и объекты уровня.
                foreach (Transform child in level.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                var track = level.GetComponent<TrackPath>();
                var data = new SerializedObject(track);
                var segments = data.FindProperty("segments"); segments.arraySize = 7;
                float[] lengths = { 24, 2 * Mathf.PI, 25, 2 * Mathf.PI, 24, 2 * Mathf.PI, 20 };
                float[] turns = { 0, 90, 0, -90, 0, 90, 0 };
                for (int i = 0; i < lengths.Length; i++)
                {
                    segments.GetArrayElementAtIndex(i).FindPropertyRelative("length").floatValue = lengths[i];
                    segments.GetArrayElementAtIndex(i).FindPropertyRelative("turnDegrees").floatValue = turns[i];
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                var mesh = MovementStageBuilder.BuildMesh(track);
                const string meshPath = Root + "Settings/FirstLevelTrack.asset";
                var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (savedMesh == null) { AssetDatabase.CreateAsset(mesh, meshPath); savedMesh = mesh; }
                else { EditorUtility.CopySerialized(mesh, savedMesh); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(savedMesh); }
                level.GetComponent<MeshFilter>().sharedMesh = savedMesh;
                level.GetComponent<MeshCollider>().sharedMesh = savedMesh;
                var pickups = Child("Pickups", level.transform);
                var choices = Child("Choices and Flags", level.transform);
                for (int i = 0; i < 4; i++) Place(money, track, pickups, 6 + i, 1.3f);
                foreach (float offset in new[] { -1.3f, 0, 1.3f })
                    for (int i = 0; i < 3; i++) Place(money, track, pickups, 11 + i * 1.2f, offset);
                for (int i = 0; i < 3; i++) Place(money, track, pickups, 17 + i * 1.2f, -1.3f);
                Place(alcohol, track, pickups, 15.5f, -1.3f);
                Place(alcohol, track, pickups, 22, 0);
                Place(flags, track, choices, 32, 0);
                foreach (float offset in new[] { -1.3f, 0, 1.3f })
                    for (int i = 0; i < 5; i++) Place(money, track, pickups, 37 + i, offset);
                foreach (float offset in new[] { -1.3f, 0, 0.65f }) Place(alcohol, track, pickups, 44, offset);
                Place(gate, track, choices, 51.5f, 0);
                Place(flags, track, choices, 63, 0);
                foreach (float offset in new[] { 0.5f, 1.4f }) Place(alcohol, track, pickups, 68, offset);
                foreach (float offset in new[] { -1.4f, -0.5f, 0.4f }) Place(alcohol, track, pickups, 73.5f, offset);
                foreach (float offset in new[] { -1.5f, -0.8f, -0.1f, 0.6f }) Place(alcohol, track, pickups, 77.4f, offset);
                foreach (float offset in new[] { -1.3f, 0, 1.3f })
                    for (int i = 0; i < 6; i++) Place(money, track, pickups, 79.5f + i, offset);
                Place(flags, track, choices, 93.5f, 0);
                foreach (float offset in new[] { -1.3f, 0, 1.3f })
                    for (int i = 0; i < 7; i++) Place(money, track, pickups, 97 + i, offset);
                var end = Child("Finish Anchor", level.transform);
                var pose = track.Evaluate(track.Length);
                end.SetPositionAndRotation(pose.position, pose.rotation);
                // Запас поверхности для остановки; финишные множители добавляются на этапе 6.
                Box("Finish Landing", end, new Vector3(0, -0.12f, 1.5f), new Vector3(track.Width, 0.24f, 3),
                    AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Track.mat"));
                PrefabUtility.SaveAsPrefabAsset(level, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(level); }
            var manager = Object.FindFirstObjectByType<LevelManager>();
            manager.Init();
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            Bind(motor, "path", manager.CurrentLevelInstance.GetComponent<TrackPath>());
            Object.FindFirstObjectByType<RunnerCamera>().Snap();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static GameObject BuildGate()
        {
            var host = new GameObject("School Party Gate");
            var gate = host.AddComponent<ChoiceGate>();
            var red = Surface("GateRed", new Color(1, 0.06f, 0.015f));
            var green = Surface("GateGreen", new Color(0.28f, 1, 0.015f));
            var white = Surface("ChoiceHighlight", Color.white);
            Transform[] sides = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var side = Child(i == 0 ? "Party" : "School", host.transform); sides[i] = side;
                side.localPosition = Vector3.right * (i == 0 ? -1.025f : 1.025f);
                var material = i == 0 ? red : green;
                Box("Left Post", side, new Vector3(-0.94f, 1.1f, 0), new Vector3(0.16f, 2.2f, 0.16f), material);
                Box("Right Post", side, new Vector3(0.94f, 1.1f, 0), new Vector3(0.16f, 2.2f, 0.16f), material);
                Box("Top", side, new Vector3(0, 2.15f, 0), new Vector3(2.04f, 0.15f, 0.16f), material);
                var sign = MeshObject("Sign", "ChoiceDoor", side);
                sign.localScale = new Vector3(2.85f, 2.85f, 2.85f);
                sign.localPosition = new Vector3(0, 2.15f, -0.11f);
                WorldLabel(i == 0 ? "ВЕЧЕРИНКА" : "ШКОЛА", side, new Vector3(0, 2.2f, -0.19f));
                var highlight = Box("Selected", side, new Vector3(0, 2.52f, 0), new Vector3(1.65f, 0.06f, 0.18f), white);
                highlight.SetActive(false);
                Bind(gate, i == 0 ? "partyHighlight" : "schoolHighlight", highlight);
            }
            var party = Child("Party Icon", sides[0]);
            var partyMesh = MeshObject("Party", "Party", party);
            partyMesh.localRotation = Quaternion.Euler(0, 180, 90);
            Normalize(partyMesh, new Vector3(0, 1.05f, 0), 0.9f);
            var school = Child("School Icon", sides[1]);
            var hat = MeshObject("Hat", "Hat_School", school);
            hat.localScale = Vector3.one * 1.8f; hat.localPosition = new Vector3(0, 1.05f, 0);
            var diploma = MeshObject("Diploma", "Diplome", school);
            diploma.localScale = Vector3.one * 1.6f; diploma.localPosition = new Vector3(0, 0.78f, -0.03f);
            diploma.localRotation = Quaternion.Euler(0, -10, -15);
            Bind(gate, "partyIcon", party.gameObject); Bind(gate, "schoolIcon", school.gameObject);
            var prefab = PrefabUtility.SaveAsPrefabAsset(host, Root + "Prefabs/SchoolPartyGate.prefab");
            Object.DestroyImmediate(host); return prefab;
        }

        private static GameObject BuildFlags()
        {
            var host = new GameObject("Flag Zone");
            var zone = host.AddComponent<FlagZone>();
            Box("Yellow Area", host.transform, new Vector3(0, 0.008f, 1), new Vector3(4.2f, 0.015f, 2),
                Surface("FlagArea", new Color(1, 0.83f, 0.01f)));
            var left = Child("Left Flag Pivot", host.transform);
            left.localPosition = new Vector3(-1.88f, 0.07f, 1.4f); left.localRotation = Quaternion.Euler(0, 180, 90);
            var leftMesh = MeshObject("Flag", "Flag", left); leftMesh.localScale = Vector3.one * 0.3f;
            var right = Child("Right Flag Pivot", host.transform);
            right.localPosition = new Vector3(1.88f, 0.07f, 1.4f); right.localRotation = Quaternion.Euler(0, 0, 90);
            var rightMesh = MeshObject("Flag", "Flag", right); rightMesh.localScale = Vector3.one * 0.3f;
            Bind(zone, "leftFlag", left); Bind(zone, "rightFlag", right);
            var prefab = PrefabUtility.SaveAsPrefabAsset(host, Root + "Prefabs/FlagZone.prefab");
            Object.DestroyImmediate(host); return prefab;
        }

        private static void Place(GameObject prefab, TrackPath track, Transform parent, float distance, float offset)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = prefab.name + " " + distance.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            var pose = track.Evaluate(distance);
            go.transform.SetPositionAndRotation(pose.position + pose.rotation * Vector3.right * offset, pose.rotation);
            var data = new SerializedObject(go.GetComponent<TrackInteraction>());
            data.FindProperty("distance").floatValue = distance;
            data.FindProperty("lateralOffset").floatValue = offset;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static Transform Child(string name, Transform parent)
        {
            var child = new GameObject(name).transform; child.SetParent(parent, false); return child;
        }
        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material; return go;
        }
        private static Transform MeshObject(string name, string mesh, Transform parent)
        {
            var transform = Child(name, parent);
            transform.gameObject.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Visual/Mesh/" + mesh + ".asset");
            var renderer = transform.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Avatar.mat");
            return transform;
        }
        private static void Normalize(Transform mesh, Vector3 center, float height)
        {
            var bounds = mesh.GetComponent<MeshFilter>().sharedMesh.bounds;
            float scale = height / bounds.size.y;
            mesh.localScale = Vector3.one * scale;
            mesh.localPosition = center - mesh.localRotation * (bounds.center * scale);
        }
        private static Material Surface(string name, Color color)
        {
            string path = Root + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("RunRich/Reference Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", color); material.SetColor("_ShadeColor", new Color(0.6f, 0.6f, 0.86f));
            material.SetFloat("_ShadeStrength", 0.2f); EditorUtility.SetDirty(material); return material;
        }
        private static void WorldLabel(string value, Transform parent, Vector3 position)
        {
            var host = new GameObject("Label", typeof(RectTransform), typeof(Canvas));
            var rect = host.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.localPosition = position; rect.localScale = Vector3.one * 0.0038f; rect.sizeDelta = new Vector2(420, 110);
            host.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var panel = host.AddComponent<RoundedPanel>(); panel.color = new Color(0.2f, 0.21f, 0.25f); panel.raycastTarget = false;
            var panelData = new SerializedObject(panel); panelData.FindProperty("cornerRadius").floatValue = 12; panelData.ApplyModifiedPropertiesWithoutUndo();
            var text = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            text.rectTransform.SetParent(rect, false); text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Visual/Fonts/Inter-SemiBold.ttf");
            text.fontSize = value == "ШКОЛА" ? 72 : 54; text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter;
            text.text = value; text.color = Color.white; text.raycastTarget = false;
        }
        private static void Bind(Object target, string property, Object value)
        {
            var data = new SerializedObject(target); data.FindProperty(property).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

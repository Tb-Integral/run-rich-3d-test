using System.Linq;
using ButchersGames;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RunRich.Editor
{
    public static class FinishStageBuilder
    {
        private const string Root = "Assets/_Project/";
        private static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/Visual/Fonts/Inter-SemiBold.ttf");

        [MenuItem("Tools/Run Rich/Prepare Finish Stage")]
        private static void FromMenu()
        {
            if (!EditorUtility.DisplayDialog("Подготовить финиш?", "Финиш Level01 и экран результата будут восстановлены. Основная расстановка останется прежней.", "Подготовить", "Отмена")) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build();
        }

        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(VisualFoundationBuilder.ScenePath);
            const string levelPath = Root + "Prefabs/Level01.prefab";
            var level = PrefabUtility.LoadPrefabContents(levelPath);
            try
            {
                foreach (string name in new[] { "Finish Anchor", "Finish Course" })
                {
                    var old = level.transform.Find(name);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                }
                var track = level.GetComponent<TrackPath>();
                var data = new SerializedObject(track);
                var segments = data.FindProperty("segments");
                segments.GetArrayElementAtIndex(6).FindPropertyRelative("length").floatValue = 66;
                data.ApplyModifiedPropertiesWithoutUndo();
                float start = track.Length - 46;
                var mesh = MovementStageBuilder.BuildMesh(track);
                mesh.name = "FirstLevelTrack";
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "Settings/FirstLevelTrack.asset");
                EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved);
                level.GetComponent<MeshFilter>().sharedMesh = saved;
                level.GetComponent<MeshCollider>().sharedMesh = saved;
                var finish = Child("Finish Course", level.transform);
                var pose = track.Evaluate(start);
                finish.SetPositionAndRotation(pose.position, pose.rotation);
                Set(finish.gameObject.AddComponent<FinishCourse>(), "startDistance", start);
                var white = Surface("FinishWhite", Color.white);
                var black = Surface("FinishBlack", new Color(0.025f, 0.04f, 0.06f));
                for (int row = 0; row < 2; row++)
                    for (int column = 0; column < 10; column++)
                        Box("Finish Tile", finish, new Vector3(-1.89f + column * 0.42f, 0.009f, row * 0.42f),
                            new Vector3(0.42f, 0.018f, 0.42f), (row + column) % 2 == 0 ? white : black);

                var doors = Surface("FinishDoors", Color.white, "door_texture");
                var atlas = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Avatar.mat");
                int[] required = { 20, 65, 105, 140 };
                for (int i = 0; i < 4; i++)
                {
                    float z = 2.2f + 10 * i;
                    var gate = BuildGate(i, doors, atlas);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(gate, finish);
                    instance.transform.localPosition = Vector3.forward * z;
                    var component = instance.GetComponent<FinishGate>();
                    Set(component, "distance", start + z - 1.8f);
                    Set(component, "requiredScore", required[i]);
                    Set(component, "multiplier", i + 2);
                }

                var money = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/MoneyPickup.prefab");
                var bottle = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/AlcoholPickup.prefab");
                foreach (float z in new[] { 5f, 6, 7, 8, 9, 24, 25 }) PlacePickup(money, finish, start, z, -1.25f);
                foreach (float z in new[] { 17f, 28 }) PlacePickup(bottle, finish, start, z, 1.25f);

                BuildMoneyPile(money, finish);

                Carpet("Descent_Carpet", "carpet", finish, 5.2f);
                Carpet("Rich_Carpet", "carpet", finish, 15.2f);
                Carpet("Red_Carpet", "red_carpet", finish, 26);
                foreach (float side in new[] { -1f, 1f })
                {
                    var bench = Mesh("Bench", "Bench", finish, atlas);
                    bench.localPosition = new Vector3(side * 1.85f, 0, 6.8f);
                    bench.localRotation = Quaternion.Euler(0, side * 90, 0);
                    bench.localScale = Vector3.one * 0.85f;
                    for (int i = 0; i < 2; i++)
                    {
                        var trash = Mesh("Trash", "Trash.000", finish, atlas);
                        trash.localScale = Vector3.one * 1.3f;
                        trash.localPosition = new Vector3(side * 1.8f, 0, 1.5f + i * 1.2f);
                    }
                }
                var photographer = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(AnimationAssets.PhotographerPath), finish);
                photographer.name = "Photographer";
                photographer.transform.localPosition = new Vector3(-1.25f, 0, 31);
                photographer.transform.localRotation = Quaternion.Euler(0, 155, 0);
                foreach (var renderer in photographer.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.sharedMaterials = Enumerable.Repeat(atlas, renderer.sharedMesh.subMeshCount).ToArray();
                    renderer.updateWhenOffscreen = true;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                var animator = photographer.GetComponent<Animator>();
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Root + "Animations/Photographer.controller");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                PrefabUtility.SaveAsPrefabAsset(level, levelPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(level); }
            var manager = Object.FindFirstObjectByType<LevelManager>();
            manager.Init();
            Bind(Object.FindFirstObjectByType<RunnerMotor>(), "path", manager.CurrentLevelInstance.GetComponent<TrackPath>());
            Object.FindFirstObjectByType<RunnerCamera>().Snap();
            BuildResultHud();
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }

        private static GameObject BuildGate(int index, Material doors, Material atlas)
        {
            string name = "FinishGateX" + (index + 2);
            var host = new GameObject(name);
            var gate = host.AddComponent<FinishGate>();
            Set(gate, "multiplier", index + 2);
            Set(gate, "requiredScore", new[] { 20, 65, 105, 140 }[index]);
            var visuals = Child("Visuals", host.transform);
            var gateData = new SerializedObject(gate);
            gateData.FindProperty("isFinal").boolValue = index == 3;
            gateData.ApplyModifiedPropertiesWithoutUndo();
            string[] frames = { "Door_Poor_000", "Door_Descent_02", "Door_Rich_00" };
            string[] left = { "Door_Poor_002", "Door_Descent_00", "Door_Rich_01" };
            string[] right = { "Door_Poor_001", "Door_Descent_01", "Door_Rich_02" };
            const float scale = 1.85f;
            var model = Child("Model", visuals); model.localScale = Vector3.one * scale;
            var leftPivot = Child("Left Hinge", model); leftPivot.localPosition = Vector3.left * 0.9f;
            var rightPivot = Child("Right Hinge", model); rightPivot.localPosition = Vector3.right * 0.9f;
            if (index < 3)
            {
                Mesh("Frame", frames[index], model, atlas);
                Mesh("Left Door", left[index], leftPivot, doors).localPosition = Vector3.right * 0.9f;
                Mesh("Right Door", right[index], rightPivot, doors).localPosition = Vector3.left * 0.9f;
            }
            else
            {
                var gold = Surface("FinishGold", new Color(1, 0.74f, 0.05f));
                var door = Mesh("Right Door", "Door_Million_00", rightPivot, gold);
                door.localPosition = Vector3.left * 0.9f;
                var other = Mesh("Left Door", "Door_Million_00", leftPivot, gold);
                other.localScale = new Vector3(-1, 1, 1); other.localPosition = Vector3.right * 0.9f;
                foreach (float x in new[] { -1.12f, 1.12f })
                    Box("Gold Post", model, new Vector3(x, 0.95f, 0), new Vector3(0.12f, 1.9f, 0.12f), gold);
            }
            Bind(gate, "leftDoor", leftPivot); Bind(gate, "rightDoor", rightPivot);
            string[] signs = { "EndLevel_Orange", "EndLevel_Yellow", "EndLevel_Green", "EndLevel_Blue" };
            var sign = Mesh("Multiplier Sign", signs[index], visuals, atlas);
            sign.localScale = Vector3.one * 1.7f;
            sign.localPosition = new Vector3(0, index >= 2 ? 3.05f : 2.35f, -0.18f);
            var world = new GameObject("Multiplier", typeof(RectTransform), typeof(Canvas));
            var rect = world.GetComponent<RectTransform>(); rect.SetParent(visuals, false);
            rect.localPosition = sign.localPosition + new Vector3(0, 0.35f, -0.05f);
            rect.localScale = Vector3.one * 0.0035f; rect.sizeDelta = new Vector2(250, 170);
            world.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var text = Label("Text", rect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250, 170), "×" + (index + 2), 130);
            text.fontStyle = FontStyle.Bold;
            var result = PrefabUtility.SaveAsPrefabAsset(host, Root + "Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(host); return result;
        }

        private static void BuildResultHud()
        {
            var canvas = Object.FindFirstObjectByType<RunHud>().transform;
            var safe = canvas.Find("Safe Area");
            var old = safe.Find("Result Screen"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var hud = canvas.GetComponent<RunResultHud>() ?? canvas.gameObject.AddComponent<RunResultHud>();
            var screen = Rect("Result Screen", safe, Vector2.zero, Vector2.zero, Vector2.zero);
            screen.anchorMin = Vector2.zero; screen.anchorMax = Vector2.one; screen.offsetMin = screen.offsetMax = Vector2.zero;
            var title = Label("Result Title", screen, new Vector2(0.5f, 1), new Vector2(0, -295), new Vector2(850, 130), "ВЫ ПОБЕДИЛИ!", 70);
            var outline = title.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(0.18f, 0.48f, 0.69f); outline.effectDistance = new Vector2(3, -3);
            var details = Label("Result Details", screen, new Vector2(0.5f, 1), new Vector2(0, -420), new Vector2(760, 120), "", 36);
            var next = Button("Next Level", screen, new Vector2(0, 315), new Color(0, 0.73f, 0.93f), "ДАЛЕЕ", out var nextLabel);
            var retry = Button("Retry", screen, new Vector2(0, 315), new Color(0.98f, 0.35f, 0.13f), "ЕЩЁ РАЗ", out _);
            Bind(hud, "session", Object.FindFirstObjectByType<RunSession>()); Bind(hud, "screen", screen.gameObject);
            Bind(hud, "title", title); Bind(hud, "details", details); Bind(hud, "actionLabel", nextLabel);
            Bind(hud, "next", next); Bind(hud, "retry", retry);
            screen.gameObject.SetActive(false);
        }

        private static Button Button(string name, Transform parent, Vector2 position, Color color, string value, out Text label)
        {
            var rect = Rect(name, parent, new Vector2(0.5f, 0), position, new Vector2(520, 165));
            var panel = rect.gameObject.AddComponent<RoundedPanel>(); panel.color = color;
            var shadow = rect.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0.25f, 0.4f, 0.7f); shadow.effectDistance = new Vector2(0, -8);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = panel;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            label = Label("Label", rect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(490, 145), value, 44);
            label.fontStyle = FontStyle.Bold; return button;
        }
        private static void BuildMoneyPile(GameObject money, Transform parent)
        {
            var pile = Child("Money Pile", parent);
            pile.localPosition = new Vector3(0, 0, 37);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Visual/Mesh/LowPoly/bills.fbx");
            var material = money.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            for (int layer = 0; layer < 7; layer++)
            {
                int columns = 7 - layer;
                int rows = 8 - layer;
                for (int row = 0; row < rows; row++)
                    for (int column = 0; column < columns; column++)
                    {
                        // За закрытым финишем находятся декорации без компонента подбора.
                        var bundle = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pile);
                        bundle.name = "Money Bundle";
                        bundle.transform.localPosition = new Vector3((column - (columns - 1) * 0.5f) * 0.45f,
                            0.12f + layer * 0.2f, (row - (rows - 1) * 0.5f) * 0.5f);
                        bundle.transform.localScale = Vector3.one * 1.4f;
                        bundle.transform.localRotation = Quaternion.Euler(0, 90 + ((row + column + layer) % 3 - 1) * 12, 0);
                        foreach (var renderer in bundle.GetComponentsInChildren<MeshRenderer>())
                        {
                            renderer.sharedMaterial = material;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        }
                    }
            }
        }

        private static void Carpet(string mesh, string texture, Transform parent, float z)
        {
            var carpet = Mesh(mesh, mesh, parent, Surface("Finish" + mesh, Color.white, texture));
            carpet.localPosition = new Vector3(0, 0.012f, z); carpet.localRotation = Quaternion.Euler(0, -90, 0);
            carpet.localScale = new Vector3(0.9f, 1, 1.9f);
        }
        private static void PlacePickup(GameObject prefab, Transform parent, float start, float z, float x)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            obj.transform.localPosition = new Vector3(x, 0, z);
            Set(obj.GetComponent<TrackInteraction>(), "distance", start + z);
            Set(obj.GetComponent<TrackInteraction>(), "lateralOffset", x);
        }
        private static Transform Child(string name, Transform parent)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
        private static Transform Mesh(string name, string asset, Transform parent, Material material)
        {
            var t = Child(name, parent);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Visual/Mesh/" + asset + ".asset");
            t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material; return t;
        }
        private static void Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        private static Material Surface(string name, Color color, string texture = null)
        {
            string path = Root + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("RunRich/Reference Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", color); material.SetColor("_ShadeColor", new Color(0.65f, 0.63f, 0.75f));
            material.SetFloat("_ShadeStrength", 0.25f);
            if (texture != null) material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Visual/Texture2D/" + texture + ".png"));
            EditorUtility.SetDirty(material); return material;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static Text Label(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string value, int sizeFont)
        {
            var text = Rect(name, parent, anchor, position, size).gameObject.AddComponent<Text>();
            text.font = Font; text.fontSize = sizeFont; text.text = value; text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; return text;
        }
        private static void Bind(Object obj, string property, Object value)
        { var data = new SerializedObject(obj); data.FindProperty(property).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Set(Object obj, string property, float value)
        {
            var data = new SerializedObject(obj); var field = data.FindProperty(property);
            if (field.propertyType == SerializedPropertyType.Integer) field.intValue = (int)value; else field.floatValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

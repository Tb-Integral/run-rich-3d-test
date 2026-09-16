using System.Collections.Generic;
using ButchersGames;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RunRich.Editor
{
    public static class RunStageBuilder
    {
        [MenuItem("Tools/Run Rich/Prepare Run Session Stage")]
        private static void FromMenu()
        {
            if (!EditorUtility.DisplayDialog("Подготовить цикл забега?", "Префаб дорожки, список уровней и игровой UI будут восстановлены из Gameplay.", "Подготовить", "Отмена")) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build();
        }

        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(VisualFoundationBuilder.ScenePath);
            var motor = Object.FindFirstObjectByType<RunnerMotor>();
            var track = motor.Path;
            var template = Object.Instantiate(track.gameObject);
            if (PrefabUtility.IsPartOfPrefabInstance(template))
                PrefabUtility.UnpackPrefabInstance(template, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            template.name = "Level01";
            template.transform.SetParent(null);
            template.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (template.GetComponent<Level>() == null) template.AddComponent<Level>();
            const string prefabPath = "Assets/_Project/Prefabs/Level01.prefab";
            PrefabUtility.SaveAsPrefabAsset(template, prefabPath);
            Object.DestroyImmediate(template);
            if (track != null) Object.DestroyImmediate(track.gameObject);
            var levelHost = GameObject.Find("Levels") ?? new GameObject("Levels");
            var manager = Get<LevelManager>(levelHost);
            const string listPath = "Assets/_Project/Settings/PlayableLevels.asset";
            var list = AssetDatabase.LoadAssetAtPath<LevelsList>(listPath);
            if (list == null) { list = ScriptableObject.CreateInstance<LevelsList>(); AssetDatabase.CreateAsset(list, listPath); }
            list.lvls = new List<Level> { AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponent<Level>() };
            list.randomizedLvls = false;
            EditorUtility.SetDirty(list);
            Bind(manager, "levels", list);
            manager.Init();
            var levelSource = Get<RunLevelSource>(levelHost);
            Bind(levelSource, "manager", manager);
            Bind(motor, "path", manager.CurrentLevelInstance.GetComponent<TrackPath>());
            var motorData = new SerializedObject(motor);
            motorData.FindProperty("startAutomatically").boolValue = false;
            motorData.ApplyModifiedPropertiesWithoutUndo();
            var host = GameObject.Find("Run Session") ?? new GameObject("Run Session");
            var session = Get<RunSession>(host);
            Bind(session, "motor", motor);
            Bind(session, "input", motor.GetComponent<RunnerDragInput>());
            Bind(session, "presentation", motor.GetComponent<PlayerPresentation>());
            Bind(session, "followCamera", Camera.main.GetComponent<RunnerCamera>());
            Bind(session, "levels", levelSource);

            var previousCanvas = GameObject.Find("Gameplay UI");
            if (previousCanvas != null) Object.DestroyImmediate(previousCanvas);
            var canvasObject = new GameObject("Gameplay UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(880, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            var safe = Rect("Safe Area", canvasObject.transform, Vector2.zero, Vector2.zero, Vector2.zero);
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Visual/Fonts/Inter-SemiBold.ttf");
            var levelLabel = Label("Level", safe, new Vector2(0.5f, 1), new Vector2(0, -88), new Vector2(480, 70), "УРОВЕНЬ 1", 44, font);
            var scoreLabel = Label("Score", safe, new Vector2(0.5f, 1), new Vector2(0, -155), new Vector2(400, 70), "40 $", 48, font);
            var restartRect = Rect("Restart", safe, Vector2.one, new Vector2(-76, -86), new Vector2(88, 88));
            var background = restartRect.gameObject.AddComponent<RoundedPanel>();
            background.color = new Color(0.08f, 0.22f, 0.29f, 0.85f);
            var button = restartRect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var icon = Picture("Icon", restartRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60), "restart");
            icon.color = Color.white;

            var tutorial = Rect("Tutorial", safe, Vector2.zero, Vector2.zero, Vector2.zero);
            tutorial.anchorMin = Vector2.zero; tutorial.anchorMax = Vector2.one; tutorial.offsetMin = tutorial.offsetMax = Vector2.zero;
            var hint = Rect("Hint", tutorial, new Vector2(0.5f, 0), new Vector2(0, 485), new Vector2(540, 142));
            var hintBackground = hint.gameObject.AddComponent<RoundedPanel>();
            hintBackground.color = new Color(0.23f, 0.24f, 0.27f, 0.78f);
            hintBackground.raycastTarget = false;
            Label("Hint Text", hint, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(490, 110), "ПРОВЕДИТЕ ПО ЭКРАНУ, ЧТОБЫ\nПОВЕРНУТЬ", 28, font);
            Picture("Arrows", tutorial, new Vector2(0.5f, 0), new Vector2(0, 325), new Vector2(500, 85), "arrow_left_right");
            var hand = Picture("Hand", tutorial, new Vector2(0.5f, 0), new Vector2(0, 285), new Vector2(110, 125), "hand");
            var hud = canvasObject.AddComponent<RunHud>();
            Bind(hud, "session", session); Bind(hud, "levelLabel", levelLabel); Bind(hud, "scoreLabel", scoreLabel);
            Bind(hud, "tutorial", tutorial.gameObject); Bind(hud, "hand", hand.rectTransform); Bind(hud, "restart", button); Bind(hud, "safeArea", safe);
            Bind(session, "hud", hud);
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem));
                var input = events.AddComponent<InputSystemUIInputModule>();
                input.AssignDefaultActions();
            }
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static T Get<T>(GameObject obj) where T : Component => obj.TryGetComponent<T>(out var c) ? c : obj.AddComponent<T>();
        private static void Bind(Object target, string property, Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(property).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        private static Text Label(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string value, int fontSize, Font font)
        {
            var text = Rect(name, parent, anchor, position, size).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = fontSize; text.text = value; text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white; text.raycastTarget = false;
            text.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1, -2);
            return text;
        }
        private static Image Picture(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string sprite)
        {
            var image = Rect(name, parent, anchor, position, size).gameObject.AddComponent<Image>();
            string path = "Assets/Visual/Texture2D/" + sprite + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled || !importer.alphaIsTransparency)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }
    }
}

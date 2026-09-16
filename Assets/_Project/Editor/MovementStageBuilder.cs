using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RunRich.Editor
{
    public static class MovementStageBuilder
    {
        [MenuItem("Tools/Run Rich/Prepare Movement Stage")]
        private static void FromMenu()
        {
            if (!EditorUtility.DisplayDialog("Подготовить этап движения?", "Тестовая дорожка и настройки движения в Gameplay будут восстановлены. Остальные объекты сцены сохранятся.", "Подготовить", "Отмена")) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build();
        }

        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(VisualFoundationBuilder.ScenePath);
            var environment = GameObject.Find("Environment").transform;
            var old = environment.Find("First straight");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var trackObject = environment.Find("Movement track");
            if (trackObject == null)
            {
                trackObject = new GameObject("Movement track").transform;
                trackObject.SetParent(environment, false);
            }
            var path = Get<TrackPath>(trackObject.gameObject);
            var data = new SerializedObject(path);
            data.FindProperty("width").floatValue = 4.2f;
            var segments = data.FindProperty("segments");
            segments.arraySize = 5;
            float[] lengths = { 24, Mathf.PI * 2, 22, Mathf.PI * 2, 30 };
            float[] turns = { 0, 90, 0, -90, 0 };
            for (int i = 0; i < lengths.Length; i++)
            {
                var segment = segments.GetArrayElementAtIndex(i);
                segment.FindPropertyRelative("length").floatValue = lengths[i];
                segment.FindPropertyRelative("turnDegrees").floatValue = turns[i];
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            var mesh = BuildMesh(path);
            const string meshPath = "Assets/_Project/Settings/MovementTrack.asset";
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existingMesh == null) AssetDatabase.CreateAsset(mesh, meshPath);
            else { EditorUtility.CopySerialized(mesh, existingMesh); Object.DestroyImmediate(mesh); mesh = existingMesh; EditorUtility.SetDirty(mesh); }
            Get<MeshFilter>(trackObject.gameObject).sharedMesh = mesh;
            Get<MeshRenderer>(trackObject.gameObject).sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Track.mat");
            Get<MeshCollider>(trackObject.gameObject).sharedMesh = mesh;

            const string settingsPath = "Assets/_Project/Settings/RunnerSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<RunnerSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<RunnerSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
            }
            var runner = GameObject.Find("Runner");
            var input = Get<RunnerDragInput>(runner);
            var motor = Get<RunnerMotor>(runner);
            var motorData = new SerializedObject(motor);
            motorData.FindProperty("path").objectReferenceValue = path;
            motorData.FindProperty("settings").objectReferenceValue = settings;
            motorData.FindProperty("dragInput").objectReferenceValue = input;
            motorData.FindProperty("presentation").objectReferenceValue = runner.GetComponent<PlayerPresentation>();
            motorData.ApplyModifiedPropertiesWithoutUndo();
            var camera = Get<RunnerCamera>(Camera.main.gameObject);
            var cameraData = new SerializedObject(camera);
            cameraData.FindProperty("target").objectReferenceValue = motor;
            cameraData.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static T Get<T>(GameObject target) where T : Component => target.TryGetComponent<T>(out var component) ? component : target.AddComponent<T>();

        private static Mesh BuildMesh(TrackPath path)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const float thickness = 0.24f;
            int count = Mathf.CeilToInt((path.Length + 10) / 0.25f);
            Vector3[] previous = null;
            for (int i = 0; i <= count; i++)
            {
                float distance = Mathf.Lerp(-10, path.Length, i / (float)count);
                Pose pose = path.Evaluate(distance);
                if (distance < 0) pose.position += pose.rotation * Vector3.forward * distance;
                Vector3 left = path.transform.InverseTransformPoint(pose.position - pose.rotation * Vector3.right * path.Width * 0.5f);
                Vector3 right = path.transform.InverseTransformPoint(pose.position + pose.rotation * Vector3.right * path.Width * 0.5f);
                var section = new[] { left, right, left - Vector3.up * thickness, right - Vector3.up * thickness };
                if (previous == null) Quad(section[0], section[1], section[3], section[2]);
                else
                {
                    Quad(previous[0], section[0], section[1], previous[1]);
                    Quad(previous[2], previous[3], section[3], section[2]);
                    Quad(previous[0], previous[2], section[2], section[0]);
                    Quad(previous[1], section[1], section[3], previous[3]);
                }
                previous = section;
            }
            Quad(previous[1], previous[0], previous[2], previous[3]);
            var mesh = new Mesh { name = "Movement track" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            }
        }
    }
}

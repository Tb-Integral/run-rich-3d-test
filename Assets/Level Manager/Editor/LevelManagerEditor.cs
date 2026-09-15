using UnityEditor;
using UnityEngine;

namespace ButchersGames
{
    [CustomEditor(typeof(LevelManager))]
    public class LevelManagerEditor : Editor
    {
        private SerializedProperty _editorMode;
        private SerializedProperty _levelList;

        private void OnEnable()
        {
            _editorMode = serializedObject.FindProperty("editorMode");
            _levelList = serializedObject.FindProperty("levels");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_editorMode, new GUIContent("Editor Mode"));
            EditorGUILayout.PropertyField(_levelList);
            serializedObject.ApplyModifiedProperties();

            var manager = (LevelManager)target;
            if (_editorMode.boolValue)
            {
                bool hasLevels = manager.Levels != null && manager.Levels.Count > 0;
                using (new EditorGUI.DisabledScope(!hasLevels))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    int number = EditorGUILayout.IntField("Current Level", manager.CurrentLevelIndex + 1);
                    if (EditorGUI.EndChangeCheck())
                    {
                        manager.SelectLevel(number - 1);
                        EditorUtility.SetDirty(manager);
                    }

                    if (GUILayout.Button("<<", GUILayout.Width(30)))
                    {
                        manager.PrevLevel();
                        EditorUtility.SetDirty(manager);
                    }
                    if (GUILayout.Button(">>", GUILayout.Width(30)))
                    {
                        manager.NextLevel();
                        EditorUtility.SetDirty(manager);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (GUILayout.Button("Reset Level Progress"))
                LevelManager.ResetProgress();
        }
    }
}

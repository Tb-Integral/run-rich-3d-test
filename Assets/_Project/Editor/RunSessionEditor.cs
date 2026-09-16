using UnityEditor;
using UnityEngine;

namespace RunRich.Editor
{
    [CustomEditor(typeof(RunSession))]
    public sealed class RunSessionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying) return;
            var session = (RunSession)target;
            EditorGUILayout.LabelField("Состояние", session.State.ToString());
            EditorGUILayout.LabelField("Проверка завершения (только Editor)", EditorStyles.boldLabel);
            if (GUILayout.Button("Начать завершение")) session.BeginFinishing();
            if (GUILayout.Button("Поражение")) session.Lose();
            if (GUILayout.Button("Заново")) session.Restart();
        }
    }
}

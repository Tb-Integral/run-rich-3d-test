using UnityEditor;
using UnityEngine;

namespace RunRich.Editor
{
    [CustomEditor(typeof(PlayerWealth))]
    public sealed class PlayerWealthEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying) return;
            var wealth = (PlayerWealth)target;
            EditorGUILayout.LabelField("Счёт", wealth.Score.ToString());
            EditorGUILayout.LabelField("Статус", wealth.Tier.label);
            var session = Object.FindFirstObjectByType<RunSession>();
            using (new EditorGUI.DisabledScope(session == null || session.State != RunSession.RunState.Running))
            {
                EditorGUILayout.LabelField("Проверка порогов (только Editor)");
                if (GUILayout.Button("Получить 20")) session.TryChangeScore(20);
                if (GUILayout.Button("Потерять 20")) session.TryChangeScore(-20);
            }
        }
    }
}

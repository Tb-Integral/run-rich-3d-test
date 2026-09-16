using UnityEditor;
using UnityEngine;

namespace RunRich.Editor
{
    [CustomEditor(typeof(PlayerPresentation))]
    public sealed class PlayerPresentationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("В Play Mode здесь доступны проверка походок, смена одежды и финальные анимации.", MessageType.Info);
                return;
            }
            var view = (PlayerPresentation)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Проверка анимаций (только Editor)", EditorStyles.boldLabel);
            float happiness = EditorGUILayout.Slider("Счастье", view.Happiness, 0, 1);
            if (!Mathf.Approximately(happiness, view.Happiness)) view.SetHappiness(happiness);
            if (GUILayout.Button("Ходьба")) view.SetWalking(true);
            if (GUILayout.Button("Ожидание")) view.SetWalking(false);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Одежда хуже")) view.SetOutfit(view.OutfitIndex - 1);
            if (GUILayout.Button("Одежда лучше")) view.SetOutfit(view.OutfitIndex + 1);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Победа — танец")) view.ShowVictory();
            if (GUILayout.Button("Поражение — Stomping")) view.ShowDefeat();
            if (GUILayout.Button("Сбросить проверку")) view.ResetPresentation();
        }
    }
}

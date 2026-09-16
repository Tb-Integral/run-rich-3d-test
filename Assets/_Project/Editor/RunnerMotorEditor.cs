using UnityEditor;
using UnityEngine;

namespace RunRich.Editor
{
    [CustomEditor(typeof(RunnerMotor))]
    public sealed class RunnerMotorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying) return;
            var motor = (RunnerMotor)target;
            EditorGUILayout.LabelField("Путь", $"{motor.Distance:F1} / {motor.Path.Length:F1}");
            if (GUILayout.Button("С начала"))
            {
                motor.ResetToStart();
                motor.BeginRun();
                if (Camera.main != null) Camera.main.GetComponent<RunnerCamera>()?.Snap();
            }
            if (GUILayout.Button(motor.IsRunning ? "Остановить" : "Продолжить"))
            {
                if (motor.IsRunning) motor.Stop(); else motor.BeginRun();
            }
        }
    }
}

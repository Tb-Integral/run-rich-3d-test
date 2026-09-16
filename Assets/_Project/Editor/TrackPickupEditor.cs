using UnityEditor;
using UnityEngine;

namespace RunRich.Editor
{
    [CustomEditor(typeof(TrackPickup)), CanEditMultipleObjects]
    public sealed class TrackPickupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();
            EditorGUILayout.HelpBox("Положение задаётся Distance вдоль трассы и Lateral Offset поперёк неё. Эти координаты используются для подбора.", MessageType.Info);
            if (!changed && !GUILayout.Button("Разместить по координатам трассы")) return;
            foreach (var item in targets)
            {
                var pickup = (TrackPickup)item;
                var track = pickup.GetComponentInParent<TrackPath>();
                if (track == null) continue;
                Undo.RecordObject(pickup.transform, "Размещён пикап");
                var pose = track.Evaluate(pickup.Distance);
                pickup.transform.SetPositionAndRotation(pose.position + pose.rotation * Vector3.right * pickup.LateralOffset, pose.rotation);
                PrefabUtility.RecordPrefabInstancePropertyModifications(pickup.transform);
            }
        }
    }
}

using UnityEngine;

namespace RunRich
{
    [CreateAssetMenu(menuName = "Run Rich/Runner Settings")]
    public sealed class RunnerSettings : ScriptableObject
    {
        [field: SerializeField, Min(0.1f)] public float ForwardSpeed { get; private set; } = 5.5f;
        [field: SerializeField, Min(0.01f)] public float DragSensitivity { get; private set; } = 1.5f;
        [field: SerializeField, Min(0)] public float EdgePadding { get; private set; } = 0.3f;
        [field: SerializeField] public Vector3 CameraOffset { get; private set; } = new(0, 1.72f, -5.8f);
        [field: SerializeField, Range(0, 1)] public float CameraLateralFollow { get; private set; } = 0.8f;
        [field: SerializeField, Min(0)] public float CameraTurnDamping { get; private set; } = 0.1f;
        [field: SerializeField] public float CameraPitch { get; private set; } = 9.4f;
    }
}

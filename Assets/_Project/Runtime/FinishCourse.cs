using UnityEngine;

namespace RunRich
{
    public sealed class FinishCourse : MonoBehaviour
    {
        [SerializeField, Min(0)] private float startDistance;
        [SerializeField] private Vector3 cameraOffset = new(0, 1.9f, -6f);
        [SerializeField] private float cameraPitch = 12.5f;
        [SerializeField, Range(20, 70)] private float cameraFieldOfView = 60;
        [SerializeField, Range(0, 1)] private float cameraLateralFollow = 0.45f;
        public float StartDistance => startDistance;
        public Vector3 CameraOffset => cameraOffset;
        public float CameraPitch => cameraPitch;
        public float CameraFieldOfView => cameraFieldOfView;
        public float CameraLateralFollow => cameraLateralFollow;
    }
}

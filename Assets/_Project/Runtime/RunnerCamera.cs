using UnityEngine;

namespace RunRich
{
    public sealed class RunnerCamera : MonoBehaviour
    {
        [SerializeField] private RunnerMotor target;
        private Quaternion _heading;
        private bool _initialized;
        private float _previousDistance;
        private TrackPath _path;
        private FinishCourse _finish;
        private Camera _camera;
        private float _baseFieldOfView;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _baseFieldOfView = _camera.fieldOfView;
        }

        private void LateUpdate() => Follow(Time.deltaTime);
        private void OnEnable() => _initialized = false;

        public void Snap() { _initialized = false; Follow(0); }

        public void Follow(float deltaTime)
        {
            if (target == null || target.Path == null || target.Settings == null) return;
            var settings = target.Settings;
            if (_path != target.Path)
            {
                _path = target.Path;
                _finish = _path.GetComponentInChildren<FinishCourse>();
            }
            Pose pose = target.TrackPose;
            Vector3 offset = settings.CameraOffset;
            float pitch = settings.CameraPitch;
            float lateralFollow = settings.CameraLateralFollow;
            float fov = _baseFieldOfView;
            if (_finish != null)
            {
                float finishBlend = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(_finish.StartDistance - 6, _finish.StartDistance + 4, target.Distance));
                offset = Vector3.Lerp(offset, _finish.CameraOffset, finishBlend);
                pitch = Mathf.Lerp(pitch, _finish.CameraPitch, finishBlend);
                lateralFollow = Mathf.Lerp(lateralFollow, _finish.CameraLateralFollow, finishBlend);
                fov = Mathf.Lerp(fov, _finish.CameraFieldOfView, finishBlend);
            }
            float lateral = target.LateralOffset * lateralFollow;
            if (!_initialized || target.Distance < _previousDistance)
            {
                _heading = pose.rotation;
                _initialized = true;
            }
            float blend = settings.CameraTurnDamping <= 0 ? 1 : 1 - Mathf.Exp(-deltaTime / settings.CameraTurnDamping);
            _heading = Quaternion.Slerp(_heading, pose.rotation, blend);
            transform.SetPositionAndRotation(pose.position + _heading * (offset + Vector3.right * lateral),
                _heading * Quaternion.Euler(pitch, 0, 0));
            if (_camera != null) _camera.fieldOfView = fov;
            _previousDistance = target.Distance;
        }
    }
}

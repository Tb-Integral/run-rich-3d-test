using UnityEngine;

namespace RunRich
{
    public sealed class RunnerCamera : MonoBehaviour
    {
        [SerializeField] private RunnerMotor target;
        private Quaternion _heading;
        private bool _initialized;
        private float _previousDistance;

        private void LateUpdate() => Follow(Time.deltaTime);
        private void OnEnable() => _initialized = false;

        public void Snap() { _initialized = false; Follow(0); }

        public void Follow(float deltaTime)
        {
            if (target == null || target.Path == null || target.Settings == null) return;
            var settings = target.Settings;
            Pose pose = target.TrackPose;
            float lateral = target.LateralOffset * settings.CameraLateralFollow;
            if (!_initialized || target.Distance < _previousDistance)
            {
                _heading = pose.rotation;
                _initialized = true;
            }
            float blend = settings.CameraTurnDamping <= 0 ? 1 : 1 - Mathf.Exp(-deltaTime / settings.CameraTurnDamping);
            _heading = Quaternion.Slerp(_heading, pose.rotation, blend);
            transform.SetPositionAndRotation(pose.position + _heading * (settings.CameraOffset + Vector3.right * lateral),
                _heading * Quaternion.Euler(settings.CameraPitch, 0, 0));
            _previousDistance = target.Distance;
        }
    }
}

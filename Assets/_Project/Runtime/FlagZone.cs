using UnityEngine;

namespace RunRich
{
    public sealed class FlagZone : TrackInteraction
    {
        [SerializeField] private Transform leftFlag;
        [SerializeField] private Transform rightFlag;
        [SerializeField, Min(0.01f)] private float raiseDuration = 0.35f;
        private Quaternion _leftDown;
        private Quaternion _rightDown;
        private float _elapsed;
        public bool IsRaised => IsTriggered && _elapsed >= raiseDuration;

        private void Awake()
        {
            _leftDown = leftFlag.localRotation;
            _rightDown = rightFlag.localRotation;
        }

        protected override bool Apply(RunSession session, WealthSettings settings, float crossingOffset) => true;

        private void Update()
        {
            if (!IsTriggered || IsRaised) return;
            _elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_elapsed / raiseDuration));
            leftFlag.localRotation = Quaternion.Slerp(_leftDown, Quaternion.Euler(0, 180, 0), progress);
            rightFlag.localRotation = Quaternion.Slerp(_rightDown, Quaternion.identity, progress);
        }
    }
}

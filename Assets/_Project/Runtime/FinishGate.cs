using UnityEngine;

namespace RunRich
{
    public sealed class FinishGate : TrackInteraction
    {
        [SerializeField, Min(1)] private int multiplier = 2;
        [SerializeField, Min(0)] private int requiredScore = 20;
        [SerializeField] private Transform leftDoor;
        [SerializeField] private Transform rightDoor;
        [SerializeField, Min(0.01f)] private float openDuration = 0.25f;
        [SerializeField] private bool isFinal;
        private Quaternion _leftClosed;
        private Quaternion _rightClosed;
        private float _elapsed;

        public int Multiplier => multiplier;
        public int RequiredScore => requiredScore;
        public bool IsOpen { get; private set; }
        public bool IsFinal => isFinal;

        private void Awake()
        {
            _leftClosed = leftDoor.localRotation;
            _rightClosed = rightDoor.localRotation;
        }

        protected override bool Apply(RunSession session, WealthSettings settings, float crossingOffset)
        {
            IsOpen = session.ResolveFinishGate(requiredScore, multiplier, Distance, crossingOffset, isFinal);
            return true;
        }

        private void Update()
        {
            if (!IsOpen) return;
            _elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_elapsed / openDuration));
            leftDoor.localRotation = _leftClosed * Quaternion.Euler(0, -100 * progress, 0);
            rightDoor.localRotation = _rightClosed * Quaternion.Euler(0, 100 * progress, 0);
        }
    }
}

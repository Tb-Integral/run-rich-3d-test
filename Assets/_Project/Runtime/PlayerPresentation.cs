using UnityEngine;

namespace RunRich
{
    public sealed class PlayerPresentation : MonoBehaviour
    {
        public enum MotionState { Idle, Walking, Victory, Defeat }

        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualPivot;
        [SerializeField] private GameObject[] outfits;
        [SerializeField, Min(0)] private float happinessDamping = 0.25f;
        [SerializeField, Min(0.01f)] private float transitionDuration = 0.15f;
        [SerializeField, Min(0.1f)] private float upgradeDuration = 0.65f;
        [SerializeField, Range(0, 1)] private float initialHappiness = 0.5f;
        [SerializeField, Min(0)] private int initialOutfit = 1;

        [SerializeField, Range(0, 60)] private float maxSteeringAngle = 25;
        [SerializeField, Min(0)] private float steeringDamping = 0.025f;
        [SerializeField, Min(0)] private float steeringReturnDamping = 0.1f;
        [SerializeField, Min(0)] private float steeringReleaseDelay = 0.08f;

        private float _targetSteeringAngle;
        private float _timeWithoutSteering;
        private float _upgradeAngle;
        public float SteeringAngle { get; private set; }

        private static readonly int HappinessParameter = Animator.StringToHash("Happiness");
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        private static readonly int WalkingState = Animator.StringToHash("Base Layer.Locomotion");
        private static readonly int VictoryState = Animator.StringToHash("Base Layer.Victory");
        private static readonly int DefeatState = Animator.StringToHash("Base Layer.Defeat");
        private static readonly int UpgradeState = Animator.StringToHash("Base Layer.Upgrade");
        private Quaternion _restRotation;
        private float _upgradeElapsed;

        public float Happiness { get; private set; }
        public int OutfitIndex { get; private set; }
        public int OutfitCount => outfits.Length;
        public bool IsUpgrading { get; private set; }
        public MotionState State { get; private set; }

        private void Awake()
        {
            _restRotation = visualPivot.localRotation;
            animator.applyRootMotion = false;
            ResetPresentation();
        }

        private void Update()
        {
            animator.SetFloat(HappinessParameter, Happiness, happinessDamping, Time.deltaTime);
            if (!IsUpgrading) return;
            _upgradeElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_upgradeElapsed / upgradeDuration);
            _upgradeAngle = Mathf.SmoothStep(0, 360, progress);
            if (progress < 1) return;
            CancelUpgrade();
            PlayState();
        }

        private void LateUpdate()
        {
            _timeWithoutSteering += Time.deltaTime;
            float targetAngle = State == MotionState.Walking && _timeWithoutSteering <= steeringReleaseDelay
                ? _targetSteeringAngle : 0;
            float damping = Mathf.Approximately(targetAngle, 0) ? steeringReturnDamping : steeringDamping;
            float blend = damping <= 0 ? 1 : 1 - Mathf.Exp(-Time.deltaTime / damping);
            SteeringAngle = Mathf.Lerp(SteeringAngle, targetAngle, blend);
            ApplyVisualRotation();
        }

        public void SetLateralMotion(float lateralSpeed)
        {
            if (State != MotionState.Walking) return;
            if (Mathf.Abs(lateralSpeed) < 0.01f) return;
            // Жест задаёт сторону, а не угол из соотношения скоростей. Нулевая выборка
            // между событиями мыши не обрывает короткий поворот на высоком FPS.
            _targetSteeringAngle = Mathf.Sign(lateralSpeed) * maxSteeringAngle;
            _timeWithoutSteering = 0;
        }

        private void ApplyVisualRotation()
        {
            // Руление и поворот при смене одежды складываются только на визуале.
            visualPivot.localRotation = _restRotation * Quaternion.Euler(0, SteeringAngle + _upgradeAngle, 0);
        }

        private void ResetSteering()
        {
            _targetSteeringAngle = 0;
            _timeWithoutSteering = float.PositiveInfinity;
            SteeringAngle = 0;
        }

        public void SetHappiness(float normalized, bool immediate = false)
        {
            Happiness = Mathf.Clamp01(normalized);
            if (immediate) animator.SetFloat(HappinessParameter, Happiness);
        }

        public void SetWalking(bool walking)
        {
            if (State == MotionState.Victory || State == MotionState.Defeat) return;
            var next = walking ? MotionState.Walking : MotionState.Idle;
            if (State == next) return;
            State = next;
            if (!walking) _targetSteeringAngle = 0;
            if (!IsUpgrading) PlayState();
        }

        public bool SetOutfit(int index, bool animateUpgrade = true)
        {
            if (index < 0 || index >= outfits.Length || State == MotionState.Victory || State == MotionState.Defeat)
                return false;
            if (index == OutfitIndex) return false;
            bool improved = index > OutfitIndex;
            bool interruptedUpgrade = IsUpgrading;
            OutfitIndex = index;
            ApplyOutfit();
            CancelUpgrade();
            if (improved && animateUpgrade)
            {
                IsUpgrading = true;
                _upgradeElapsed = 0;
                animator.CrossFadeInFixedTime(UpgradeState, transitionDuration);
            }
            else if (interruptedUpgrade) PlayState();
            return true;
        }

        public void ShowVictory() => Finish(MotionState.Victory);
        public void ShowDefeat() => Finish(MotionState.Defeat);

        public void ResetPresentation()
        {
            ResetSteering();
            CancelUpgrade();
            State = MotionState.Idle;
            Happiness = initialHappiness;
            OutfitIndex = Mathf.Clamp(initialOutfit, 0, outfits.Length - 1);
            ApplyOutfit();
            animator.SetFloat(HappinessParameter, Happiness);
            animator.Play(IdleState, 0, 0);
        }

        private void Finish(MotionState state)
        {
            if (State == MotionState.Victory || State == MotionState.Defeat) return;
            ResetSteering();
            CancelUpgrade();
            State = state;
            PlayState();
        }

        private void ApplyOutfit()
        {
            for (int i = 0; i < outfits.Length; i++) outfits[i].SetActive(i == OutfitIndex);
        }

        private void PlayState()
        {
            // При выключении объекта Animator уже может быть неактивен; OnEnable восстановит состояние.
            if (animator == null || !animator.isActiveAndEnabled) return;
            int state = State switch
            {
                MotionState.Walking => WalkingState,
                MotionState.Victory => VictoryState,
                MotionState.Defeat => DefeatState,
                _ => IdleState
            };
            animator.CrossFadeInFixedTime(state, transitionDuration);
        }

        private void CancelUpgrade()
        {
            IsUpgrading = false;
            _upgradeAngle = 0;
            ApplyVisualRotation();
        }

        private void OnDisable()
        {
            bool wasUpgrading = IsUpgrading;
            ResetSteering();
            CancelUpgrade();
            if (wasUpgrading && animator != null && animator.isActiveAndEnabled) PlayState();
        }

        private void OnEnable()
        {
            if (animator != null) PlayState();
        }
    }
}

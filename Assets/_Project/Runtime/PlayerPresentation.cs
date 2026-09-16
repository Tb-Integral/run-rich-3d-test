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
            // Поворачивается только визуал: направление движения и коллайдер не затрагиваются.
            visualPivot.localRotation = _restRotation * Quaternion.Euler(0, Mathf.SmoothStep(0, 360, progress), 0);
            if (progress < 1) return;
            CancelUpgrade();
            PlayState();
        }

        public void SetHappiness(float normalized) => Happiness = Mathf.Clamp01(normalized);

        public void SetWalking(bool walking)
        {
            if (State == MotionState.Victory || State == MotionState.Defeat) return;
            var next = walking ? MotionState.Walking : MotionState.Idle;
            if (State == next) return;
            State = next;
            if (!IsUpgrading) PlayState();
        }

        public bool SetOutfit(int index)
        {
            if (index < 0 || index >= outfits.Length || State == MotionState.Victory || State == MotionState.Defeat)
                return false;
            if (index == OutfitIndex) return false;
            bool improved = index > OutfitIndex;
            bool interruptedUpgrade = IsUpgrading;
            OutfitIndex = index;
            ApplyOutfit();
            CancelUpgrade();
            if (improved)
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
            visualPivot.localRotation = _restRotation;
        }

        private void OnDisable()
        {
            if (!IsUpgrading) return;
            CancelUpgrade();
            if (animator != null && animator.isActiveAndEnabled) PlayState();
        }

        private void OnEnable()
        {
            if (animator != null) PlayState();
        }
    }
}

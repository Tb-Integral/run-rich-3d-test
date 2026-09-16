using UnityEngine;

namespace RunRich
{
    public sealed class RunAudio : MonoBehaviour
    {
        [SerializeField] private RunSession session;
        [SerializeField] private PlayerWealth wealth;
        [SerializeField] private PlayerPresentation presentation;
        [SerializeField] private RunnerMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField] private RunAudioSettings settings;
        [SerializeField] private AudioSource steps;
        [SerializeField] private AudioSource effects;
        [SerializeField] private AudioSource result;
        [SerializeField] private AudioSource ui;
        private static readonly int WalkingState = Animator.StringToHash("Base Layer.Locomotion");
        private RunSession.RunState _state;
        private int _lastHalfStep = -1;
        private int _lastOutfit;
        private float _nextPickup;

        public RunAudioSettings Settings => settings;

        private void OnEnable()
        {
            wealth.AmountChanged += OnAmount;
            session.Changed += OnState;
            session.FinishGateOpened += OnGateOpened;
            _lastOutfit = wealth.Tier.outfitIndex;
            OnState();
        }
        private void OnDisable()
        {
            wealth.AmountChanged -= OnAmount;
            session.Changed -= OnState;
            session.FinishGateOpened -= OnGateOpened;
            StopRunSounds();
            ui.Stop();
        }
        private void Update()
        {
            if (!motor.IsRunning || session.State != RunSession.RunState.Running || presentation.IsUpgrading)
            { _lastHalfStep = -1; return; }
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != WalkingState) { _lastHalfStep = -1; return; }
            // Два контакта ног за цикл: звук следует темпу смешанной анимации, а не FPS.
            int halfStep = Mathf.FloorToInt(state.normalizedTime * 2);
            if (_lastHalfStep >= 0 && halfStep != _lastHalfStep)
            {
                var clips = presentation.OutfitIndex >= 2 ? settings.Heels : settings.Footsteps;
                if (clips != null && clips.Length > 0)
                    Play(steps, clips[halfStep % clips.Length], settings.StepVolume);
            }
            _lastHalfStep = halfStep;
        }
        private void OnAmount(int delta)
        {
            int outfit = wealth.Tier.outfitIndex;
            bool upgraded = outfit > _lastOutfit;
            _lastOutfit = outfit;
            if (upgraded) Play(effects, settings.Upgrade, settings.EffectVolume);
            else if (Time.unscaledTime >= _nextPickup)
                Play(effects, delta > 0 ? settings.Collect : settings.LoseMoney, settings.EffectVolume);
            _nextPickup = Time.unscaledTime + settings.PickupCooldown;
        }
        private void OnState()
        {
            var state = session.State;
            if (state == RunSession.RunState.Ready)
            {
                StopRunSounds();
                _lastOutfit = wealth.Tier.outfitIndex;
                _nextPickup = 0;
            }
            if (state != _state)
            {
                if (state != RunSession.RunState.Running) { steps.Stop(); _lastHalfStep = -1; }
                if (state == RunSession.RunState.Won) Play(result, settings.Victory, settings.EffectVolume);
                if (state == RunSession.RunState.Lost)
                {
                    effects.Stop();
                    Play(result, settings.Defeat, settings.EffectVolume);
                }
            }
            _state = state;
        }
        private void StopRunSounds()
        {
            steps.Stop(); effects.Stop(); result.Stop(); _lastHalfStep = -1;
        }
        public void PlayClick() => Play(ui, settings.Click, settings.EffectVolume);
        private void OnGateOpened() => Play(effects, settings.Gate, settings.EffectVolume);
        public void PlayPhotograph() => Play(effects, settings.Photograph, settings.EffectVolume);
        private void Play(AudioSource source, AudioClip clip, float volume)
        {
            if (!isActiveAndEnabled || clip == null || settings.MasterVolume <= 0) return;
            source.PlayOneShot(clip, volume * settings.MasterVolume);
        }
    }
}

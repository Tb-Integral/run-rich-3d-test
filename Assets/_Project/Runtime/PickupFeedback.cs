using UnityEngine;

namespace RunRich
{
    public sealed class PickupFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerWealth wealth;
        [SerializeField] private RunSession session;
        [SerializeField] private ParticleSystem bills;
        [SerializeField] private ParticleSystem stars;
        [SerializeField] private ParticleSystem loss;
        [SerializeField] private ParticleSystem upgradeFlash;
        [SerializeField] private ParticleSystem victoryBills;
        [SerializeField] private ParticleSystem victoryDollars;
        [SerializeField] private RunnerMotor motor;
        [SerializeField] private RunAudio audioFeedback;
        [SerializeField, Min(1)] private int billCount = 2;
        [SerializeField, Min(1)] private int starCount = 4;
        [SerializeField, Min(1)] private int lossCount = 12;
        [SerializeField, Min(1)] private int victoryBillCount = 18;
        [SerializeField, Min(1)] private int victoryDollarCount = 36;
        [SerializeField, Min(0)] private float photographDelay = 0.45f;
        private int _lastOutfit;
        private RunSession.RunState _state;
        private ParticleSystem _photograph;
        private float _photoCountdown = -1;

        private void OnEnable()
        {
            wealth.AmountChanged += Emit;
            session.Changed += RefreshState;
            _lastOutfit = wealth.Tier.outfitIndex;
            RefreshState();
        }
        private void OnDisable()
        {
            wealth.AmountChanged -= Emit;
            session.Changed -= RefreshState;
            Clear();
        }
        private void Emit(int delta)
        {
            if (delta > 0)
            {
                bills.Emit(billCount); stars.Emit(starCount);
                if (wealth.Tier.outfitIndex > _lastOutfit && upgradeFlash != null)
                { upgradeFlash.Clear(); upgradeFlash.Emit(1); stars.Emit(starCount * 3); }
            }
            else loss.Emit(lossCount);
            _lastOutfit = wealth.Tier.outfitIndex;
        }
        private void RefreshState()
        {
            if (session.State == RunSession.RunState.Ready)
            { Clear(); _lastOutfit = wealth.Tier.outfitIndex; }
            if (session.State == RunSession.RunState.Won && _state != RunSession.RunState.Won)
            {
                victoryBills?.Emit(victoryBillCount);
                victoryDollars?.Emit(victoryDollarCount);
                var course = motor != null ? motor.Path.GetComponentInChildren<FinishCourse>() : null;
                if (session.ResultMultiplier >= 4 && course != null && course.PhotographFlash != null)
                { _photograph = course.PhotographFlash; _photoCountdown = photographDelay; }
            }
            _state = session.State;
        }
        private void Update()
        {
            if (_photoCountdown < 0) return;
            _photoCountdown -= Time.deltaTime;
            if (_photoCountdown > 0) return;
            _photoCountdown = -1;
            if (session.State != RunSession.RunState.Won) return;
            if (_photograph != null) _photograph.Emit(1);
            audioFeedback?.PlayPhotograph();
        }
        private void Clear()
        {
            bills.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            stars.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            loss.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            upgradeFlash?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            victoryBills?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            victoryDollars?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_photograph != null) _photograph.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _photograph = null;
            _photoCountdown = -1;
        }
    }
}

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
        [SerializeField, Min(1)] private int billCount = 2;
        [SerializeField, Min(1)] private int starCount = 4;
        [SerializeField, Min(1)] private int lossCount = 12;

        private void OnEnable()
        {
            wealth.AmountChanged += Emit;
            session.Changed += ResetWhenReady;
        }
        private void OnDisable()
        {
            wealth.AmountChanged -= Emit;
            session.Changed -= ResetWhenReady;
            Clear();
        }
        private void Emit(int delta)
        {
            if (delta > 0) { bills.Emit(billCount); stars.Emit(starCount); }
            else loss.Emit(lossCount);
        }
        private void ResetWhenReady()
        {
            if (session.State == RunSession.RunState.Ready) Clear();
        }
        private void Clear()
        {
            bills.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            stars.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            loss.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}

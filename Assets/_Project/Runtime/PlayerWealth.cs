using System;
using UnityEngine;

namespace RunRich
{
    public sealed class PlayerWealth : MonoBehaviour
    {
        [SerializeField] private WealthSettings settings;
        [SerializeField] private PlayerPresentation presentation;

        public event Action Changed;
        public event Action<int> AmountChanged;
        public WealthSettings Settings => settings;
        public int Score { get; private set; }
        public float Normalized => (float)Score / settings.MaximumScore;
        public WealthSettings.Tier Tier => settings.TierFor(Score);

        public void ResetValue()
        {
            Score = settings.InitialScore;
            ApplyPresentation(false);
            Changed?.Invoke();
        }

        public void Change(int delta)
        {
            int before = Score;
            Score = settings.Clamp((long)Score + delta);
            if (Score == before) return;
            ApplyPresentation(true);
            Changed?.Invoke();
            AmountChanged?.Invoke(Score - before);
        }

        private void ApplyPresentation(bool animate)
        {
            presentation.SetHappiness(settings.Happiness(Score));
            presentation.SetOutfit(Tier.outfitIndex, animate);
        }
    }
}

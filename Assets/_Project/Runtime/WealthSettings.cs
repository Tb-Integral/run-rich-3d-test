using System;
using UnityEngine;

namespace RunRich
{
    [CreateAssetMenu(menuName = "Run Rich/Wealth Settings")]
    public sealed class WealthSettings : ScriptableObject
    {
        [Serializable]
        public struct Tier
        {
            [Min(0)] public int minimumScore;
            public string label;
            public Color color;
            [Min(0)] public int outfitIndex;
        }

        [SerializeField, Min(0)] private int initialScore = 40;
        [SerializeField, Min(1)] private int maximumScore = 150;
        [SerializeField, Min(1)] private int happyScore = 80;
        [SerializeField, Min(1)] private int moneyValue = 2;
        [SerializeField, Min(1)] private int alcoholPenalty = 20;
        [SerializeField] private Tier[] tiers =
        {
            new Tier { minimumScore = 0, label = "НИЩИЙ", color = new Color(1, 0.06f, 0.04f), outfitIndex = 0 },
            new Tier { minimumScore = 20, label = "БЕДНЫЙ", color = new Color(1, 0.32f, 0), outfitIndex = 1 },
            new Tier { minimumScore = 65, label = "СОСТОЯТЕЛЬНЫЙ", color = new Color(1, 0.82f, 0), outfitIndex = 2 },
            new Tier { minimumScore = 105, label = "БОГАТЫЙ", color = new Color(0.35f, 0.8f, 0), outfitIndex = 3 },
            new Tier { minimumScore = 140, label = "МИЛЛИОНЕР", color = new Color(0.05f, 0.8f, 0.64f), outfitIndex = 4 }
        };

        public int InitialScore => Clamp(initialScore);
        public int MaximumScore => Mathf.Max(1, maximumScore);
        public int MoneyValue => Mathf.Max(1, moneyValue);
        public int AlcoholPenalty => Mathf.Max(1, alcoholPenalty);
        public int TierCount => tiers.Length;
        public Tier GetTier(int index) => tiers[index];
        public int Clamp(long score) => (int)Math.Max(0, Math.Min(MaximumScore, score));
        public float Happiness(int score) => Mathf.Clamp01((float)score / Mathf.Max(1, happyScore));

        public Tier TierFor(int score)
        {
            // Выбор не зависит от порядка элементов в Inspector.
            Tier result = tiers[0];
            int highestMinimum = int.MinValue;
            foreach (var tier in tiers)
                if (tier.minimumScore <= score && tier.minimumScore >= highestMinimum)
                {
                    result = tier;
                    highestMinimum = tier.minimumScore;
                }
            return result;
        }

        private void OnValidate()
        {
            maximumScore = Mathf.Max(1, maximumScore);
            initialScore = Mathf.Clamp(initialScore, 0, maximumScore);
        }
    }
}

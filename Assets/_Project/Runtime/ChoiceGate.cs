using UnityEngine;

namespace RunRich
{
    public sealed class ChoiceGate : TrackInteraction
    {
        public enum Choice { None, Party, School }
        [SerializeField, Min(0)] private int schoolReward = 20;
        [SerializeField, Min(0)] private int partyPenalty = 20;
        [SerializeField] private GameObject partyIcon;
        [SerializeField] private GameObject schoolIcon;
        [SerializeField] private GameObject partyHighlight;
        [SerializeField] private GameObject schoolHighlight;
        public Choice Selected { get; private set; }

        protected override bool Apply(RunSession session, WealthSettings settings, float crossingOffset)
        {
            // Центр относится к правой створке: за один проход выбирается одна сторона.
            bool school = crossingOffset >= LateralOffset;
            if (!session.TryChangeScore(school ? schoolReward : -partyPenalty)) return false;
            Selected = school ? Choice.School : Choice.Party;
            (school ? schoolIcon : partyIcon).SetActive(false);
            (school ? schoolHighlight : partyHighlight).SetActive(true);
            return true;
        }
    }
}

using UnityEngine;

namespace RunRich
{
    public sealed class TrackPickup : TrackInteraction
    {
        public enum PickupKind { Money, Alcohol }
        [SerializeField] private PickupKind kind;
        [SerializeField, Min(0.01f)] private float collectionRadius = 0.48f;

        public PickupKind Kind => kind;
        public bool IsCollected => IsTriggered;

        public override bool Intersects(Vector2 from, Vector2 to)
        {
            var point = new Vector2(LateralOffset, Distance);
            Vector2 segment = to - from;
            float t = segment.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - from, segment) / segment.sqrMagnitude) : 0;
            return (point - from - segment * t).sqrMagnitude <= collectionRadius * collectionRadius;
        }

        public bool TryCollect(RunSession session, WealthSettings settings)
            => TryActivate(session, settings, LateralOffset);

        protected override bool Apply(RunSession session, WealthSettings settings, float crossingOffset)
        {
            int delta = kind == PickupKind.Money ? settings.MoneyValue : -settings.AlcoholPenalty;
            if (!session.TryChangeScore(delta)) return false;
            gameObject.SetActive(false);
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = kind == PickupKind.Money ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, collectionRadius);
        }
    }
}

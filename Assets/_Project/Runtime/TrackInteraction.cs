using UnityEngine;

namespace RunRich
{
    public abstract class TrackInteraction : MonoBehaviour
    {
        [SerializeField, Min(0)] private float distance;
        [SerializeField] private float lateralOffset;

        public float Distance => distance;
        public float LateralOffset => lateralOffset;
        public bool IsTriggered { get; private set; }

        public virtual bool Intersects(Vector2 from, Vector2 to) => from.y <= distance && to.y >= distance;

        public float CrossingOffset(Vector2 from, Vector2 to)
        {
            float progress = to.y > from.y ? Mathf.Clamp01((distance - from.y) / (to.y - from.y)) : 1;
            return Mathf.Lerp(from.x, to.x, progress);
        }

        public bool TryActivate(RunSession session, WealthSettings settings, float crossingOffset)
        {
            if (IsTriggered || !isActiveAndEnabled || !session.isActiveAndEnabled || session.State != RunSession.RunState.Running)
                return false;
            // Событие изменения счёта не должно повторно активировать тот же объект.
            IsTriggered = true;
            if (Apply(session, settings, crossingOffset)) return true;
            IsTriggered = false;
            return false;
        }

        protected abstract bool Apply(RunSession session, WealthSettings settings, float crossingOffset);
    }
}

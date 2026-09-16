using System;
using UnityEngine;

namespace RunRich
{
    public sealed class PickupCollector : MonoBehaviour
    {
        [SerializeField] private RunnerMotor motor;
        [SerializeField] private RunSession session;
        [SerializeField] private PlayerWealth wealth;
        private TrackPickup[] _pickups = Array.Empty<TrackPickup>();

        private void OnEnable() => motor.Advanced += Collect;
        private void OnDisable() => motor.Advanced -= Collect;

        public void Bind(TrackPath track)
        {
            _pickups = track.GetComponentsInChildren<TrackPickup>();
            Array.Sort(_pickups, (left, right) => left.Distance.CompareTo(right.Distance));
        }

        private void Collect(Vector2 previous, Vector2 current)
        {
            if (!session.isActiveAndEnabled || session.State != RunSession.RunState.Running) return;
            // Проверяем весь пройденный отрезок в координатах трассы: предметы
            // не пропускаются на низком FPS и на дуге между двумя кадрами.
            foreach (var pickup in _pickups)
                if (pickup != null && !pickup.IsCollected && pickup.Intersects(previous, current))
                    pickup.TryCollect(session, wealth.Settings);
        }
    }
}

using System;
using UnityEngine;

namespace RunRich
{
    public sealed class PickupCollector : MonoBehaviour
    {
        [SerializeField] private RunnerMotor motor;
        [SerializeField] private RunSession session;
        [SerializeField] private PlayerWealth wealth;
        private TrackInteraction[] _interactions = Array.Empty<TrackInteraction>();

        private void OnEnable() => motor.Advanced += Collect;
        private void OnDisable() => motor.Advanced -= Collect;

        public void Bind(TrackPath track)
        {
            _interactions = track.GetComponentsInChildren<TrackInteraction>();
            Array.Sort(_interactions, (left, right) => left.Distance.CompareTo(right.Distance));
        }

        private void Collect(Vector2 previous, Vector2 current)
        {
            if (!session.isActiveAndEnabled || session.State != RunSession.RunState.Running) return;
            // Проверяем весь пройденный отрезок в координатах трассы: предметы
            // не пропускаются на низком FPS и на дуге между двумя кадрами.
            // Ворота и пикапы обрабатываются в порядке прохождения, даже если
            // большой шаг кадра пересёк несколько разных объектов.
            foreach (var interaction in _interactions)
            {
                if (session.State != RunSession.RunState.Running) break;
                if (interaction != null && !interaction.IsTriggered && interaction.Intersects(previous, current))
                    interaction.TryActivate(session, wealth.Settings, interaction.CrossingOffset(previous, current));
            }
        }
    }
}

using System;
using UnityEngine;

namespace RunRich
{
    public sealed class TrackPath : MonoBehaviour
    {
        [Serializable]
        public struct Segment
        {
            [Min(0.01f)] public float length;
            [Range(-180, 180)] public float turnDegrees;
        }

        [SerializeField, Min(0.1f)] private float width = 4.2f;
        [SerializeField] private Segment[] segments = Array.Empty<Segment>();
        public float Width => width;
        public float Length
        {
            get
            {
                float result = 0;
                foreach (var segment in segments) result += Mathf.Max(0.01f, segment.length);
                return result;
            }
        }

        // Длина измеряется по центру трассы; дуги дают непрерывное направление на стыках.
        public Pose Evaluate(float distance)
        {
            Vector3 position = Vector3.zero;
            float yaw = 0;
            float remaining = Mathf.Clamp(distance, 0, Length);
            foreach (var segment in segments)
            {
                float length = Mathf.Max(0.01f, segment.length);
                float travel = Mathf.Min(remaining, length);
                float curvature = segment.turnDegrees * Mathf.Deg2Rad / length;
                Vector3 delta;
                if (Mathf.Abs(curvature) < 0.00001f) delta = Vector3.forward * travel;
                else
                {
                    float angle = curvature * travel;
                    delta = new Vector3((1 - Mathf.Cos(angle)) / curvature, 0, Mathf.Sin(angle) / curvature);
                }
                position += Quaternion.Euler(0, yaw, 0) * delta;
                yaw += segment.turnDegrees * travel / length;
                remaining -= travel;
                if (remaining <= 0) break;
            }
            return new Pose(transform.TransformPoint(position), transform.rotation * Quaternion.Euler(0, yaw, 0));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            var previous = Evaluate(0);
            for (float distance = 0.5f; distance < Length + 0.5f; distance += 0.5f)
            {
                var next = Evaluate(distance);
                Gizmos.DrawLine(previous.position, next.position);
                previous = next;
            }
        }
    }
}

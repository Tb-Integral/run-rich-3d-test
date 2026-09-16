using UnityEngine;
using UnityEngine.UI;

namespace RunRich
{
    // Скругление геометрией сохраняет чёткий край при любом размере Canvas.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedPanel : MaskableGraphic
    {
        [SerializeField, Min(0)] private float cornerRadius = 70;

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.height) * 0.5f);
            vertices.AddVert(rect.center, color, Vector2.zero);
            const int steps = 16;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = corner switch
                {
                    0 => new Vector2(rect.xMax - radius, rect.yMax - radius),
                    1 => new Vector2(rect.xMin + radius, rect.yMax - radius),
                    2 => new Vector2(rect.xMin + radius, rect.yMin + radius),
                    _ => new Vector2(rect.xMax - radius, rect.yMin + radius)
                };
                for (int step = 0; step <= steps; step++)
                {
                    float angle = (corner * 90 + step * 90f / steps) * Mathf.Deg2Rad;
                    vertices.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
                }
            }
            int count = 4 * (steps + 1);
            for (int index = 1; index <= count; index++)
                vertices.AddTriangle(0, index == count ? 1 : index + 1, index);
        }
    }
}

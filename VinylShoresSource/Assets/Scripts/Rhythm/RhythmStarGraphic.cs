using UnityEngine;

namespace VinylShores.Rhythm
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RhythmStarGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper helper)
        {
            helper.Clear();
            Rect rect = rectTransform.rect;
            Vector2 centre = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            helper.AddVert(centre, color, Vector2.zero);
            for (int i = 0; i < 10; i++)
            {
                float angle = (90 - i * 36) * Mathf.Deg2Rad;
                float r = radius * (i % 2 == 0 ? 1 : 0.44f);
                helper.AddVert(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r, color, Vector2.zero);
            }
            for (int i = 0; i < 10; i++) helper.AddTriangle(0, i + 1, (i + 1) % 10 + 1);
        }
    }
}

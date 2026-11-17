using UnityEngine;

namespace VinylShores.Rhythm
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RhythmShadowGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper helper)
        {
            helper.Clear();
            Rect rect = rectTransform.rect;
            Vector2 centre = rect.center;
            helper.AddVert(centre, color, Vector2.zero);
            for (int i = 0; i < 20; i++)
            {
                float angle = i * Mathf.PI * 0.1f;
                helper.AddVert(centre + new Vector2(Mathf.Cos(angle) * rect.width, Mathf.Sin(angle) * rect.height) * 0.5f, color, Vector2.zero);
            }
            for (int i = 0; i < 20; i++) helper.AddTriangle(0, i + 1, (i + 1) % 20 + 1);
        }
    }
}

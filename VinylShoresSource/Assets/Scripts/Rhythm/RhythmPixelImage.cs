using UnityEngine;
using UnityEngine.UI;

namespace VinylShores.Rhythm
{
    /// <summary>Flat outlined pixel geometry shared by arcade panels, keys and hit feedback.</summary>
    public sealed class RhythmPixelImage : Image
    {
        public enum PixelShape { Frame, Brackets, Segments, LeftArrow, RightArrow, Keycap, Sparkle }
        [SerializeField] private PixelShape shape;
        [SerializeField] private Color fill = new Color(0.09f, 0.20f, 0.29f, 1);
        // Explicit 9x7 silhouette: three-pixel shaft and a solid triangular head.
        private static readonly int[] ArrowStarts = { 3, 2, 1, 0, 1, 2, 3 };
        private static readonly int[] ArrowWidths = { 1, 2, 8, 9, 8, 2, 1 };
        private static readonly Color Ink = new Color(0.025f, 0.065f, 0.12f, 1);
        public PixelShape Shape { get => shape; set { shape = value; SetVerticesDirty(); } }
        public Color Fill { get => fill; set { fill = value; SetVerticesDirty(); } }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (r.width <= 0 || r.height <= 0) return;
            if (shape == PixelShape.Frame || shape == PixelShape.Keycap)
            {
                float corner = shape == PixelShape.Keycap ? 2 : 4;
                Stepped(mesh, r, Tint(Ink), corner);
                Stepped(mesh, Inset(r, 2), color, corner);
                Stepped(mesh, Inset(r, 6), Tint(fill), corner);
            }
            else if (shape == PixelShape.Brackets)
            {
                Brackets(mesh, r, Tint(Ink), 6);
                Brackets(mesh, Inset(r, 1), color, 3);
            }
            else if (shape == PixelShape.Segments)
            {
                for (float x = r.xMin; x < r.xMax; x += 18)
                {
                    float width = Mathf.Min(14, r.xMax - x);
                    Quad(mesh, new Rect(x, r.yMin, width, r.height), color);

                }
            }
            else if (shape == PixelShape.Sparkle)
            {
                Quad(mesh, new Rect(r.center.x - 2, r.yMin, 4, r.height), Tint(Ink));
                Quad(mesh, new Rect(r.xMin, r.center.y - 2, r.width, 4), Tint(Ink));
                Quad(mesh, new Rect(r.center.x - 1, r.yMin + 2, 2, r.height - 4), color);
                Quad(mesh, new Rect(r.xMin + 2, r.center.y - 1, r.width - 4, 2), color);
            }
            else
            {
                float unit = Mathf.Floor(Mathf.Min(r.width / 9, r.height / 7));
                if (unit < 1) return;
                Vector2 origin = r.center - new Vector2(9, 7) * (unit * 0.5f);
                for (int row = 0; row < ArrowStarts.Length; row++)
                {
                    int start = ArrowStarts[row];
                    int width = ArrowWidths[row];
                    if (shape == PixelShape.RightArrow) start = 9 - start - width;
                    Quad(mesh, new Rect(origin.x + start * unit, origin.y + row * unit,
                        width * unit, unit), color);
                }
            }
        }

        private Color Tint(Color value) => WithAlpha(value, value.a * color.a);
        private static Color WithAlpha(Color value, float alpha) { value.a = alpha; return value; }
        private static Rect Inset(Rect r, float amount) => new Rect(r.xMin + amount, r.yMin + amount,
            Mathf.Max(0, r.width - amount * 2), Mathf.Max(0, r.height - amount * 2));

        private static void Brackets(VertexHelper mesh, Rect r, Color tint, float thickness)
        {
            float length = Mathf.Min(14, r.width / 3);
            foreach (float x in new[] { r.xMin, r.xMax - length })
                foreach (float y in new[] { r.yMin, r.yMax - thickness }) Quad(mesh, new Rect(x, y, length, thickness), tint);
            foreach (float x in new[] { r.xMin, r.xMax - thickness })
                foreach (float y in new[] { r.yMin, r.yMax - length }) Quad(mesh, new Rect(x, y, thickness, length), tint);
        }

        private static void Stepped(VertexHelper mesh, Rect r, Color tint, float step)
        {
            step = Mathf.Min(step, Mathf.Min(r.width, r.height) / 6);
            Quad(mesh, new Rect(r.xMin + step * 2, r.yMin, r.width - step * 4, step), tint);
            Quad(mesh, new Rect(r.xMin + step, r.yMin + step, r.width - step * 2, step), tint);
            Quad(mesh, new Rect(r.xMin, r.yMin + step * 2, r.width, r.height - step * 4), tint);
            Quad(mesh, new Rect(r.xMin + step, r.yMax - step * 2, r.width - step * 2, step), tint);
            Quad(mesh, new Rect(r.xMin + step * 2, r.yMax - step, r.width - step * 4, step), tint);
        }

        private static void Quad(VertexHelper mesh, Rect r, Color tint)
        {
            if (r.width <= 0 || r.height <= 0) return;
            int n = mesh.currentVertCount;
            mesh.AddVert(new Vector3(r.xMin, r.yMin), tint, Vector2.zero);
            mesh.AddVert(new Vector3(r.xMin, r.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax, r.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax, r.yMin), tint, Vector2.zero);
            mesh.AddTriangle(n, n + 1, n + 2);
            mesh.AddTriangle(n, n + 2, n + 3);
        }
    }
}

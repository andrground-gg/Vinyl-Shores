using UnityEngine;

namespace VinylShores.Rhythm
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RhythmStarGraphic : UnityEngine.UI.MaskableGraphic
    {
        [SerializeField, Min(0)] private float inset;
        [SerializeField, Range(0, 2)] private int erosion;
        private const int Grid = 32;
        private static readonly bool[,] Cells = BuildCells();

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper helper)
        {
            helper.Clear();
            Rect r = rectTransform.rect;
            float size = Mathf.Max(0, Mathf.Min(r.width, r.height) - inset * 2);
            float cell = size / Grid;
            Vector2 origin = r.center - Vector2.one * size * .5f;
            for (int y = 0; y < Grid; y++)
            {
                int x = 0;
                while (x < Grid)
                {
                    if (!Solid(x, y)) { x++; continue; }
                    int start = x;
                    while (x < Grid && Solid(x, y)) x++;
                    int n = helper.currentVertCount;
                    float left = origin.x + start * cell, right = origin.x + x * cell;
                    float bottom = origin.y + y * cell, top = bottom + cell;
                    helper.AddVert(new Vector3(left, bottom), color, Vector2.zero);
                    helper.AddVert(new Vector3(left, top), color, Vector2.zero);
                    helper.AddVert(new Vector3(right, top), color, Vector2.zero);
                    helper.AddVert(new Vector3(right, bottom), color, Vector2.zero);
                    helper.AddTriangle(n, n + 1, n + 2); helper.AddTriangle(n, n + 2, n + 3);
                }
            }
        }

        private bool Solid(int x, int y)
        {
            for (int dy = -erosion; dy <= erosion; dy++)
                for (int dx = -erosion; dx <= erosion; dx++)
                {
                    int px = x + dx, py = y + dy;
                    if (px < 0 || py < 0 || px >= Grid || py >= Grid || !Cells[px, py]) return false;
                }
            return true;
        }

        private static bool[,] BuildCells()
        {
            var points = new Vector2[10];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = (90 - i * 36) * Mathf.Deg2Rad;
                points[i] = Vector2.one * .5f + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (i % 2 == 0 ? .5f : .22f);
            }
            var cells = new bool[Grid, Grid];
            for (int y = 0; y < Grid; y++)
                for (int x = 0; x < Grid; x++)
                {
                    Vector2 p = new Vector2((x + .5f) / Grid, (y + .5f) / Grid);
                    bool inside = false;
                    for (int i = 0, j = 9; i < 10; j = i++)
                        if ((points[i].y > p.y) != (points[j].y > p.y) &&
                            p.x < (points[j].x - points[i].x) * (p.y - points[i].y) / (points[j].y - points[i].y) + points[i].x)
                            inside = !inside;
                    cells[x, y] = inside;
                }
            return cells;
        }
    }
}

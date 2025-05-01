using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ultra-lightweight UI line renderer: one quad per segment.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : Graphic
{
    [SerializeField] List<Vector2> points = new();   // exposed to Inspector
    Vector2[] _pts = System.Array.Empty<Vector2>();  // fast array copy

    [Range(0.5f, 10f)] public float LineThickness = 2f;

    /* ─── public API ───────────────────────────────────────────── */

    public List<Vector2> Points
    {
        get => points;
        set
        {
            points = value;
            CacheArray();
            SetVerticesDirty();
        }
    }

    /// <summary>Call after editing the list directly.</summary>
    public void Refresh() { CacheArray(); SetVerticesDirty(); }

    /* ─── internal helpers ─────────────────────────────────────── */

    void CacheArray()
    {
        _pts = points == null ? System.Array.Empty<Vector2>() : points.ToArray();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_pts.Length < 2) return;

        float half = LineThickness * 0.5f;
        Color32 col32 = color;

        for (int i = 0; i < _pts.Length - 1; i++)
        {
            Vector2 a = _pts[i];
            Vector2 b = _pts[i + 1];
            Vector2 dir = b - a;
            if (dir.sqrMagnitude < 0.0001f) continue;          // skip degenerate
            dir.Normalize();
            Vector2 perp = new Vector2(-dir.y, dir.x) * half;

            int idx = vh.currentVertCount;
            vh.AddVert(a - perp, col32, Vector2.zero);
            vh.AddVert(a + perp, col32, Vector2.zero);
            vh.AddVert(b + perp, col32, Vector2.zero);
            vh.AddVert(b - perp, col32, Vector2.zero);

            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx + 2, idx + 3, idx);
        }
    }
}

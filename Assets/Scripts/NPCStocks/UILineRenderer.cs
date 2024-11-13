using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : Graphic
{
    [SerializeField]
    public List<Vector2> Points;

    public float LineThickness = 2f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (Points == null || Points.Count < 2)
            return;

        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        Vector2 prevPoint = Points[0];
        for (int i = 1; i < Points.Count; i++)
        {
            Vector2 currentPoint = Points[i];
            DrawLineSegment(vh, prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }

    private void DrawLineSegment(VertexHelper vh, Vector2 start, Vector2 end)
    {
        float angle = Mathf.Atan2(end.y - start.y, end.x - start.x);
        Vector2 offset = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * LineThickness / 2f;

        Vector2 v1 = start - offset;
        Vector2 v2 = start + offset;
        Vector2 v3 = end + offset;
        Vector2 v4 = end - offset;

        int index = vh.currentVertCount;

        vh.AddVert(v1, color, Vector2.zero);
        vh.AddVert(v2, color, Vector2.zero);
        vh.AddVert(v3, color, Vector2.zero);
        vh.AddVert(v4, color, Vector2.zero);

        vh.AddTriangle(index + 0, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index + 0);
    }
}

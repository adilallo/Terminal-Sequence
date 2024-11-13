using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NPCStockSceneManager : MonoBehaviour
{
    [SerializeField] private float lineThickness = 2f;

    public GoogleSheetsHandler googleSheetsHandler;

    // Reference to the RectTransform of the graph area
    public RectTransform graphArea;

    // Colors for each NPC line
    public Color[] npcColors;

    // Dictionary to store the lines for each NPC
    private Dictionary<string, GameObject> npcLines = new Dictionary<string, GameObject>();

    void Start()
    {
        if (googleSheetsHandler == null)
        {
            googleSheetsHandler = FindFirstObjectByType<GoogleSheetsHandler>();
        }

        googleSheetsHandler.OnDataRetrieved += OnDataRetrievedHandler;
        googleSheetsHandler.GetAllVideoSelections();
    }

    private void OnDataRetrievedHandler(List<GoogleSheetsHandler.VideoSelection> videoSelections)
    {
        // Process the data
        foreach (var selection in videoSelections)
        {
            int selectionCount = int.Parse(selection.SelectionCount);

            // Generate path for this NPC
            List<Vector2> path = GeneratePath(selectionCount);

            // Draw the line
            DrawLine(selection, path);
        }
    }

    private List<Vector2> GeneratePath(int selectionCount)
{
    // Start from the bottom-right corner of the graph area in normalized coordinates
    Vector2 startPosition = new Vector2(0f, 0f); // Normalized bottom-right

    List<Vector2> path = new List<Vector2>();
    Vector2 currentPosition = startPosition;
    path.Add(currentPosition);

    for (int i = 0; i < selectionCount; i++)
    {
        // Randomly change direction
        Vector2 direction = Random.insideUnitCircle.normalized;
        float stepLength = 0.1f; // Adjust as needed (normalized units)
        currentPosition += direction * stepLength;

        // Clamp positions to (0,1) range
        currentPosition.x = Mathf.Clamp01(currentPosition.x);
        currentPosition.y = Mathf.Clamp01(currentPosition.y);

        path.Add(currentPosition);
    }

    return path;
}

    private void DrawLine(GoogleSheetsHandler.VideoSelection selection, List<Vector2> path)
    {
        // Create a new GameObject for the line
        GameObject lineObj = new GameObject(selection.NPCName + "_Line");
        lineObj.transform.SetParent(graphArea, false);

        // Add the UILineRenderer component
        UILineRenderer uiLineRenderer = lineObj.AddComponent<UILineRenderer>();

        // **Adjust the RectTransform of the UILineRenderer**
        RectTransform lineRect = uiLineRenderer.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0, 0);
        lineRect.anchorMax = new Vector2(1, 1);
        lineRect.pivot = new Vector2(0, 0);
        lineRect.sizeDelta = Vector2.zero;
        lineRect.anchoredPosition = Vector2.zero;

        // Set up the UILineRenderer
        int npcIndex = int.Parse(selection.NPCIndex);
        Color lineColor = npcColors.Length > npcIndex ? npcColors[npcIndex] : Color.black;
        uiLineRenderer.color = lineColor;
        uiLineRenderer.LineThickness = lineThickness; // Adjust as needed

        // Map normalized positions to graph area local positions
        List<Vector2> mappedPath = new List<Vector2>();
        foreach (var point in path)
        {
            float x = point.x * graphArea.rect.width;
            float y = point.y * graphArea.rect.height;
            Vector2 mappedPoint = new Vector2(x, y);
            mappedPath.Add(mappedPoint);
        }

        uiLineRenderer.Points = mappedPath;

        // Store the line for future reference
        npcLines[selection.NPCName] = lineObj;
    }
}

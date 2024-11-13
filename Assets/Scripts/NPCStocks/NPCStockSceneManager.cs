using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class NPCStockSceneManager : MonoBehaviour
{
    [SerializeField] private float lineThickness = 2f;

    public GoogleSheetsHandler googleSheetsHandler;

    // Reference to the RectTransform of the graph area
    public RectTransform graphArea;

    // Colors for each NPC line
    public Color[] npcColors;

    // Add this line
    [SerializeField] private Sprite[] npcSprites; // Assign these in the Inspector

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
        List<Vector2> path = new List<Vector2>();

        // Handle cases where selectionCount is less than 2 to avoid division by zero
        if (selectionCount < 2)
        {
            selectionCount = 2;
        }

        for (int i = 0; i < selectionCount; i++)
        {
            // Evenly spaced x-values between 0 and 1
            float x = (float)i / (selectionCount - 1);

            // Random y-values between 0 and 1, or replace this with your data-driven y-values
            float y = Random.value;

            // Clamp positions to (0,1) range just to be safe
            x = Mathf.Clamp01(x);
            y = Mathf.Clamp01(y);

            path.Add(new Vector2(x, y));
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

        // Create and animate the NPC image
        CreateAndAnimateNPCImage(selection, mappedPath);
    }

    private void CreateAndAnimateNPCImage(GoogleSheetsHandler.VideoSelection selection, List<Vector2> mappedPath)
    {
        // Create a new GameObject for the NPC image
        GameObject npcImageObj = new GameObject(selection.NPCName + "_Image");
        npcImageObj.transform.SetParent(graphArea, false);

        // Add Image component
        Image npcImage = npcImageObj.AddComponent<Image>();

        // Set the sprite to the NPC's sprite
        int npcIndex = int.Parse(selection.NPCIndex);
        Sprite npcSprite = npcSprites.Length > npcIndex ? npcSprites[npcIndex] : null;
        if (npcSprite != null)
        {
            npcImage.sprite = npcSprite;
        }
        else
        {
            Debug.LogWarning($"No sprite found for NPC at index {npcIndex}");
            return;
        }

        // Set RectTransform settings
        RectTransform imageRect = npcImage.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0, 0);
        imageRect.anchorMax = new Vector2(0, 0);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.sizeDelta = new Vector2(50, 50); // Adjust size as needed

        // Initially position the image at the start of the path
        if (mappedPath.Count > 0)
        {
            imageRect.anchoredPosition = mappedPath[0];
        }

        // Start the coroutine to move the image along the path
        StartCoroutine(MoveImageAlongPath(npcImageObj, mappedPath));
    }

    private IEnumerator MoveImageAlongPath(GameObject npcImageObj, List<Vector2> path)
    {
        RectTransform imageRect = npcImageObj.GetComponent<RectTransform>();

        // Precompute cumulative lengths
        List<float> cumulativeLengths = new List<float>();
        cumulativeLengths.Add(0f);
        float totalLength = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            float segmentLength = Vector2.Distance(path[i - 1], path[i]);
            totalLength += segmentLength;
            cumulativeLengths.Add(totalLength);
        }

        float speed = totalLength / 5f; // Adjust duration (in seconds) by changing the denominator
        float t = 0f; // Parameter along the path
        float direction = 1f; // 1 for forward, -1 for backward

        while (true)
        {
            t += direction * speed * Time.deltaTime;

            // Check for bounds and reverse direction if necessary
            if (t > totalLength)
            {
                t = totalLength;
                direction = -1f;
            }
            else if (t < 0f)
            {
                t = 0f;
                direction = 1f;
            }

            // Get position along the path at distance t
            Vector2 position = GetPositionAtDistance(path, cumulativeLengths, t);
            imageRect.anchoredPosition = position;

            yield return null;
        }
    }

    private Vector2 GetPositionAtDistance(List<Vector2> path, List<float> cumulativeLengths, float t)
    {
        // Find which segment t is in
        int index = 0;
        for (int i = 1; i < cumulativeLengths.Count; i++)
        {
            if (t <= cumulativeLengths[i])
            {
                index = i - 1;
                break;
            }
        }

        // Get the segment's start and end points
        Vector2 p0 = path[index];
        Vector2 p1 = path[index + 1];

        // Compute the fraction along the segment
        float segmentLength = cumulativeLengths[index + 1] - cumulativeLengths[index];
        float segmentT = (t - cumulativeLengths[index]) / segmentLength;

        // Interpolate position
        Vector2 position = Vector2.Lerp(p0, p1, segmentT);

        return position;
    }
}

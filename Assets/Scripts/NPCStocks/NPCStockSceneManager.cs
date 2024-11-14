using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class NPCStockSceneManager : MonoBehaviour
{
    [SerializeField] private float lineThickness = 2f;
    [SerializeField] private float fadeDuration = 2f; // Duration for fade-in

    public GoogleSheetsHandler googleSheetsHandler;

    // Reference to the RectTransform of the graph area
    public RectTransform graphArea;

    // Colors for each NPC line
    public Color[] npcColors;

    [SerializeField] private Sprite[] npcSprites; // Assign these in the Inspector

    [SerializeField] private CanvasGroup uiCanvasGroup; // Assign this in the Inspector
    [SerializeField] private Material flickrImageMaterial; // Material used for Flickr images

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

        // Start fading in the UI at the start
        StartCoroutine(FadeInUI());
    }

    private IEnumerator FadeInUI()
    {
        float elapsedTime = 0f;

        // Ensure CanvasGroup starts fully transparent
        uiCanvasGroup.alpha = 0;
        flickrImageMaterial.SetFloat("_CanvasGroupAlpha", 0);

        while (elapsedTime < fadeDuration)
        {
            float alpha = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);

            uiCanvasGroup.alpha = alpha;
            flickrImageMaterial.SetFloat("_CanvasGroupAlpha", alpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure UI is fully visible after fade-in
        uiCanvasGroup.alpha = 1;
        flickrImageMaterial.SetFloat("_CanvasGroupAlpha", 1);
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

        if (selectionCount < 2)
        {
            selectionCount = 2;
        }

        for (int i = 0; i < selectionCount; i++)
        {
            float x = (float)i / (selectionCount - 1);
            float y = Random.value;

            x = Mathf.Clamp01(x);
            y = Mathf.Clamp01(y);

            path.Add(new Vector2(x, y));
        }

        return path;
    }

    private void DrawLine(GoogleSheetsHandler.VideoSelection selection, List<Vector2> path)
    {
        GameObject lineObj = new GameObject(selection.NPCName + "_Line");
        lineObj.transform.SetParent(graphArea, false);

        UILineRenderer uiLineRenderer = lineObj.AddComponent<UILineRenderer>();

        RectTransform lineRect = uiLineRenderer.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0, 0);
        lineRect.anchorMax = new Vector2(1, 1);
        lineRect.pivot = new Vector2(0, 0);
        lineRect.sizeDelta = Vector2.zero;
        lineRect.anchoredPosition = Vector2.zero;

        int npcIndex = int.Parse(selection.NPCIndex);
        Color lineColor = npcColors.Length > npcIndex ? npcColors[npcIndex] : Color.black;
        uiLineRenderer.color = lineColor;
        uiLineRenderer.LineThickness = lineThickness;

        List<Vector2> mappedPath = new List<Vector2>();
        foreach (var point in path)
        {
            float x = point.x * graphArea.rect.width;
            float y = point.y * graphArea.rect.height;
            Vector2 mappedPoint = new Vector2(x, y);
            mappedPath.Add(mappedPoint);
        }

        uiLineRenderer.Points = mappedPath;

        npcLines[selection.NPCName] = lineObj;

        CreateAndAnimateNPCImage(selection, mappedPath);
    }

    private void CreateAndAnimateNPCImage(GoogleSheetsHandler.VideoSelection selection, List<Vector2> mappedPath)
    {
        GameObject npcImageObj = new GameObject(selection.NPCName + "_Image");
        npcImageObj.transform.SetParent(graphArea, false);

        Image npcImage = npcImageObj.AddComponent<Image>();

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

        RectTransform imageRect = npcImage.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0, 0);
        imageRect.anchorMax = new Vector2(0, 0);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.sizeDelta = new Vector2(64, 64);

        if (mappedPath.Count > 0)
        {
            imageRect.anchoredPosition = mappedPath[0];
        }

        StartCoroutine(MoveImageAlongPath(npcImageObj, mappedPath));
    }

    private IEnumerator MoveImageAlongPath(GameObject npcImageObj, List<Vector2> path)
    {
        RectTransform imageRect = npcImageObj.GetComponent<RectTransform>();

        List<float> cumulativeLengths = new List<float> { 0f };
        float totalLength = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            float segmentLength = Vector2.Distance(path[i - 1], path[i]);
            totalLength += segmentLength;
            cumulativeLengths.Add(totalLength);
        }

        float speed = totalLength / 15f;
        float t = 0f;
        float direction = 1f;

        while (true)
        {
            t += direction * speed * Time.deltaTime;

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

            Vector2 position = GetPositionAtDistance(path, cumulativeLengths, t);
            imageRect.anchoredPosition = position;

            yield return null;
        }
    }

    private Vector2 GetPositionAtDistance(List<Vector2> path, List<float> cumulativeLengths, float t)
    {
        int index = 0;
        for (int i = 1; i < cumulativeLengths.Count; i++)
        {
            if (t <= cumulativeLengths[i])
            {
                index = i - 1;
                break;
            }
        }

        Vector2 p0 = path[index];
        Vector2 p1 = path[index + 1];

        float segmentLength = cumulativeLengths[index + 1] - cumulativeLengths[index];
        float segmentT = (t - cumulativeLengths[index]) / segmentLength;

        return Vector2.Lerp(p0, p1, segmentT);
    }
}
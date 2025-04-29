using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utility;

public class NPCStockSceneManager : MonoBehaviour
{
    [Header("Pooling / References")]
    [SerializeField] private PoolManager pool;            // drag PoolManager here
    [SerializeField] private RectTransform graphArea;     // the parent for lines & images

    [Header("Visual Settings")]
    [SerializeField] private float lineThickness = 2f;
    [SerializeField] private Sprite[] npcSprites;
    [SerializeField] private Color[] npcColors;

    [Header("UI Fading")]
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private Material uiMaterial;          // material that exposes _CanvasGroupAlpha
    [SerializeField] private Material flickrImageMaterial; // secondary fade material
    [SerializeField] private float fadeDuration = 2f;

    private GoogleSheetsHandler googleSheetsHandler;
    private SceneChanger sceneChanger;

    // Internal pooled instances alive in current graph
    private readonly List<GameObject> _liveLines = new();
    private readonly List<GameObject> _liveImages = new();

    // Sprite mover state struct
    private struct Mover
    {
        public RectTransform rect;
        public List<Vector2> path;
        public List<float> cum;
        public float t;
        public float dir;
        public float total;
    }
    private readonly List<Mover> movers = new();

    // --------------------------------------------------
    // Life?cycle
    // --------------------------------------------------
    private IEnumerator Start()
    {
        while (googleSheetsHandler == null)
        {
            googleSheetsHandler = FindFirstObjectByType<GoogleSheetsHandler>();
            yield return null;
        }
        sceneChanger = FindFirstObjectByType<SceneChanger>();

        googleSheetsHandler.OnDataRetrieved += OnDataRetrievedHandler;
        googleSheetsHandler.GetAllVideoSelections();

        uiCanvasGroup.interactable = false;
        StartCoroutine(FadeCanvas(0f, 1f, fadeDuration, () => uiCanvasGroup.interactable = true));
    }

    private void OnDestroy()
    {
        if (googleSheetsHandler != null)
            googleSheetsHandler.OnDataRetrieved -= OnDataRetrievedHandler;
    }

    // --------------------------------------------------
    // UI Fade helpers
    // --------------------------------------------------
    private IEnumerator FadeCanvas(float from, float to, float dur, System.Action onComplete = null)
    {
        float t = 0f;
        while (t < dur)
        {
            float a = Mathf.Lerp(from, to, t / dur);
            uiCanvasGroup.alpha = a;
            uiMaterial.SetFloat("_CanvasGroupAlpha", a);
            flickrImageMaterial.SetFloat("_CanvasGroupAlpha", a);
            t += Time.deltaTime;
            yield return null;
        }
        uiCanvasGroup.alpha = to;
        uiMaterial.SetFloat("_CanvasGroupAlpha", to);
        flickrImageMaterial.SetFloat("_CanvasGroupAlpha", to);
        onComplete?.Invoke();
    }

    public void OnBackButton()
    {
        uiCanvasGroup.interactable = false;
        StartCoroutine(FadeCanvas(1f, 0f, fadeDuration, () => sceneChanger.LoadLobbySceneWithoutFade()));
    }

    // --------------------------------------------------
    // Google?sheet callback
    // --------------------------------------------------
    private void OnDataRetrievedHandler(List<GoogleSheetsHandler.VideoSelection> data)
    {
        ClearGraph();
        if (data == null || data.Count == 0)
        {
            Debug.LogWarning("NPCStockSceneManager: No data received.");
            return;
        }

        foreach (var sel in data)
        {
            int count = int.TryParse(sel.SelectionCount, out var n) ? n : 2;
            if (count < 2) count = 2;

            List<Vector2> path = GeneratePath(count);
            DrawLineAndSprite(sel, path);
        }
    }

    // --------------------------------------------------
    // Graph building helpers
    // --------------------------------------------------
    private void ClearGraph()
    {
        foreach (var go in _liveLines) pool.Return("Line", go);
        foreach (var go in _liveImages) pool.Return("NPCImage", go);
        _liveLines.Clear();
        _liveImages.Clear();
        movers.Clear();
    }

    private List<Vector2> GeneratePath(int steps)
    {
        var list = new List<Vector2>(steps);
        for (int i = 0; i < steps; ++i)
        {
            float x = (float)i / (steps - 1);
            float y = Random.value;
            list.Add(new Vector2(x, y));
        }
        return list;
    }

    private void DrawLineAndSprite(GoogleSheetsHandler.VideoSelection sel, List<Vector2> path)
    {
        // -------- line ------------------------------------------------
        GameObject lineObj = pool.Get("Line", graphArea);
        _liveLines.Add(lineObj);
#if UNITY_UI_EXTENSIONS
        var lr = lineObj.GetComponent<UILineRenderer>();
        if (lr == null) lr = lineObj.AddComponent<UILineRenderer>();
#else
        var lr = lineObj.GetComponent<UILineRenderer>();
#endif
        int idx = int.Parse(sel.NPCIndex);
        lr.color = npcColors.Length > idx ? npcColors[idx] : Color.black;
        lr.LineThickness = lineThickness;

        // map path into pixel space
        var localPoints = new List<Vector2>(path.Count);
        float w = graphArea.rect.width;
        float h = graphArea.rect.height;
        for (int i = 0; i < path.Count; i++)
            localPoints.Add(new Vector2(path[i].x * w, path[i].y * h));

        lr.Points = localPoints;        // unique list per line
        lr.SetAllDirty();

        // -------- sprite ----------------------------------------------
        GameObject imgObj = pool.Get("NPCImage", graphArea);
        _liveImages.Add(imgObj);
        var img = imgObj.GetComponent<Image>();
        if (img == null) img = imgObj.AddComponent<Image>();
        img.sprite = npcSprites.Length > idx ? npcSprites[idx] : null;

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(64, 64);
        rt.anchoredPosition = localPoints[0];

        BuildMover(rt, localPoints);
    }

    private void BuildMover(RectTransform rt, List<Vector2> mappedPath)
    {
        // cumulative length table
        var cum = new List<float>(mappedPath.Count);
        float total = 0f;
        cum.Add(0f);
        for (int i = 1; i < mappedPath.Count; i++)
        {
            total += Vector2.Distance(mappedPath[i - 1], mappedPath[i]);
            cum.Add(total);
        }
        movers.Add(new Mover
        {
            rect = rt,
            path = new List<Vector2>(mappedPath),
            cum = cum,
            t = 0f,
            dir = 1f,
            total = total
        });
    }

    // --------------------------------------------------
    // Single Update loop animating all sprites
    // --------------------------------------------------
    private void Update()
    {
        if (movers.Count == 0) return;
        float dt = Time.deltaTime;
        for (int i = 0; i < movers.Count; i++)
        {
            var m = movers[i];
            m.t += m.dir * (m.total / 15f) * dt;
            if (m.t > m.total) { m.t = m.total; m.dir = -1f; }
            else if (m.t < 0f) { m.t = 0f; m.dir = 1f; }
            m.rect.anchoredPosition = GetPositionAtDistance(m.path, m.cum, m.t);
            movers[i] = m; // copy back because struct
        }
    }

    private static Vector2 GetPositionAtDistance(List<Vector2> path, List<float> cum, float t)
    {
        int seg = 0;
        while (seg < cum.Count - 1 && t > cum[seg + 1]) seg++;
        float segLen = cum[seg + 1] - cum[seg];
        float segT = (t - cum[seg]) / segLen;
        return Vector2.Lerp(path[seg], path[seg + 1], segT);
    }
}
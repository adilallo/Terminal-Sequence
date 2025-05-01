using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utility;

public class NPCStockSceneManager : MonoBehaviour
{
    /* ─── Inspector ───────────────────────────────────────────────── */

    [Header("Pooling / References")]
    [SerializeField] PoolManager pool;
    [SerializeField] RectTransform graphArea;

    [Header("Visual")]
    [SerializeField] float lineThickness = 2f;
    [SerializeField] Sprite[] npcSprites;
    [SerializeField] Color[] npcColors;

    [Header("UI Fade")]
    [SerializeField] CanvasGroup uiCanvasGroup;
    [SerializeField] Material uiMaterial;
    [SerializeField] Material flickrMaterial;
    [SerializeField] float fadeDuration = 2f;

    /* ─── runtime refs ────────────────────────────────────────────── */

    GoogleSheetsHandler sheets;
    SceneChanger sceneChanger;

    readonly List<GameObject> liveLines = new();
    readonly List<GameObject> liveImages = new();

    /* batched materials for SetFloat */
    Material[] fadeMats;

    /* ─── mover class (no per-frame copies) ───────────────────────── */

    sealed class Mover
    {
        public RectTransform rect;
        public Vector2[] path;
        public float[] cum;
        public float t, dir, total, speed;
        public int seg;          // current segment index

        public void Tick(float dt)
        {
            t += dir * speed * dt;

            if (t > total) { t = total; dir = -1f; }
            else if (t < 0f) { t = 0f; dir = 1f; }

            // advance / rewind seg so cum[seg] <= t <= cum[seg+1]
            while (seg < cum.Length - 2 && t > cum[seg + 1]) seg++;
            while (seg > 0 && t < cum[seg]) seg--;

            float segLen = cum[seg + 1] - cum[seg];
            float u = (t - cum[seg]) / segLen;
            rect.anchoredPosition = Vector2.Lerp(path[seg], path[seg + 1], u);
        }
    }
    readonly List<Mover> movers = new();

    /* ─── start / destroy ─────────────────────────────────────────── */

    void Start()
    {
        sheets = FindObjectOfType<GoogleSheetsHandler>();
        sceneChanger = FindObjectOfType<SceneChanger>();

        if (sheets != null)
        {
            sheets.OnDataRetrieved += OnDataRetrieved;
            sheets.GetAllVideoSelections();
        }
        else Debug.LogError("GoogleSheetsHandler not found!");

        fadeMats = new[] { uiMaterial, flickrMaterial };
        uiCanvasGroup.interactable = false;
        StartCoroutine(FadeCanvas(0f, 1f, fadeDuration,
            () => uiCanvasGroup.interactable = true));
    }

    void OnDestroy()
    {
        if (sheets != null)
            sheets.OnDataRetrieved -= OnDataRetrieved;
    }

    /* ─── UI fade ─────────────────────────────────────────────────── */

    IEnumerator FadeCanvas(float from, float to, float dur, System.Action done = null)
    {
        float t = 0;
        while (t < dur)
        {
            float a = Mathf.Lerp(from, to, t / dur);
            uiCanvasGroup.alpha = a;
            for (int i = 0; i < fadeMats.Length; i++) fadeMats[i]?.SetFloat("_CanvasGroupAlpha", a);
            t += Time.deltaTime; yield return null;
        }
        uiCanvasGroup.alpha = to;
        for (int i = 0; i < fadeMats.Length; i++) fadeMats[i]?.SetFloat("_CanvasGroupAlpha", to);
        done?.Invoke();
    }

    public void OnBackButton()
    {
        uiCanvasGroup.interactable = false;
        StartCoroutine(FadeCanvas(1f, 0f, fadeDuration,
            () => sceneChanger.LoadLobbySceneWithoutFade()));
    }

    /* ─── sheet callback ─────────────────────────────────────────── */

    void OnDataRetrieved(List<GoogleSheetsHandler.VideoSelection> data)
    {
        ClearGraph();
        if (data == null || data.Count == 0) { Debug.LogWarning("No data"); return; }

        foreach (var sel in data)
        {
            int steps = Mathf.Max(int.Parse(sel.SelectionCount), 2);
            Vector2[] path = BuildPath(steps);
            DrawLineAndSprite(sel, path);
        }
    }

    /* ─── graph helpers ──────────────────────────────────────────── */

    void ClearGraph()
    {
        foreach (var g in liveLines) pool.Return("Line", g);
        foreach (var g in liveImages) pool.Return("NPCImage", g);
        liveLines.Clear(); liveImages.Clear(); movers.Clear();
    }

    static Vector2[] BuildPath(int steps)
    {
        var p = new Vector2[steps];
        for (int i = 0; i < steps; i++)
            p[i] = new Vector2((float)i / (steps - 1), Random.value);
        return p;
    }

    void DrawLineAndSprite(GoogleSheetsHandler.VideoSelection sel, Vector2[] path01)
    {
        int idx = int.Parse(sel.NPCIndex);

        /* line */
        var lineObj = pool.Get("Line", graphArea);
        liveLines.Add(lineObj);
        var lr = lineObj.GetComponent<UILineRenderer>();
        lr.color = npcColors.Length > idx ? npcColors[idx] : Color.black;
        lr.LineThickness = lineThickness;

        float w = graphArea.rect.width, h = graphArea.rect.height;
        var ptsArr = new Vector2[path01.Length];
        for (int i = 0; i < ptsArr.Length; i++) ptsArr[i] = new Vector2(path01[i].x * w, path01[i].y * h);
        lr.Points = new List<Vector2>(ptsArr);        // ✅ List for UILineRenderer
        lr.SetAllDirty();

        /* sprite */
        var imgObj = pool.Get("NPCImage", graphArea);
        liveImages.Add(imgObj);
        var img = imgObj.GetComponent<Image>();
        img.sprite = npcSprites.Length > idx ? npcSprites[idx] : null;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(.5f, .5f);
        rt.sizeDelta = new Vector2(64, 64);
        rt.anchoredPosition = ptsArr[0];

        AddMover(rt, ptsArr);
    }

    void AddMover(RectTransform rt, Vector2[] pts)
    {
        int n = pts.Length;
        var cum = new float[n];
        float tot = 0f;
        cum[0] = 0f;
        for (int i = 1; i < n; i++) { tot += Vector2.Distance(pts[i - 1], pts[i]); cum[i] = tot; }

        movers.Add(new Mover
        {
            rect = rt,
            path = pts,
            cum = cum,
            t = 0f,
            dir = 1f,
            total = tot,
            speed = tot / 15f,
            seg = 0
        });
    }

    /* ─── Update – drive all movers ───────────────────────────────── */

    void Update()
    {
        float dt = Time.deltaTime;
        foreach (var m in movers) m.Tick(dt);
    }
}

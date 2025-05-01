using UnityEngine;
using UnityEngine.UI;

public class CustomCursor : MonoBehaviour
{
    [SerializeField] RawImage cursorImage;

    RectTransform parentRect;
    Vector3 lastMousePos;
    bool cursorConfined = true;

    void Awake()
    {
        parentRect = cursorImage.rectTransform.parent as RectTransform;
        Cursor.visible = false;
        cursorImage.enabled = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    void Update()
    {
        /* ── show image on first movement ───────────────────────── */
        if (!cursorImage.enabled)
        {
            if (Input.GetAxisRaw("Mouse X") != 0f || Input.GetAxisRaw("Mouse Y") != 0f)
                cursorImage.enabled = true;
        }

        /* ── move image only if mouse moved ─────────────────────── */
        Vector3 mPos = Input.mousePosition;
        if (mPos != lastMousePos && cursorConfined)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, mPos, null, out Vector2 local);
            cursorImage.rectTransform.localPosition = local;
            lastMousePos = mPos;
        }

        /* ── toggle confinement with ESC ────────────────────────── */
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (cursorConfined) ReleaseCursor();
            else ConstrainCursor();
        }

        /* ── click inside window re-confines ────────────────────── */
        if (!cursorConfined && Input.GetMouseButtonDown(0))
            ConstrainCursor();
    }

    /* ── helper methods ─────────────────────────────────────────── */
    void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorImage.enabled = false;
        cursorConfined = false;
    }

    void ConstrainCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        cursorImage.enabled = true;
        cursorConfined = true;
    }
}

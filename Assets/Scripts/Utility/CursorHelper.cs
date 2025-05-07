using UnityEngine;
using UnityEngine.UI;

public class CursorHelper : MonoBehaviour
{
    bool cursorConfined = true;

    void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    void Update()
    {
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
        cursorConfined = false;
    }

    void ConstrainCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        cursorConfined = true;
    }
}

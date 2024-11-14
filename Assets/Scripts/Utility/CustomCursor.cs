using UnityEngine;
using UnityEngine.UI;

public class CustomCursor : MonoBehaviour
{
    [SerializeField] private RawImage cursorImage;
    private bool mouseMoved = false;
    private bool cursorConfined = true;

    void Start()
    {
        Cursor.visible = false;
        cursorImage.enabled = false;

        // Start with the cursor confined within the game window
        Cursor.lockState = CursorLockMode.Confined;
    }

    void Update()
    {
        // If mouse has moved, enable the custom cursor image
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            if (!mouseMoved)
            {
                mouseMoved = true;
                cursorImage.enabled = true;
            }
        }

        // Handle custom cursor movement based on mouse input
        if (cursorImage != null && cursorConfined)
        {
            Vector2 cursorPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)cursorImage.transform.parent,
                Input.mousePosition,
                null,
                out cursorPosition
            );
            cursorImage.rectTransform.localPosition = cursorPosition;
        }

        // Toggle cursor confinement when ESC is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorConfinement();
        }

        // Re-confine the cursor when clicking inside the game window
        if (Input.GetMouseButtonDown(0) && !cursorConfined)
        {
            ConstrainCursor();
        }
    }

    private void ToggleCursorConfinement()
    {
        if (cursorConfined)
        {
            // Release the cursor so it can move freely outside the game window
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cursorImage.enabled = false;
            cursorConfined = false;
        }
        else
        {
            // Confine the cursor back within the game window
            ConstrainCursor();
        }
    }

    private void ConstrainCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        cursorImage.enabled = true;
        cursorConfined = true;
    }
}

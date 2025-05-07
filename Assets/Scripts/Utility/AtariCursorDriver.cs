using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform), typeof(RawImage))]
public class AtariCursorDriver : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] InputActionAsset actions;
    [SerializeField] string mapName = "Atari";
    [SerializeField] string moveAction = "Move";
    [SerializeField] string fireAction = "Fire";

    [Header("Tuning")]
    [SerializeField] float speed = 900f;          // pixels/sec at full tilt

    // Cached refs (all on the same hierarchy branch)
    RectTransform rt;
    Canvas canvas;
    GraphicRaycaster raycaster;
    EventSystem eventSystem;

    // Input actions
    InputAction moveA;
    InputAction fireA;

    // State
    Vector2 pos;
    static readonly List<RaycastResult> hits = new(8);
    public static Vector2 ScreenCursorPos { get; private set; }

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        raycaster = canvas.GetComponent<GraphicRaycaster>();
        eventSystem = EventSystem.current;

        var map = actions.FindActionMap(mapName, true);
        moveA = map.FindAction(moveAction, true);
        fireA = map.FindAction(fireAction, true);

        // start in centre of screen
        pos = new Vector2(Screen.width, Screen.height) * 0.5f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, pos, null, out var local);
        rt.anchoredPosition = local;
    }

    void OnEnable()
    {
        moveA.actionMap.Enable();
        fireA.performed += OnFire;
    }

    void OnDisable()
    {
        fireA.performed -= OnFire;
        moveA?.actionMap.Disable();
    }

    void Update()
    {
        Vector2 delta = moveA.ReadValue<Vector2>() * speed * Time.deltaTime;
        if (delta.sqrMagnitude < 0.0001f) return;          // stick idle

        pos += delta;
        pos.x = Mathf.Clamp(pos.x, 0, Screen.width);
        pos.y = Mathf.Clamp(pos.y, 0, Screen.height);

        ScreenCursorPos = pos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, pos, null, out var local);
        rt.anchoredPosition = local;
    }

    void OnFire(InputAction.CallbackContext _)
    {
        if (raycaster == null || eventSystem == null) return;

        var ped = new PointerEventData(eventSystem) { position = pos };
        hits.Clear();
        raycaster.Raycast(ped, hits);
        if (hits.Count == 0) return;

        var go = hits[0].gameObject;
        ExecuteEvents.ExecuteHierarchy(go, ped, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.ExecuteHierarchy(go, ped, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.ExecuteHierarchy(go, ped, ExecuteEvents.pointerClickHandler);
    }
}

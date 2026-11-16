using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Maps gameplay-camera ray hits on the phone display quad to uGUI pointer events.
/// UV space matches the RT / display quad (origin bottom-left).
/// </summary>
[DisallowMultipleComponent]
public class PhoneScreenInputBridge : MonoBehaviour
{
    private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Camera uiCamera;
    [SerializeField] private Canvas phoneCanvas;
    [SerializeField] private PhoneScreenPresenter screenPresenter;
    [SerializeField] private float maxRayDistance = 5f;

    private GraphicRaycaster _graphicRaycaster;
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(16);
    private PointerEventData _pointerData;
    private GameObject _currentHover;
    private GameObject _pressTarget;
    private bool _isPointerDown;
    private Vector2 _uiPixelPos;
    private Vector2 _lastUiPixelPos;
    private bool _bridgeEnabled;

    public void Configure(
        Camera gameplayCam,
        Camera uiCam,
        Canvas canvas,
        PhoneScreenPresenter presenter)
    {
        gameplayCamera = gameplayCam;
        uiCamera = uiCam;
        phoneCanvas = canvas;
        screenPresenter = presenter;
        _graphicRaycaster = canvas != null ? canvas.GetComponent<GraphicRaycaster>() : null;
        if (_graphicRaycaster != null)
            _graphicRaycaster.ignoreReversedGraphics = false;
    }

    // Back-compat for older builder wiring.
    public void Configure(Camera gameplayCam, Camera uiCam, Canvas canvas, Collider screen)
    {
        var presenter = screen != null
            ? screen.GetComponent<PhoneScreenPresenter>() ?? screen.GetComponentInParent<PhoneScreenPresenter>()
            : null;
        Configure(gameplayCam, uiCam, canvas, presenter);
    }

    public void SetBridgeEnabled(bool enabled)
    {
        if (_bridgeEnabled == enabled)
            return;

        _bridgeEnabled = enabled;
        if (!enabled)
            ReleasePointer();
    }

    private void Update()
    {
        if (!_bridgeEnabled || gameplayCamera == null || uiCamera == null || phoneCanvas == null)
            return;

        if (EventSystem.current == null || Mouse.current == null)
            return;

        if (_pointerData == null)
            _pointerData = new PointerEventData(EventSystem.current);

        bool overScreen = TryGetUiPixelPosition(out _uiPixelPos);
        UpdatePointer(overScreen);
        _lastUiPixelPos = overScreen ? _uiPixelPos : _lastUiPixelPos;
    }

    private bool TryGetUiPixelPosition(out Vector2 uiPixelPos)
    {
        uiPixelPos = default;
        if (screenPresenter == null)
            return false;

        Collider displayCol = screenPresenter.DisplayCollider;
        if (displayCol == null)
            return false;

        Vector2 mouse = Mouse.current.position.ReadValue();
        Ray ray = gameplayCamera.ScreenPointToRay(mouse);
        int hitCount = Physics.RaycastNonAlloc(ray, HitBuffer, maxRayDistance, ~0, QueryTriggerInteraction.Ignore);
        if (hitCount <= 0)
            return false;

        RaycastHit? screenHit = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = HitBuffer[i];
            if (hit.collider != displayCol)
                continue;
            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                screenHit = hit;
            }
        }

        if (screenHit == null)
            return false;

        if (!screenPresenter.TryWorldPointToUv(screenHit.Value.point, out Vector2 uv))
            return false;

        // Map UV → RT pixels (not Game View). Origin bottom-left, matching uGUI.
        RenderTexture target = uiCamera.targetTexture;
        float w = target != null ? target.width : Mathf.Max(1, uiCamera.pixelWidth);
        float h = target != null ? target.height : Mathf.Max(1, uiCamera.pixelHeight);
        uiPixelPos = new Vector2(uv.x * w, uv.y * h);
        return true;
    }

    private void UpdatePointer(bool overScreen)
    {
        Vector2 pos = overScreen ? _uiPixelPos : new Vector2(-10000f, -10000f);
        Vector2 delta = overScreen ? _uiPixelPos - _lastUiPixelPos : Vector2.zero;

        _pointerData.Reset();
        _pointerData.position = pos;
        _pointerData.delta = delta;
        _pointerData.button = PointerEventData.InputButton.Left;
        _pointerData.scrollDelta = Mouse.current.scroll.ReadValue() * 0.1f;

        _raycastResults.Clear();
        if (overScreen)
        {
            if (_graphicRaycaster == null && phoneCanvas != null)
                _graphicRaycaster = phoneCanvas.GetComponent<GraphicRaycaster>();

            if (_graphicRaycaster != null)
                _graphicRaycaster.Raycast(_pointerData, _raycastResults);
            else
            {
                EventSystem.current.RaycastAll(_pointerData, _raycastResults);
                FilterToPhoneCanvas(_raycastResults);
            }
        }

        GameObject newHover = _raycastResults.Count > 0 ? _raycastResults[0].gameObject : null;
        HandleHover(newHover);

        bool pressed = Mouse.current.leftButton.isPressed;
        bool pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
        bool releasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;

        if (pressedThisFrame && overScreen && newHover != null)
        {
            _isPointerDown = true;
            _pressTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(newHover);
            if (_pressTarget == null)
                _pressTarget = newHover;

            _pointerData.pressPosition = pos;
            _pointerData.pointerPressRaycast = _raycastResults[0];
            _pointerData.pointerPress = ExecuteEvents.ExecuteHierarchy(newHover, _pointerData, ExecuteEvents.pointerDownHandler);
            _pointerData.rawPointerPress = newHover;

            GameObject dragHandler = ExecuteEvents.GetEventHandler<IBeginDragHandler>(newHover);
            if (dragHandler != null)
            {
                _pointerData.pointerDrag = dragHandler;
                ExecuteEvents.Execute(dragHandler, _pointerData, ExecuteEvents.beginDragHandler);
            }
        }

        if (_isPointerDown && pressed && _pointerData.pointerDrag != null)
        {
            _pointerData.dragging = true;
            ExecuteEvents.Execute(_pointerData.pointerDrag, _pointerData, ExecuteEvents.dragHandler);
        }

        if (releasedThisFrame && _isPointerDown)
        {
            if (_pointerData.pointerDrag != null)
            {
                ExecuteEvents.Execute(_pointerData.pointerDrag, _pointerData, ExecuteEvents.endDragHandler);
                if (newHover != null)
                    ExecuteEvents.ExecuteHierarchy(newHover, _pointerData, ExecuteEvents.dropHandler);
            }

            if (_pointerData.pointerPress != null)
                ExecuteEvents.Execute(_pointerData.pointerPress, _pointerData, ExecuteEvents.pointerUpHandler);
            else if (_pressTarget != null)
                ExecuteEvents.Execute(_pressTarget, _pointerData, ExecuteEvents.pointerUpHandler);

            GameObject clickHandler = _pressTarget;
            if (clickHandler != null && (newHover == clickHandler || IsChildOf(newHover, clickHandler) ||
                                         (newHover != null && ExecuteEvents.GetEventHandler<IPointerClickHandler>(newHover) == clickHandler)))
            {
                ExecuteEvents.Execute(clickHandler, _pointerData, ExecuteEvents.pointerClickHandler);
            }

            _pointerData.pointerDrag = null;
            _pointerData.dragging = false;
            _pointerData.pointerPress = null;
            _pressTarget = null;
            _isPointerDown = false;
        }

        if (overScreen && newHover != null && _pointerData.scrollDelta.sqrMagnitude > 0.0001f)
        {
            GameObject scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(newHover);
            if (scrollHandler != null)
                ExecuteEvents.ExecuteHierarchy(scrollHandler, _pointerData, ExecuteEvents.scrollHandler);
        }
    }

    private void HandleHover(GameObject newHover)
    {
        if (_currentHover == newHover)
            return;

        if (_currentHover != null)
            ExecuteEvents.ExecuteHierarchy(_currentHover, _pointerData, ExecuteEvents.pointerExitHandler);

        _currentHover = newHover;

        if (_currentHover != null)
            ExecuteEvents.ExecuteHierarchy(_currentHover, _pointerData, ExecuteEvents.pointerEnterHandler);
    }

    private void FilterToPhoneCanvas(List<RaycastResult> results)
    {
        for (int i = results.Count - 1; i >= 0; i--)
        {
            if (results[i].gameObject.GetComponentInParent<Canvas>() != phoneCanvas)
                results.RemoveAt(i);
        }
    }

    private static bool IsChildOf(GameObject child, GameObject parent)
    {
        if (child == null || parent == null)
            return false;
        return child == parent || child.transform.IsChildOf(parent.transform);
    }

    private void ReleasePointer()
    {
        if (_pointerData == null && EventSystem.current != null)
            _pointerData = new PointerEventData(EventSystem.current);

        if (_pointerData != null && _isPointerDown)
        {
            if (_pointerData.pointerDrag != null)
                ExecuteEvents.Execute(_pointerData.pointerDrag, _pointerData, ExecuteEvents.endDragHandler);
            if (_pressTarget != null)
                ExecuteEvents.Execute(_pressTarget, _pointerData, ExecuteEvents.pointerUpHandler);
        }

        if (_currentHover != null && _pointerData != null)
            ExecuteEvents.ExecuteHierarchy(_currentHover, _pointerData, ExecuteEvents.pointerExitHandler);

        _currentHover = null;
        _pressTarget = null;
        _isPointerDown = false;
        if (_pointerData != null)
        {
            _pointerData.pointerDrag = null;
            _pointerData.dragging = false;
            _pointerData.pointerPress = null;
        }
    }

    private void OnDisable()
    {
        SetBridgeEnabled(false);
    }
}

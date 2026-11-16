using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the off-screen Phone UI camera + RenderTexture pipeline and page switching.
/// RT aspect matches the Screen mesh; Screen Space - Camera + ConstantPixelSize (1 RT pixel = 1 UI unit).
/// </summary>
[DisallowMultipleComponent]
public class PhoneUIController : MonoBehaviour
{
    public const int DesignLongSide = 1920;

    [Header("Wiring")]
    [SerializeField] private GameObject phoneView;
    [SerializeField] private PhoneScreenPresenter screenPresenter;
    [SerializeField] private Canvas phoneCanvas;
    [SerializeField] private Camera phoneUiCamera;
    [SerializeField] private PhoneScreenInputBridge inputBridge;
    [SerializeField] private GameObject worldDimPanel;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private Transform pageHost;
    [SerializeField] private GameObject pausePage;
    [SerializeField] private GameObject browserFeedPage;

    [Header("Runtime")]
    [SerializeField] private int phoneUiLayer = -1;

    private RenderTexture _renderTexture;
    private PhoneScreenId _activeScreen = PhoneScreenId.Pause;
    private bool _isVisible;
    private int _rtWidth = 1080;
    private int _rtHeight = 1920;

    public bool IsVisible => _isVisible;
    public PhoneScreenId ActiveScreen => _activeScreen;
    public RenderTexture ScreenTexture => _renderTexture;
    public int RtWidth => _rtWidth;
    public int RtHeight => _rtHeight;
    public event Action<bool> VisibilityChanged;

    private void Awake()
    {
        EnsurePipeline();
        ExcludePhoneLayerFromGameplayCameras();
        ParkUiRigOffscreen();
        HideImmediate();
    }

    private void OnDestroy()
    {
        ReleaseRenderTexture();
    }

    public void Show(PhoneScreenId screenId)
    {
        EnsurePipeline();
        _activeScreen = screenId;
        ApplyActivePage();

        if (phoneView != null)
            phoneView.SetActive(true);
        if (worldDimPanel != null)
            worldDimPanel.SetActive(true);

        SetCrosshairVisible(false);

        if (phoneCanvas != null)
            phoneCanvas.gameObject.SetActive(true);
        if (phoneUiCamera != null)
        {
            phoneUiCamera.gameObject.SetActive(true);
            phoneUiCamera.enabled = true;
        }

        BindTextureToPresenter();
        WireInputBridge();
        inputBridge?.SetBridgeEnabled(true);

        _isVisible = true;
        VisibilityChanged?.Invoke(true);
    }

    public void Hide()
    {
        if (!_isVisible)
        {
            HideImmediate();
            return;
        }

        HideImmediate();
        VisibilityChanged?.Invoke(false);
    }

    private void HideImmediate()
    {
        inputBridge?.SetBridgeEnabled(false);

        if (phoneView != null)
            phoneView.SetActive(false);
        if (worldDimPanel != null)
            worldDimPanel.SetActive(false);

        SetCrosshairVisible(true);

        if (phoneCanvas != null)
            phoneCanvas.gameObject.SetActive(false);
        if (phoneUiCamera != null)
        {
            phoneUiCamera.enabled = false;
            phoneUiCamera.gameObject.SetActive(false);
        }

        _isVisible = false;
    }

    private void LateUpdate()
    {
        // Something in HUD flow can re-enable Crosshair; keep it down while phone is up.
        if (_isVisible)
            SetCrosshairVisible(false);
    }

    /// <summary>
    /// Hides/shows every Crosshair in the loaded scenes (prefab refs are often stale).
    /// </summary>
    public void SetCrosshairVisible(bool visible)
    {
        if (crosshair != null)
            ApplyCrosshairVisibility(crosshair, visible);

        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (var t in transforms)
        {
            if (t != null && t.name == "Crosshair")
                ApplyCrosshairVisibility(t.gameObject, visible);
        }
    }

    private static void ApplyCrosshairVisibility(GameObject go, bool visible)
    {
        if (go == null)
            return;

        go.SetActive(visible);

        var image = go.GetComponent<Image>();
        if (image != null)
            image.enabled = visible;

        var canvasRenderer = go.GetComponent<CanvasRenderer>();
        if (canvasRenderer != null && !visible)
            canvasRenderer.SetAlpha(0f);
        else if (canvasRenderer != null && visible)
            canvasRenderer.SetAlpha(1f);
    }

    private void ExcludePhoneLayerFromGameplayCameras()
    {
        if (phoneUiLayer < 0)
            phoneUiLayer = LayerMask.NameToLayer("PhoneUI");
        if (phoneUiLayer < 0)
            return;

        int maskBit = 1 << phoneUiLayer;
        var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);
        foreach (var cam in cameras)
        {
            if (cam == null || cam == phoneUiCamera)
                continue;
            cam.cullingMask &= ~maskBit;
        }
    }

    private void ParkUiRigOffscreen()
    {
        Transform rig = null;
        if (phoneUiCamera != null)
            rig = phoneUiCamera.transform.parent;
        if (rig == null && phoneCanvas != null)
            rig = phoneCanvas.transform.parent;
        if (rig == null)
            return;

        rig.localPosition = new Vector3(0f, -50f, 0f);
        rig.localRotation = Quaternion.identity;
        rig.localScale = Vector3.one;
    }

    private void ApplyActivePage()
    {
        if (pausePage != null)
            pausePage.SetActive(_activeScreen == PhoneScreenId.Pause);
        if (browserFeedPage != null)
            browserFeedPage.SetActive(_activeScreen == PhoneScreenId.BrowserFeed);
    }

    private void EnsurePipeline()
    {
        if (screenPresenter == null && phoneView != null)
            screenPresenter = phoneView.GetComponentInChildren<PhoneScreenPresenter>(true);

        ResolveRtSizeFromScreenMesh();
        EnsureRenderTexture();

        if (phoneUiCamera != null)
        {
            phoneUiCamera.targetTexture = _renderTexture;
            phoneUiCamera.clearFlags = CameraClearFlags.SolidColor;
            phoneUiCamera.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
            phoneUiCamera.orthographic = true;
            phoneUiCamera.nearClipPlane = 0.01f;
            phoneUiCamera.farClipPlane = 100f;
            phoneUiCamera.allowHDR = false;
            phoneUiCamera.allowMSAA = false;
            if (phoneUiLayer >= 0)
                phoneUiCamera.cullingMask = 1 << phoneUiLayer;
        }

        if (phoneCanvas != null && phoneUiCamera != null)
        {
            // Identity camera: SS-Camera places the canvas in front, no Y180 / no RT flips.
            phoneUiCamera.transform.localPosition = Vector3.zero;
            phoneUiCamera.transform.localRotation = Quaternion.identity;
            phoneUiCamera.ResetProjectionMatrix();
            phoneUiCamera.orthographic = true;
            phoneUiCamera.orthographicSize = 5f;

            phoneCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            phoneCanvas.worldCamera = phoneUiCamera;
            phoneCanvas.planeDistance = 1f;

            var canvasRt = phoneCanvas.GetComponent<RectTransform>();
            if (canvasRt != null)
            {
                canvasRt.localScale = Vector3.one;
                canvasRt.localRotation = Quaternion.identity;
                canvasRt.localPosition = Vector3.zero;
                canvasRt.anchorMin = new Vector2(0.5f, 0.5f);
                canvasRt.anchorMax = new Vector2(0.5f, 0.5f);
                canvasRt.pivot = new Vector2(0.5f, 0.5f);
                canvasRt.sizeDelta = new Vector2(_rtWidth, _rtHeight);
            }

            // ConstantPixelSize avoids ScaleWithScreenSize picking up Game View aspect.
            var scaler = phoneCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
                scaler.referencePixelsPerUnit = 100f;
            }

            StripBrokenRoundedMask(phoneCanvas.transform);
        }

        BindTextureToPresenter();
        WireInputBridge();
    }

    private static void StripBrokenRoundedMask(Transform canvasRoot)
    {
        Transform maskTf = null;
        foreach (var t in canvasRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "RoundedMask")
            {
                maskTf = t;
                break;
            }
        }

        if (maskTf == null)
            return;

        var mask = maskTf.GetComponent<Mask>();
        if (mask != null)
            Destroy(mask);

        var rectMask = maskTf.GetComponent<RectMask2D>();
        if (rectMask != null)
            Destroy(rectMask);

        var image = maskTf.GetComponent<Image>();
        if (image != null)
        {
            // Keep Image only if needed as a container; make it fully transparent / non-masking.
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;
            image.maskable = false;
        }

        var rt = maskTf as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }

    private void WireInputBridge()
    {
        if (inputBridge == null || phoneCanvas == null || phoneUiCamera == null)
            return;

        if (screenPresenter == null && phoneView != null)
            screenPresenter = phoneView.GetComponentInChildren<PhoneScreenPresenter>(true);

        Camera gameplayCam = null;
        var cams = GetComponentsInChildren<Camera>(true);
        foreach (var cam in cams)
        {
            if (cam != phoneUiCamera)
            {
                gameplayCam = cam;
                break;
            }
        }

        if (gameplayCam != null && screenPresenter != null)
            inputBridge.Configure(gameplayCam, phoneUiCamera, phoneCanvas, screenPresenter);
    }

    private void ResolveRtSizeFromScreenMesh()
    {
        float aspect = 9f / 16f;

        if (screenPresenter == null && phoneView != null)
            screenPresenter = phoneView.GetComponentInChildren<PhoneScreenPresenter>(true);

        if (screenPresenter != null)
        {
            screenPresenter.EnsureDisplayReady();
            Bounds b = screenPresenter.DisplayLocalBounds;
            float width = b.size.x;
            float height = b.size.z > 1e-5f ? b.size.z : b.size.y;
            if (width > 1e-5f && height > 1e-5f)
                aspect = width / height;
        }

        if (aspect <= 1f)
        {
            _rtHeight = DesignLongSide;
            _rtWidth = Mathf.Max(2, Mathf.RoundToInt(DesignLongSide * aspect));
            if ((_rtWidth & 1) == 1) _rtWidth++;
        }
        else
        {
            _rtWidth = DesignLongSide;
            _rtHeight = Mathf.Max(2, Mathf.RoundToInt(DesignLongSide / aspect));
            if ((_rtHeight & 1) == 1) _rtHeight++;
        }
    }

    private void EnsureRenderTexture()
    {
        if (_renderTexture != null &&
            _renderTexture.width == _rtWidth &&
            _renderTexture.height == _rtHeight)
            return;

        ReleaseRenderTexture();

        _renderTexture = new RenderTexture(_rtWidth, _rtHeight, 24, RenderTextureFormat.ARGB32)
        {
            name = "PhoneUI_RT",
            antiAliasing = 1,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };
        _renderTexture.Create();
    }

    private void ReleaseRenderTexture()
    {
        if (_renderTexture == null)
            return;

        if (phoneUiCamera != null)
            phoneUiCamera.targetTexture = null;

        _renderTexture.Release();
        Destroy(_renderTexture);
        _renderTexture = null;
    }

    private void BindTextureToPresenter()
    {
        if (screenPresenter == null && phoneView != null)
            screenPresenter = phoneView.GetComponentInChildren<PhoneScreenPresenter>(true);

        screenPresenter?.SetRenderTexture(_renderTexture);
    }
}

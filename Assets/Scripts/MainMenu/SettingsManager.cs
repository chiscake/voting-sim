using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Persistent audio/brightness settings used by the main-menu sliders.
/// Bootstraps itself before the first scene load if no instance exists in the scene.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string VolumePrefsKey = "settings.volume";
    private const string BrightnessPrefsKey = "settings.brightness";
    private const float DefaultVolume = 1f;
    private const float DefaultBrightness = 1f;

    [SerializeField] private Image brightnessOverlay;

    private float _volume = DefaultVolume;
    private float _brightness = DefaultBrightness;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        var go = new GameObject(nameof(SettingsManager));
        go.AddComponent<SettingsManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureBrightnessOverlay();
        LoadAndApply();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public float GetVolume() => _volume;

    public float GetBrightness() => _brightness;

    public void SetVolume(float value)
    {
        _volume = Mathf.Clamp01(value);
        AudioListener.volume = _volume;
        PlayerPrefs.SetFloat(VolumePrefsKey, _volume);
        PlayerPrefs.Save();
    }

    public void SetBrightness(float value)
    {
        _brightness = Mathf.Clamp01(value);
        ApplyBrightness();
        PlayerPrefs.SetFloat(BrightnessPrefsKey, _brightness);
        PlayerPrefs.Save();
    }

    private void LoadAndApply()
    {
        _volume = PlayerPrefs.GetFloat(VolumePrefsKey, DefaultVolume);
        _brightness = PlayerPrefs.GetFloat(BrightnessPrefsKey, DefaultBrightness);
        AudioListener.volume = _volume;
        ApplyBrightness();
    }

    private void ApplyBrightness()
    {
        if (brightnessOverlay == null)
            return;

        var color = brightnessOverlay.color;
        // 1 = full bright (transparent overlay), 0 = dark (opaque black overlay).
        color.a = 1f - _brightness;
        brightnessOverlay.color = color;
    }

    private void EnsureBrightnessOverlay()
    {
        if (brightnessOverlay != null)
            return;

        var canvasGo = new GameObject("BrightnessOverlayCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        canvasGo.AddComponent<CanvasScaler>();

        var imageGo = new GameObject("BrightnessOverlay");
        imageGo.transform.SetParent(canvasGo.transform, false);
        brightnessOverlay = imageGo.AddComponent<Image>();
        brightnessOverlay.color = Color.black;
        brightnessOverlay.raycastTarget = false;

        var rect = brightnessOverlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("Brightness UI")]
    [SerializeField] private Image brightnessOverlayImage; // Ссылка на черную картинку на этом же Canvas

    private float currentVolume = 1f;
    private float currentBrightness = 1f;

    private void Awake()
    {
        // Паттерн Singleton + DontDestroyOnLoad для всего объекта (включая Canvas)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Сохраняет сам объект, Canvas и картинку во всех сценах!
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void LoadSettings()
    {
        currentVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
        currentBrightness = PlayerPrefs.GetFloat("GameBrightness", 1f);

        ApplyVolume(currentVolume);
        ApplyBrightness(currentBrightness);
    }

    public void SetVolume(float value)
    {
        currentVolume = value;
        ApplyVolume(value);
        PlayerPrefs.SetFloat("GameVolume", value);
        PlayerPrefs.Save();
    }

    public void SetBrightness(float value)
    {
        currentBrightness = value;
        ApplyBrightness(value);
        PlayerPrefs.SetFloat("GameBrightness", value);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float value)
    {
        AudioListener.volume = value;
    }

    private void ApplyBrightness(float value)
    {
        if (brightnessOverlayImage != null)
        {
            Color color = brightnessOverlayImage.color;
            // Плавное изменение прозрачности: от 0.8 (темно) до 0 (ярко)
            color.a = Mathf.Lerp(0.8f, 0f, value);
            brightnessOverlayImage.color = color;
        }
    }

    public float GetVolume() => currentVolume;
    public float GetBrightness() => currentBrightness;
}

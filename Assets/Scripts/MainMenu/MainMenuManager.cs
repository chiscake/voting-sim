using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "GameScene"; // Имя вашей игровой сцены

    [Header("Camera & Views")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform tableSeatPoint; // Точка взгляда на бюллетень (начальная)
    [SerializeField] private Transform phoneSeatPoint; // Точка взгляда на телефон (настройки)
    [SerializeField] private float transitionSpeed = 5f;

    [Header("UI Panels")]
    [SerializeField] private GameObject ballotMenuPanel; // Панель бюллетеня (главное меню)
    [SerializeField] private GameObject settingsPhonePanel; // Панель телефона (настройки)

    [Header("Settings Sliders")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider brightnessSlider;

    private Transform targetViewPoint;
    private bool isTransitioning = false;

    private void Start()
    {
        if (mainCamera != null && tableSeatPoint != null)
        {
            mainCamera.transform.position = tableSeatPoint.position;
            mainCamera.transform.rotation = tableSeatPoint.rotation;
        }

        targetViewPoint = tableSeatPoint;

        // Настройка начального состояния панелей
        if (ballotMenuPanel != null) ballotMenuPanel.SetActive(true);
        if (settingsPhonePanel != null) settingsPhonePanel.SetActive(false);

        // Инициализация ползунков
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (brightnessSlider != null)
        {
            // Здесь можно подключить ваш менеджер яркости
            brightnessSlider.value = 1f;
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }
    }

    private void Update()
    {
        // Плавный перелет камеры между столом (бюллетенем) и телефоном
        if (isTransitioning && targetViewPoint != null && mainCamera != null)
        {
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetViewPoint.position, Time.deltaTime * transitionSpeed);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetViewPoint.rotation, Time.deltaTime * transitionSpeed);

            if (Vector3.Distance(mainCamera.transform.position, targetViewPoint.position) < 0.05f)
            {
                mainCamera.transform.position = targetViewPoint.position;
                mainCamera.transform.rotation = targetViewPoint.rotation;
                isTransitioning = false;
            }
        }
    }

    // --- КНОПКИ ГЛАВНОГО МЕНЮ (БЮЛЛЕТЕНЬ) ---

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        if (phoneSeatPoint != null)
        {
            targetViewPoint = phoneSeatPoint;
            isTransitioning = true;
        }

        if (ballotMenuPanel != null) ballotMenuPanel.SetActive(false);
        if (settingsPhonePanel != null) settingsPhonePanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Выход из игры...");
        Application.Quit();
    }

    // --- КНОПКИ И НАСТРОЙКИ ТЕЛЕФОНА ---

    public void BackToMainMenu()
    {
        if (tableSeatPoint != null)
        {
            targetViewPoint = tableSeatPoint;
            isTransitioning = true;
        }

        if (settingsPhonePanel != null) settingsPhonePanel.SetActive(false);
        if (ballotMenuPanel != null) ballotMenuPanel.SetActive(true);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
    }

    public void SetBrightness(float value)
    {
        // Реализуйте изменение яркости через Post-Processing или мировой Overlay-панель
        Debug.Log($"Яркость: {value}");
    }
}

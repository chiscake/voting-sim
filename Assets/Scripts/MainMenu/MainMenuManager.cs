using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Camera & Views")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform tableSeatPoint;
    [SerializeField] private Transform phoneSeatPoint;
    [SerializeField] private float transitionSpeed = 5f;

    [Header("UI Panels")]
    [SerializeField] private GameObject ballotMenuPanel;
    [SerializeField] private GameObject settingsPhonePanel;

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

        if (ballotMenuPanel != null) ballotMenuPanel.SetActive(true);
        if (settingsPhonePanel != null) settingsPhonePanel.SetActive(false);

        // Initialize sliders from the global SettingsManager.
        if (SettingsManager.Instance != null)
        {
            if (volumeSlider != null)
            {
                volumeSlider.value = SettingsManager.Instance.GetVolume();
                volumeSlider.onValueChanged.AddListener(SettingsManager.Instance.SetVolume);
            }

            if (brightnessSlider != null)
            {
                brightnessSlider.value = SettingsManager.Instance.GetBrightness();
                brightnessSlider.onValueChanged.AddListener(SettingsManager.Instance.SetBrightness);
            }
        }
    }

    private void Update()
    {
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

    public void PlayGame()
    {
        // CharacterCreationScene was referenced by the last commit but never added to the repo.
        // Load the playable polling-station scene until that flow is committed.
        SceneManager.LoadScene("PollingStation");
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
        Application.Quit();
    }

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
}

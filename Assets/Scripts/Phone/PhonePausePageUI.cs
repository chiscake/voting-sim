using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pause controls hosted on the phone screen page.
/// </summary>
public class PhonePausePageUI : MonoBehaviour
{
    private PlayerController _player;

    private void OnEnable()
    {
        ResolvePlayer();
    }

    public void ResumeGame()
    {
        ResolvePlayer();
        if (_player != null)
            _player.ClosePhoneModal();
        else
            Debug.LogWarning("PhonePausePageUI: PlayerController not found; cannot resume.");
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void ResolvePlayer()
    {
        if (_player == null)
            _player = FindAnyObjectByType<PlayerController>();
    }
}

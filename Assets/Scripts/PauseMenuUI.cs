using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    private PlayerController playerController;

    private void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();
    }

    // Метод для кнопки "Продолжить" (Resume)
    public void ResumeGame()
    {
        if (playerController != null)
            playerController.ClosePhoneModal();
    }

    // Метод для кнопки "Выход в главное меню" или закрытия игры
    public void QuitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}

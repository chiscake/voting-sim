using UnityEngine;

public class PauseMenuUI : MonoBehaviour
{
    private PlayerController playerController;

    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
    }

    // Метод для кнопки "Продолжить" (Resume)
    public void ResumeGame()
    {
        if (playerController != null)
        {
            playerController.TogglePause();
        }
    }

    // Метод для кнопки "Выход в главное меню" или закрытия игры
    public void QuitGame()
    {
        Time.timeScale = 1f;
        Debug.Log("Выход из игры...");
        Application.Quit();
    }
}
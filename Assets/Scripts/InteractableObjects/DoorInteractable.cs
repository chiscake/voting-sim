using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Mode Settings")]
    [Tooltip("Если включено — дверь загружает новую сцену. Если выключено — просто открывается/закрывается на месте.")]
    [SerializeField] private bool changeScene = false; // Галочка по умолчанию отключена (false)

    [Header("Scene Transition Settings")]
    [SerializeField] private string targetSceneName = "GameScene";

    [Header("Prompts")]
    [SerializeField] private string scenePromptText = "[E] Войти в дверь";
    [SerializeField] private string openPromptText = "[E] Открыть дверь";
    [SerializeField] private string closePromptText = "[E] Закрыть дверь";

    [Header("Door Open Logic (In-World)")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string closeTriggerName = "Close";

    private bool isOpen = false;
    private bool isTransitioning = false; // Защита от спама при переходе сцены

    public string GetInteractPrompt()
    {
        if (isTransitioning) return "";

        if (changeScene)
        {
            return scenePromptText;
        }
        else
        {
            return isOpen ? closePromptText : openPromptText;
        }
    }

    public void Interact(PlayerController player)
    {
        if (isTransitioning) return;

        if (changeScene)
        {
            // РЕЖИМ 1: Загрузка новой сцены
            isTransitioning = true;

            if (doorAnimator != null)
            {
                doorAnimator.SetTrigger(openTriggerName);
            }

            if (player.interactionPromptText != null)
            {
                player.interactionPromptText.text = "";
            }

            Debug.Log($"Загрузка сцены: {targetSceneName}");
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            // РЕЖИМ 2: Обычное открытие / закрытие двери
            isOpen = !isOpen;

            if (doorAnimator != null)
            {
                doorAnimator.SetTrigger(isOpen ? openTriggerName : closeTriggerName);
            }
            else
            {
                // Если нет аниматора, просто поворачиваем объект на 90 градусов (простой фоллбэк)
                transform.Rotate(0, isOpen ? 90f : -90f, 0);
            }

            // Обновляем текст подсказки на экране под новое состояние
            if (player.interactionPromptText != null)
            {
                player.interactionPromptText.text = GetInteractPrompt();
            }
        }
    }
}

using UnityEngine;

public class PassportInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptText = "[E] Осмотреть паспорт";

    public string GetInteractPrompt()
    {
        return promptText;
    }

    public void Interact(PlayerController player)
    {
        // Ищем менеджер UI осмотра паспорта на сцене и открываем его
        PassportInspectUI inspectUI = FindAnyObjectByType<PassportInspectUI>();
        if (inspectUI != null)
        {
            inspectUI.OpenInspection();
        }
        else
        {
            Debug.LogWarning("На сцене не найден компонент PassportInspectUI!");
        }
    }
}

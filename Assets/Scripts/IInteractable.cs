public interface IInteractable
{
    string GetInteractPrompt(); // Текст подсказки для экрана (например, "Сесть за стол" или "Взять ручку")
    void Interact(PlayerController player); // Сам сигнал взаимодействия
}
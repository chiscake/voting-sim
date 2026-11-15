using UnityEngine;

public class PlayerInteractionHandler
{
    private PlayerController owner;
    private Outline currentOutline;
    private LayerMask interactLayerMask; // Маска слоев для луча

    public PlayerInteractionHandler(PlayerController owner)
    {
        this.owner = owner;

        // Создаем маску, которая исключает слой "Player". 
        // Все остальные слои будут проверяться.
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            interactLayerMask = ~(1 << playerLayer);
        }
        else
        {
            // Если слой Player еще не создан, проверяем всё по умолчанию
            interactLayerMask = Physics.DefaultRaycastLayers;
            Debug.LogWarning("Слой 'Player' не найден в проекте! Луч может попадать в коллайдеры игрока.");
        }
    }

    public void Update()
    {
        HandleInteractions();
        CheckLookAtObject();
    }

    private void HandleInteractions()
    {
        // Взаимодействие по клавише E (interactAction)
        if (owner.interactAction != null && owner.interactAction.action.WasPressedThisFrame())
        {
            if (owner.mainCamera != null)
            {
                Ray ray = new Ray(owner.mainCamera.transform.position, owner.mainCamera.transform.forward);
                // Передаем interactLayerMask в Raycast
                if (Physics.Raycast(ray, out RaycastHit hit, owner.pickUpRange, interactLayerMask))
                {
                    IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        interactable.Interact(owner);
                    }
                }
            }
        }
    }

    private void CheckLookAtObject()
    {
        if (owner.mainCamera == null) return;

        Ray ray = new Ray(owner.mainCamera.transform.position, owner.mainCamera.transform.forward);
        // Передаем interactLayerMask в Raycast для проверки взгляда и подсветки
        if (Physics.Raycast(ray, out RaycastHit hit, owner.pickUpRange, interactLayerMask))
        {
            // 1. Проверяем интерактивный объект
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                SetPromptText(interactable.GetInteractPrompt());
            }
            else
            {
                // 2. Проверяем предмет для инвентаря (F)
                WorldItem worldItem = hit.collider.GetComponent<WorldItem>();
                if (worldItem != null)
                {
                    SetPromptText("[F] Подобрать в инвентарь");
                }
                else
                {
                    SetPromptText("");
                }
            }

            Outline hitOutline = hit.collider.GetComponent<Outline>();
            if (hitOutline != currentOutline)
            {
                DisableCurrentOutline();
                currentOutline = hitOutline;
                if (currentOutline != null) currentOutline.enabled = true;
            }
        }
        else
        {
            DisableCurrentOutline();
            SetPromptText("");
        }
    }

    private void DisableCurrentOutline()
    {
        if (currentOutline != null)
        {
            currentOutline.enabled = false;
            currentOutline = null;
        }
    }

    private void SetPromptText(string text)
    {
        if (owner.interactionPromptText != null)
        {
            owner.interactionPromptText.text = text;
        }
    }
}

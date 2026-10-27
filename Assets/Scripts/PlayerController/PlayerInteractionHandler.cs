using UnityEngine;

public class PlayerInteractionHandler
{
    private PlayerController owner;
    private Rigidbody heldObject;
    private Outline currentOutline;

    public PlayerInteractionHandler(PlayerController owner)
    {
        this.owner = owner;
    }

    public void Update()
    {
        HandleInteractions();
        CheckLookAtObject();
    }

    public void LateUpdate()
    {
        HandleHeldObjectPosition();
    }

    private void HandleInteractions()
    {
        // 1. Взаимодействие по клавише E (interactAction)
        if (owner.interactAction != null && owner.interactAction.action.WasPressedThisFrame())
        {
            if (owner.mainCamera != null)
            {
                Ray ray = new Ray(owner.mainCamera.transform.position, owner.mainCamera.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, owner.pickUpRange))
                {
                    IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        interactable.Interact(owner);
                    }
                }
            }
        }

        // 2. Физический захват в руку (Grab)
        if (owner.grabAction != null && owner.grabAction.action.WasPressedThisFrame())
        {
            if (heldObject == null) TryPickUpPhysicsObject();
        }

        // 3. Бросок из руки (Throw)
        if (owner.throwAction != null && owner.throwAction.action.WasPressedThisFrame())
        {
            if (heldObject != null) ThrowGrabbedObject();
        }
    }

    private void TryPickUpPhysicsObject()
    {
        if (owner.mainCamera == null || owner.holdPosition == null) return;

        Ray ray = new Ray(owner.mainCamera.transform.position, owner.mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, owner.pickUpRange))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                DisableCurrentOutline();
                heldObject = rb;
                heldObject.useGravity = false;
                heldObject.detectCollisions = true;
                SetPromptText("");
            }
        }
    }

    private void HandleHeldObjectPosition()
    {
        if (heldObject != null && owner.holdPosition != null)
        {
            heldObject.linearVelocity = Vector3.zero;
            heldObject.angularVelocity = Vector3.zero;

            heldObject.position = Vector3.Lerp(heldObject.position, owner.holdPosition.position, Time.deltaTime * 25f);
            heldObject.rotation = Quaternion.Slerp(heldObject.rotation, owner.holdPosition.rotation, Time.deltaTime * 25f);
        }
    }

    private void ThrowGrabbedObject()
    {
        if (heldObject == null || owner.mainCamera == null) return;

        heldObject.useGravity = true;
        heldObject.AddForce(owner.mainCamera.transform.forward * owner.throwForce, ForceMode.Impulse);
        heldObject = null;
    }

    private void CheckLookAtObject()
    {
        if (heldObject != null)
        {
            DisableCurrentOutline();
            SetPromptText("");
            return;
        }

        if (owner.mainCamera == null) return;

        Ray ray = new Ray(owner.mainCamera.transform.position, owner.mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, owner.pickUpRange))
        {
            // 1. Проверяем интерактивный объект (например, стол с бюллетенем)
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
                    // 3. Проверяем физический объект (ЛКМ)
                    Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                    {
                        SetPromptText("[ЛКМ] Взять в руку");
                    }
                    else
                    {
                        SetPromptText("");
                    }
                }
            }

            // Подсветка Outline
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
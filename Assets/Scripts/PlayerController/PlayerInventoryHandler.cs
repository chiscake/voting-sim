using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInventoryHandler
{
    private PlayerController owner;
    private InventorySlot[] inventory;
    private int selectedSlotIndex = -1; // -1 означает, что руки пусты

    private Transform inventorySlotsParent;
    private List<GameObject> spawnedSlots = new List<GameObject>();

    private GameObject currentHandItemModel; // Текущая 3D модель в руке

    public class InventorySlot
    {
        public ItemData itemData;
        public bool IsEmpty => itemData == null;
    }

    public PlayerInventoryHandler(PlayerController owner)
    {
        this.owner = owner;

        GameObject panelObj = GameObject.Find("InventoryPanel");
        if (panelObj != null)
        {
            inventorySlotsParent = panelObj.transform;
        }

        int slotCount = inventorySlotsParent != null ? inventorySlotsParent.childCount : 3;
        owner.inventorySize = slotCount;

        inventory = new InventorySlot[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            inventory[i] = new InventorySlot();
        }

        spawnedSlots.Clear();
        if (inventorySlotsParent != null)
        {
            foreach (Transform child in inventorySlotsParent)
            {
                spawnedSlots.Add(child.gameObject);
            }
        }

        UpdateUI();
        UpdateHandItemVisual();
    }

    public void Update()
    {
        // 1. Цифровые клавиши для выбора слотов (1-9)
        if (Keyboard.current != null)
        {
            for (int i = 0; i < Mathf.Min(inventory.Length, 9); i++)
            {
                Key digitKey = Key.Digit1 + i;
                if (Keyboard.current[digitKey].wasPressedThisFrame)
                {
                    ChangeSelectedSlot(i);
                    break;
                }
            }
        }

        // 2. Действие "Освободить руки" (FreeHands)
        if (owner.freeHandsAction != null && owner.freeHandsAction.action.WasPressedThisFrame())
        {
            FreeHands();
        }

        // 3. Подбор предмета по F (pickAction)
        if (owner.pickAction != null && owner.pickAction.action.WasPressedThisFrame())
        {
            TryPickUpToInventory();
        }

        // 4. Выброс предмета из выбранного слота по G (dropAction)
        if (owner.dropAction != null && owner.dropAction.action.WasPressedThisFrame())
        {
            DropFromInventory();
        }
    }

    public bool TryAddItem(ItemData item)
    {
        int emptySlot = -1;
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].IsEmpty)
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot != -1)
        {
            inventory[emptySlot].itemData = item;
            UpdateUI();

            // Автоматически выбираем этот слот — предмет сразу берется в руку
            ChangeSelectedSlot(emptySlot);
            return true;
        }

        Debug.Log("Инвентарь полон!");
        return false;
    }

    public void ChangeSelectedSlot(int index)
    {
        if (index < 0 || index >= inventory.Length) return;

        selectedSlotIndex = index;
        UpdateUI();
        UpdateHandItemVisual();
    }

    /// <summary>
    /// Сбрасывает выбор слота — руки становятся пустыми
    /// </summary>
    public void FreeHands()
    {
        selectedSlotIndex = -1;
        UpdateUI();
        UpdateHandItemVisual();
        Debug.Log("Руки освобождены.");
    }

    private void UpdateHandItemVisual()
    {
        // 1. Удаляем текущую модель из руки
        if (currentHandItemModel != null)
        {
            Object.Destroy(currentHandItemModel);
            currentHandItemModel = null;
        }

        // 2. Если слот не выбран (-1) или он пустой — рука остается пустой
        if (selectedSlotIndex < 0 || selectedSlotIndex >= inventory.Length) return;

        InventorySlot slot = inventory[selectedSlotIndex];
        if (slot.IsEmpty) return;

        // 3. Если в слоте есть предмет и у него задан префаб для руки, спавним его в Hand Socket
        if (slot.itemData.prefabInHand != null && owner.handSocket != null)
        {
            currentHandItemModel = Object.Instantiate(slot.itemData.prefabInHand, owner.handSocket);
            currentHandItemModel.transform.localPosition = Vector3.zero;
            currentHandItemModel.transform.localRotation = Quaternion.identity;
        }
    }

    private void TryPickUpToInventory()
    {
        if (owner.mainCamera == null) return;

        Ray ray = new Ray(owner.mainCamera.transform.position, owner.mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, owner.pickUpRange))
        {
            WorldItem worldItem = hit.collider.GetComponent<WorldItem>();
            if (worldItem != null && worldItem.itemData != null)
            {
                bool success = TryAddItem(worldItem.itemData);
                if (success)
                {
                    Object.Destroy(hit.collider.gameObject);
                }
            }
        }
    }

    private void DropFromInventory()
    {
        if (selectedSlotIndex < 0 || selectedSlotIndex >= inventory.Length) return;

        InventorySlot slot = inventory[selectedSlotIndex];
        if (!slot.IsEmpty)
        {
            Vector3 spawnPos = owner.itemDropPoint != null ? owner.itemDropPoint.position : owner.mainCamera.transform.position + owner.mainCamera.transform.forward * 1.5f;
            Quaternion spawnRot = owner.itemDropPoint != null ? owner.itemDropPoint.rotation : owner.mainCamera.transform.rotation;

            if (slot.itemData.itemPrefab != null)
            {
                GameObject droppedObj = Object.Instantiate(slot.itemData.itemPrefab, spawnPos, spawnRot);
                Rigidbody rb = droppedObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(spawnRot * Vector3.forward * 2f, ForceMode.Impulse);
                }
            }

            slot.itemData = null;
            UpdateUI();
            UpdateHandItemVisual(); // Модель из руки пропадет
        }
    }

    private void UpdateUI()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            GameObject slotObj = spawnedSlots[i];
            Image slotBg = slotObj.GetComponent<Image>();

            Transform iconTransform = slotObj.transform.Find("ItemIcon");
            Image iconImg = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

            if (slotBg != null)
            {
                // Подсвечиваем активный слот только если selectedSlotIndex совпадает
                slotBg.color = (i == selectedSlotIndex) ? new Color(1f, 0.9f, 0.3f, 1f) : Color.white;
            }

            if (iconImg != null)
            {
                if (!inventory[i].IsEmpty && inventory[i].itemData.icon != null)
                {
                    iconImg.gameObject.SetActive(true);
                    iconImg.sprite = inventory[i].itemData.icon;
                }
                else
                {
                    iconImg.gameObject.SetActive(false);
                    iconImg.sprite = null;
                }
            }
        }
    }
}

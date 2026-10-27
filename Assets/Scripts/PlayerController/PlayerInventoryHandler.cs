using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInventoryHandler
{
    private PlayerController owner;
    private InventorySlot[] inventory;
    private int selectedSlotIndex = 0;

    private Transform inventorySlotsParent;
    private List<GameObject> spawnedSlots = new List<GameObject>();

    public class InventorySlot
    {
        public ItemData itemData;
        public bool IsEmpty => itemData == null;
    }

    public PlayerInventoryHandler(PlayerController owner)
    {
        this.owner = owner;

        // Находим панель инвентаря на сцене
        GameObject panelObj = GameObject.Find("InventoryPanel");
        if (panelObj != null)
        {
            inventorySlotsParent = panelObj.transform;
        }

        // Автоматически определяем количество ячеек, которые вы создали в UI!
        int slotCount = inventorySlotsParent != null ? inventorySlotsParent.childCount : 3;
        owner.inventorySize = slotCount;

        inventory = new InventorySlot[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            inventory[i] = new InventorySlot();
        }

        // Собираем уже существующие на сцене ячейки из UI
        spawnedSlots.Clear();
        if (inventorySlotsParent != null)
        {
            foreach (Transform child in inventorySlotsParent)
            {
                spawnedSlots.Add(child.gameObject);
            }
        }

        UpdateUI();
    }

    public void Update()
    {
        if (Keyboard.current != null)
        {
            // Динамический опрос цифровых клавиш от 1 до количества ячеек (до 9 штук)
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

        if (owner.pickAction != null && owner.pickAction.action.WasPressedThisFrame())
        {
            TryPickUpToInventory();
        }

        if (owner.dropAction != null && owner.dropAction.action.WasPressedThisFrame())
        {
            DropFromInventory();
        }
    }

    private void ChangeSelectedSlot(int index)
    {
        selectedSlotIndex = index;
        UpdateUI();
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
                    inventory[emptySlot].itemData = worldItem.itemData;
                    Object.Destroy(hit.collider.gameObject);
                    UpdateUI();
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

            // Визуальное выделение активной ячейки изменением цвета фона
            if (slotBg != null)
            {
                slotBg.color = (i == selectedSlotIndex) ? new Color(1f, 0.9f, 0.3f, 1f) : Color.white; // Желтоватый цвет для активной
            }

            // Отображение иконки предмета
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
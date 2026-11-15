using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject itemPrefab; // Настоящий префаб хранится здесь в безопасности
    public GameObject prefabInHand; // 3D модель, которая появится в руке, когда слот выбран
}

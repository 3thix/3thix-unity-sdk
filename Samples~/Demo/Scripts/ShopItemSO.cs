using UnityEngine;

namespace Ethix.Demo
{
    [CreateAssetMenu(fileName = "ShopItem", menuName = "Demo/Shop Item SO")]
    public class ShopItemSO : ScriptableObject
    {
        public ItemTypes ItemType;
        public GameObject ShopItemPrefab;
        public string ItemName;
        public int Amount = 0;
        public float Price = 0;
        public Sprite ItemImage;
        public InventoryItemSO InventoryItemSO;
    }
}
using UnityEngine;

namespace Ethix.Demo
{
    [CreateAssetMenu(fileName = "Inventory Item", menuName = "Demo/Inventory Item")]
    public class InventoryItemSO : ScriptableObject
    {
        public ItemTypes ItemType;
        public string ItemName;
        public int Amount = 0;
        public Sprite ItemImage;
    }

    public enum ItemTypes
    {
        GoldCoins,
        Food
    }
}
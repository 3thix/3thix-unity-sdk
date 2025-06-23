using UnityEngine;
using UnityEngine.UI;

namespace Ethix.Demo
{
    public class InventoryItem : MonoBehaviour
    {
        [SerializeField] private Text _itemAmountText;
        [SerializeField] private Image _itemImage;
        public int Amount { get; private set; } = 0;
        public ItemTypes ItemType { get; private set; }

        public void Initialize(InventoryItemSO inventoryItemSO, int amount = 0)
        {
            if (inventoryItemSO != null)
            {
                ItemType = inventoryItemSO.ItemType;
                Amount = amount;

                _itemAmountText.text = $"x {Amount}";
                _itemImage.sprite = inventoryItemSO.ItemImage;
            }
        }

        public void AddAmount(int amount)
        {
            Amount += amount;
            _itemAmountText.text = $"x {Amount}";
        }
    }
}
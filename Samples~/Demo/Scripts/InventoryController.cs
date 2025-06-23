using System.Collections.Generic;
using UnityEngine;


namespace Ethix.Demo
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private Transform _inventoryPanel;
        [SerializeField] private List<InventoryItemSO> _inventoryItemsSO;
        [SerializeField] private GameObject _inventoryItemPrefab;

        public static InventoryController Instance { get; private set; }
        public List<InventoryItem> InventoryItems { get; private set; } = new();

        private List<Transform> _inventorySlots = new();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            LoadPlayerItems();
        }

        private void LoadPlayerItems()
        {
            foreach (var itemSO in _inventoryItemsSO)
            {
                var item = Instantiate(_inventoryItemPrefab, _inventoryPanel).GetComponent<InventoryItem>();
                item.Initialize(itemSO);
                AddToInventory(itemSO, 0, item);
            }
            Debug.Log("Player items loaded into inventory.");
        }

        public void AddToInventory(InventoryItemSO inventoryItemSO, int amount, InventoryItem item = null)
        {
            Debug.Log($"{GetType().Name} - {System.DateTime.Now:HH:mm:ss}: adding {inventoryItemSO.ItemName} to inventory.");


            if (item == null && !InventoryItems.Exists(i => i.ItemType == inventoryItemSO.ItemType))
            {
                Debug.Log($"{GetType().Name} - {System.DateTime.Now:HH:mm:ss}: item is null, creating a new InventoryItem instance.");
                //this is probably coming from the shop/cart
                item = Instantiate(_inventoryItemPrefab, _inventoryPanel).GetComponent<InventoryItem>();
                item.Initialize(inventoryItemSO, amount);
            }

            if (!InventoryItems.Exists(i => i.ItemType == inventoryItemSO.ItemType))
            {
                Debug.Log($"{GetType().Name} - {System.DateTime.Now:HH:mm:ss}: item does not exist in inventory, adding it now.");
                InventoryItems.Add(item);

                foreach (var slot in _inventorySlots)
                {
                    if (slot.childCount == 0)
                    {
                        item.transform.SetParent(slot);
                        item.transform.localPosition = Vector3.zero;
                        _inventorySlots.Add(item.transform);
                        break;
                    }
                }
                Debug.Log($"Item {item.name} added to inventory.");
            }
            else
            {
                var existingItem = InventoryItems.Find(i => i.ItemType == inventoryItemSO.ItemType);
                if (existingItem != null)
                {
                    existingItem.AddAmount(amount);
                    Debug.Log($"Item {inventoryItemSO.ItemName} already exists in inventory. Updated quantity to {existingItem.Amount}.");
                }
                else
                {
                    Debug.LogError($"Item {inventoryItemSO.ItemName} not found in inventory items list.");
                }
            }
        }
    }
}
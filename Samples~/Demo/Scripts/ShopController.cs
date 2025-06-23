using System.Collections.Generic;
using UnityEngine;

namespace Ethix.Demo
{
    public class ShopController : MonoBehaviour
    {
        [SerializeField] private Transform _shopPanel;
        [SerializeField] private List<ShopItemSO> _shopItems;
        void Start()
        {
            LoadShopItems();
        }

        private void LoadShopItems()
        {
            foreach (var shopItem in _shopItems)
            {
                var shopItemObject = Instantiate(shopItem.ShopItemPrefab, _shopPanel);
                var shopItemComponent = shopItemObject.GetComponent<ShopItem>();
                if (shopItemComponent != null)
                {
                    shopItemComponent.Initialize(shopItem);
                }
            }
        }
    }
}
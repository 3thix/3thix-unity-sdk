using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ethix.Demo
{
    public class CartItem : MonoBehaviour
    {
        [SerializeField] private Text _itemAmountText;
        [SerializeField] private Text _amountInCart;
        [SerializeField] private Image _itemImage;
        [SerializeField] private Button _removeButton;
        public int AmountInCart { get; private set; } = 0;
        public ShopItemSO ShopItemSO { get; private set; }

        void Start()
        {
            _removeButton.onClick.AddListener(OnRemoveButtonClicked);
        }

        public void Initialize(ShopItemSO shopItemSO)
        {
            if (shopItemSO != null)
            {
                ShopItemSO = shopItemSO;
                AmountInCart = 1;
                _itemAmountText.text = $"x {shopItemSO.Amount}";
                _amountInCart.text = $"x {AmountInCart}";
                _itemImage.sprite = shopItemSO.ItemImage;
            }
        }

        public void AddAmountInCart(int amount)
        {
            AmountInCart += amount;
            _amountInCart.text = $"x {AmountInCart}";
        }

        public void OnRemoveButtonClicked()
        {
            CartController.Instance.RemoveFromCart(this);
            Destroy(gameObject);
        }
    }
}
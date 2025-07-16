using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Ethix.EthixData;
using static Ethix.EthixManager;

namespace Ethix.Demo
{
    public class CartController : MonoBehaviour
    {
        public static CartController Instance { get; private set; }
        [SerializeField] private Text _cartTotalText;
        [SerializeField] private GameObject _cartItemPrefab;
        [SerializeField] private Transform _cartPanel;
        [SerializeField] private Button _checkoutButton;
        public List<CartItem> CartItems { get; private set; } = new();
        public List<ItemTypes> ShopItemsSO { get; private set; } = new();

        private float _cartTotal = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _checkoutButton.onClick.AddListener(OnCheckoutButtonClicked);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void AddToCart(ShopItemSO item)
        {
            if (item == null)
            {
                Debug.LogError("Attempted to add a null item to the cart.");
                return;
            }

            if (!ShopItemsSO.Contains(item.ItemType))
            {
                //Instantiate a new CartItem prefab and initialize it
                var newItem = Instantiate(_cartItemPrefab, _cartPanel).GetComponent<CartItem>();
                if (newItem != null)
                {
                    newItem.Initialize(item);
                    _cartTotal += item.Price;
                    _cartTotalText.text = $"Total: ${_cartTotal:F2}";

                    if (!CartItems.Contains(newItem))
                        CartItems.Add(newItem);

                    if (!ShopItemsSO.Contains(item.ItemType))
                        ShopItemsSO.Add(item.ItemType);

                    Debug.Log($"{GetType().Name} - {System.DateTime.Now:HH:mm:ss}: Added {item.ItemName} to cart. Total: ${_cartTotal:F2}");
                }
                else
                {
                    Debug.LogError("CartItem component not found on the instantiated prefab.");
                    return;
                }
            }
            else
            {
                //Item is already in the cart, find it and update the amount
                var existingItem = CartItems.Find(cartItem => cartItem.ShopItemSO.ItemType == item.ItemType);
                if (existingItem != null)
                {
                    existingItem.AddAmountInCart(1);
                    _cartTotal += item.Price;
                    _cartTotalText.text = $"Total: ${_cartTotal:F2}";

                    Debug.Log($"{GetType().Name} - {System.DateTime.Now:HH:mm:ss}: Updated {item.ItemName} in cart. Total: ${_cartTotal:F2}");
                }
                else
                {
                    Debug.LogError("Existing cart item not found for the given ShopItemSO.");
                }
            }
        }

        public void RemoveFromCart(CartItem item)
        {
            if (item == null)
            {
                Debug.LogError("Attempted to remove a null item from the cart.");
                return;
            }

            var existingItem = CartItems.Find(cartItem => cartItem.ShopItemSO == item.ShopItemSO);
            if (existingItem != null)
            {
                _cartTotal -= item.ShopItemSO.Price * item.AmountInCart;
                _cartTotalText.text = $"Total: ${_cartTotal:F2}";
                CartItems.Remove(existingItem);
                ShopItemsSO.Remove(existingItem.ShopItemSO.ItemType);
                Destroy(existingItem.gameObject);
                Debug.Log($"{GetType().Name} - {System.DateTime.Now:HH:mm:ss}: Removed {item.ShopItemSO.ItemName} from cart. Total: ${_cartTotal:F2}");
            }
            else
            {
                Debug.LogError("Item to remove not found in the cart.");
            }
        }

        private void OnCheckoutButtonClicked()
        {
            if (CartItems.Count == 0)
            {
                Debug.LogWarning("Cart is empty. Cannot proceed to checkout.");
                return;
            }

            // Here you would typically handle the checkout process, such as creating a payment request.
            foreach (var item in CartItems)
            {
                EthixManager.Instance.AddProductToPaymentCart(item.ShopItemSO.ItemName, item.AmountInCart, item.ShopItemSO.Price.ToString("F2"));
            }

            EthixManager.Instance.CreatePayment(Rails.CREDIT_CARD, Currencies.USD, OnPaymentSuccess, OnPaymentFailure);
        }

        private void OnPaymentSuccess(PaymentDetailsResponse response)
        {
            // Handle successful payment, e.g., show confirmation UI, clear cart, etc.

            foreach (var item in CartItems)
            {
                Debug.Log($"Purchased {item.ShopItemSO.ItemName} x{item.AmountInCart} for ${item.ShopItemSO.Price * item.AmountInCart:F2}");
                for (int i = 0; i < item.AmountInCart; i++)
                {
                    InventoryController.Instance.AddToInventory(item.ShopItemSO.InventoryItemSO, item.ShopItemSO.Amount);
                }
            }
            while (CartItems.Count > 0)
            {
                RemoveFromCart(CartItems[0]);
            }
        }

        private void OnPaymentFailure(ErrorResponse error)
        {
            if (error.error_code == "WEB_BROWSER_CLOSED")
            {
                // Handle case where the web browser was closed before payment completion
                Debug.LogWarning("Payment process was cancelled by the user.");
                return;
            }
            else
            {
                // Handle payment failure, e.g., show error message to the user
                Debug.LogError($"Payment failed: {error.message}");
            }
        }
    }
}
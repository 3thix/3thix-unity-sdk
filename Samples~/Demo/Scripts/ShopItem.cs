using UnityEngine;
using UnityEngine.UI;

namespace Ethix.Demo
{
    public class ShopItem : MonoBehaviour
    {
        [SerializeField] private Text _itemAmountText;
        [SerializeField] private Text _itemPriceText;
        [SerializeField] private Image _itemImage;
        [SerializeField] private Button _buyButton;
        private ShopItemSO _shopItemSO;

        public void Start()
        {
            _buyButton.onClick.AddListener(OnBuyButtonClicked);
        }

        public void Initialize(ShopItemSO shopItemSO)
        {
            _shopItemSO = shopItemSO;
            if (_shopItemSO != null)
            {
                _itemAmountText.text = $"x {_shopItemSO.Amount}";
                _itemPriceText.text = $"${_shopItemSO.Price}";
                _itemImage.sprite = _shopItemSO.ItemImage;
            }
            else
            {
                Debug.LogError("ShopItemSO is null. Cannot initialize ShopItem.");
            }
        }

        public void OnBuyButtonClicked()
        {
            CartController.Instance.AddToCart(_shopItemSO);
        }
    }
}
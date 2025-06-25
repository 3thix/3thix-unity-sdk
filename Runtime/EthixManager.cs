using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
using VoltstroStudios.UnityWebBrowser;
#endif
using static Ethix.EthixData;

namespace Ethix
{
    public class EthixManager : MonoBehaviour
    {
        public static EthixManager Instance { get; private set; }

        [SerializeField] private string _thirdPartyId = "";
        [SerializeField] private string _sandboxApiKey = "";
        private List<PaymentRequestItem> _paymentRequestCart = new();
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
        private WebBrowserUIBasic _webBrowserUI;
        private bool _isWebBrowserReady;
#endif

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            _webBrowserUI = FindFirstObjectByType<WebBrowserUIBasic>(FindObjectsInactive.Include);
            if (_webBrowserUI == null)
                Debug.LogError("WebBrowserUIBasic not found in the scene. Please ensure it is present.");
            else
            {
                _webBrowserUI.browserClient.OnClientConnected += () =>
                {
                    _isWebBrowserReady = true;
                    Debug.Log("WebBrowserUIBasic client connected.");
                };
                Debug.Log("WebBrowserUIBasic found in the scene.");
            }
#endif
        }

        public void CreatePayment(Rails rail, Currencies currency, Action<PaymentDetailsResponse> onPaymentSuccess = null, Action<ErrorResponse> onPaymentFailure = null)
        {
            var amount = 0.0f;
            foreach (var item in _paymentRequestCart)
            {
                if (float.TryParse(item.price_unit, out float price))
                {
                    amount += price * item.qty_unit;
                }
                else
                {
                    Debug.LogError($"Invalid price format for product {item.product_name}: {item.price_unit}");
                }
            }

            PaymentRequest paymentRequest = new()
            {
                rail = rail == Rails.AVAX_C ? "AVAX-C" : rail.ToString(),
                currency = currency.ToString(),
                amount = amount.ToString("F2"), // Format to 2 decimal places
                cart = _paymentRequestCart
            };

            ////////
            // Here you would typically send this data to your backend so you can reference it later
            ////////
            Debug.Log($"Creating Payment Request: {paymentRequest.rail}, {paymentRequest.currency}, Amount: {paymentRequest.amount}, items: {_paymentRequestCart.Count}");
            Debug.Log("For Items:");
            foreach (var item in _paymentRequestCart)
            {
                Debug.Log($"- {item.product_name}, Qty: {item.qty_unit}, Price: {item.price_unit}");
            }

            StartCoroutine(SendPaymentRequest(paymentRequest, onPaymentSuccess, onPaymentFailure));
        }

        private IEnumerator SendPaymentRequest(PaymentRequest paymentRequest, Action<PaymentDetailsResponse> onPaymentSuccess = null, Action<ErrorResponse> onPaymentFailure = null)
        {
            var url = SandboxCreatePaymentUrl;
            var json = JsonConvert.SerializeObject(paymentRequest);
            var apiKey = _sandboxApiKey;

            using var www = new UnityEngine.Networking.UnityWebRequest(url, "POST");

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("X-Api-Key", $"{apiKey}");

            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError ||
                www.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error sending payment request: {www.error}");
            }
            else
            {
                var response = JsonConvert.DeserializeObject<PaymentRequestResponse>(www.downloadHandler.text);

                ////////
                // Here you would typically send the PaymentRequest and PaymentRequestResponse to your backend so you can reference it later
                // For example, if the player doesn't pay right away, you can still reference the order and invoice IDs later and know what the player wanted to buy
                ////////

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                // Open the in-game web browser UI and load the payment URL
                _webBrowserUI.transform.root.gameObject.SetActive(true);
                yield return new WaitUntil(() => _isWebBrowserReady);
                _webBrowserUI.browserClient?.LoadUrl($"{SandboxPaymentPayUrl}{response.invoice_id}");
#else
                Application.OpenURL($"{SandboxPaymentPayUrl}{response.invoice_id}");
#endif
                StartCoroutine(PollPaymentResult(response.invoice_id, onPaymentSuccess, onPaymentFailure));
            }

            www.Dispose();
            _paymentRequestCart.Clear(); // Clear the cart after sending the request
        }

        private IEnumerator PollPaymentResult(string invoiceId, Action<PaymentDetailsResponse> onPaymentSuccess = null, Action<ErrorResponse> onPaymentFailure = null)
        {
            var paymentDetails = new PaymentDetailsBody
            {
                id = invoiceId
            };

            var body = JsonConvert.SerializeObject(paymentDetails);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);

            while (true)
            {
                using var www = new UnityEngine.Networking.UnityWebRequest(SandboxPaymentResultUrl, "POST");
                www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError ||
                    www.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error polling payment result: {www.error}");
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(www.downloadHandler.text);
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                    _webBrowserUI.transform.root.gameObject.SetActive(false);
#endif
                    onPaymentFailure?.Invoke(errorResponse);
                    www.Dispose();
                    yield break;
                }

                var response = JsonConvert.DeserializeObject<PaymentDetailsResponse>(www.downloadHandler.text);
                if (response.invoice.status == "PAID") //if paid or the web browser is closed
                {
                    Debug.Log($"Payment completed for Invoice ID: {response.invoice.id}");
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                    _webBrowserUI.transform.root.gameObject.SetActive(false);
#endif
                    onPaymentSuccess?.Invoke(response);
                    www.Dispose();
                    break;
                }
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                else if (_webBrowserUI.transform.root.gameObject.activeSelf == false)
                {
                    Debug.Log("Web browser closed or payment not completed.");
                    var errorResponse = new ErrorResponse
                    {
                        message = "Payment not completed or web browser closed.",
                        error_code = "WEB_BROWSER_CLOSED"
                    };
                    onPaymentFailure?.Invoke(errorResponse);
                    _webBrowserUI.transform.root.gameObject.SetActive(false);
                    www.Dispose();
                    break;
                }
#endif
                www.Dispose();
                yield return new WaitForSeconds(5f); // Poll every 5 seconds
            }
        }

        public void AddProductToCart(string productName, int quantity, string price)
        {
            _paymentRequestCart.Add(new PaymentRequestItem
            {
                product_name = productName,
                qty_unit = quantity,
                price_unit = price
            });
        }

        public void CreatePurchaseOrder()
        {
            //TODO
        }

        public void CreatePurchaseOrderByUser()
        {
            //TODO
        }
    }
}
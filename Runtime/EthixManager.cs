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
        [SerializeField] private string _productionApiKey = "";
        [SerializeField] private bool _isSandbox = true;
        private List<PaymentRequestItem> _paymentRequestCart = new();
        private List<PurchaseRequestItem> _purchaseRequestCart = new();

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
        private WebBrowserUIBasic _webBrowserUI;
        private bool _isWebBrowserReady;
#endif

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
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

        public void SyncUserWithEthix(string playerId, string firstName = "", string lastName = "", string email = "", string phone = "", Action<SyncUserResponse> onRequestSuccess = null, Action<ErrorResponse> onRequestFailure = null)
        {
            SyncUserRequest syncUserRequest = new()
            {
                users = new List<SyncUserRequestData>
                {
                    new SyncUserRequestData
                    {
                        third_party_id = playerId,
                        first_name = firstName,
                        last_name = lastName,
                        email = email,
                        phone = phone
                    }
                }
            };

            StartCoroutine(SendSyncUserRequest(syncUserRequest, response =>
            {
                Debug.Log($"User synced successfully");
                onRequestSuccess?.Invoke(response);
            }, error =>
            {
                Debug.LogError($"Error syncing user: {error.message}");
                onRequestFailure?.Invoke(error);
            }));
        }

        private IEnumerator SendSyncUserRequest(SyncUserRequest syncUserRequest, Action<SyncUserResponse> onSuccess, Action<ErrorResponse> onFailure)
        {
            var url = _isSandbox ? SandboxSyncUserUrl : ProductionSyncUserUrl;
            var json = JsonConvert.SerializeObject(syncUserRequest);
            var apiKey = _isSandbox ? _sandboxApiKey : _productionApiKey;

            using var www = new UnityEngine.Networking.UnityWebRequest(url, "POST");

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("X-Api-Key", apiKey);

            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError ||
                www.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error syncing user: {www.error}");
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(www.downloadHandler.text);
                onFailure?.Invoke(errorResponse);
            }
            else
            {
                var response = JsonConvert.DeserializeObject<SyncUserResponse>(www.downloadHandler.text);
                onSuccess?.Invoke(response);
            }

            www.Dispose();
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
            var url = _isSandbox ? SandboxCreatePaymentUrl : ProductionCreatePaymentUrl;
            var json = JsonConvert.SerializeObject(paymentRequest);
            var apiKey = _isSandbox ? _sandboxApiKey : _productionApiKey;

            using var www = new UnityEngine.Networking.UnityWebRequest(url, "POST");

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("X-Api-Key", apiKey);

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

                var urlPayment = _isSandbox ? SandboxPaymentPayUrl : ProductionPaymentPayUrl;
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                // Open the in-game web browser UI and load the payment URL
                _webBrowserUI.transform.root.gameObject.SetActive(true);
                yield return new WaitUntil(() => _isWebBrowserReady);
                _webBrowserUI.browserClient?.LoadUrl($"{urlPayment}{response.invoice_id}");
#else
                Application.OpenURL($"{urlPayment}{response.invoice_id}");
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

            var url = _isSandbox ? SandboxPaymentResultUrl : ProductionPaymentResultUrl;

            var body = JsonConvert.SerializeObject(paymentDetails);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);

            float timeout = 300f; // 5 minutes timeout - may take time to link ACH accounts
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                using var www = new UnityEngine.Networking.UnityWebRequest(url, "POST");
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
                else if (_webBrowserUI != null && _webBrowserUI.transform.root.gameObject.activeSelf == false)
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
                yield return new WaitForSeconds(1f); // Poll every 1 second
                elapsed += 1f;
            }
        }

        public void AddProductToPaymentCart(string productName, int quantity, string price)
        {
            _paymentRequestCart.Add(new PaymentRequestItem
            {
                product_name = productName,
                qty_unit = quantity,
                price_unit = price
            });
        }

        public void AddProductToPurchaseCart(Rails rail, Currencies currency, string price)
        {
            _purchaseRequestCart.Add(new PurchaseRequestItem
            {
                rail = rail.ToString(),
                currency = currency.ToString(),
                amount = price
            });
        }

        public void CreatePurchaseOrder(string entityId, Currencies destinationCurrency, Action<PurchaseDetailsResponse> onPurchasePaymentSuccess = null, Action<ErrorResponse> onPurchasePaymentFailure = null)
        {
            PurchaseRequest purchaseRequest = new()
            {
                fulfillment_entity_id = entityId,
                destination_currency = destinationCurrency.ToString(),
                carts = _purchaseRequestCart
            };

            ////////
            // Here you would typically send this data to your backend so you can reference it later
            ////////
            Debug.Log($"Creating Purchase Order: {purchaseRequest.fulfillment_entity_id}, {purchaseRequest.destination_currency}, # items: {_purchaseRequestCart.Count}");
            Debug.Log("For Items:");
            foreach (var item in _purchaseRequestCart)
            {
                Debug.Log($"- {item.rail} {item.currency}, Amount: {item.amount}");
            }

            StartCoroutine(SendPurchaseOrderRequest(purchaseRequest, onPurchasePaymentSuccess, onPurchasePaymentFailure));
        }


        private IEnumerator SendPurchaseOrderRequest(PurchaseRequest purchaseRequest, Action<PurchaseDetailsResponse> onPurchasePaymentSuccess = null, Action<ErrorResponse> onPurchasePaymentFailure = null)
        {
            var url = _isSandbox ? SandboxCreatePurchaseUrl : ProductionCreatePurchaseUrl;
            var json = JsonConvert.SerializeObject(purchaseRequest);
            var apiKey = _isSandbox ? _sandboxApiKey : _productionApiKey;

            using var www = new UnityEngine.Networking.UnityWebRequest(url, "POST");

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("X-Api-Key", apiKey);

            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError ||
                www.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(www.downloadHandler.text);
                Debug.LogError($"Error sending purchase order request: {errorResponse.message}");
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                    _webBrowserUI.transform.root.gameObject.SetActive(false);
#endif
                onPurchasePaymentFailure?.Invoke(errorResponse);
            }
            else
            {
                var response = JsonConvert.DeserializeObject<PurchaseRequestResponse>(www.downloadHandler.text);

                ////////
                // Here you would typically send the PurchaseRequest and PurchaseRequestResponse to your backend so you can reference it later
                // For example, if the player doesn't pay right away, you can still reference the order and invoice IDs later and know what the player wanted to buy
                ////////

                var urlPurchase = _isSandbox ? SandboxPurchasePayUrl : ProductionPurchasePayUrl;
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                // Open the in-game web browser UI and load the payment URL
                _webBrowserUI.transform.root.gameObject.SetActive(true);
                yield return new WaitUntil(() => _isWebBrowserReady);
                _webBrowserUI.browserClient?.LoadUrl($"{urlPurchase}{response.invoice_id}");
#else
                Application.OpenURL($"{urlPurchase}{response.invoice_id}");
#endif
                StartCoroutine(PollPurchaseResult(response.invoice_id, onPurchasePaymentSuccess, onPurchasePaymentFailure));
            }

            www.Dispose();
            _purchaseRequestCart.Clear(); // Clear the cart after sending the request
        }

        private IEnumerator PollPurchaseResult(string invoiceId, Action<PurchaseDetailsResponse> onPaymentSuccess = null, Action<ErrorResponse> onPaymentFailure = null)
        {
            var purchaseDetails = new PurchaseDetailsBody
            {
                id = invoiceId
            };

            var url = _isSandbox ? SandboxPurchaseResultUrl : ProductionPurchaseResultUrl;

            var body = JsonConvert.SerializeObject(purchaseDetails);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);

            float timeout = 300f; // 5 minutes timeout - may take time to link ACH accounts
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                using var www = new UnityEngine.Networking.UnityWebRequest(url, "POST");
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

                var response = JsonConvert.DeserializeObject<PurchaseDetailsResponse>(www.downloadHandler.text);
                if (response.invoice.status == "PAID" || response.invoice.status == "PAYMENT_SUBMITTED") //if paid or payment submitted (ACH), or the web browser is closed
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
                else if (_webBrowserUI != null && _webBrowserUI.transform.root.gameObject.activeSelf == false)
                {
                    Debug.Log("Web browser closed or payment not completed.");
                    var errorResponse = new ErrorResponse
                    {
                        message = "Purchase not completed or web browser closed.",
                        error_code = "WEB_BROWSER_CLOSED"
                    };
                    onPaymentFailure?.Invoke(errorResponse);
                    _webBrowserUI.transform.root.gameObject.SetActive(false);
                    www.Dispose();
                    break;
                }
#endif
                www.Dispose();
                yield return new WaitForSeconds(1f); // Poll every 1 second
                elapsed += 1f;
            }
        }

        public void CreatePurchaseOrderByUser()
        {
            //TODO
        }

        public void GetPlayerAccounts(Currencies currency, string entityId, Action<PlayerAccountsResponse> onSuccess = null, Action<ErrorResponse> onFailure = null)
        {
            StartCoroutine(SendPlayerAccountsRequest(currency, entityId, onSuccess, onFailure));
        }

        private IEnumerator SendPlayerAccountsRequest(Currencies currency, string entityId, Action<PlayerAccountsResponse> onSuccess, Action<ErrorResponse> onFailure)
        {
            var url = _isSandbox ? SandboxPlayerAccountsUrl : ProductionPlayerAccountsUrl;
            var apiKey = _isSandbox ? _sandboxApiKey : _productionApiKey;

            var body = JsonConvert.SerializeObject(new PlayerAccountsRequest
            {
                currency = currency.ToString(),
                entity_id = entityId
            });

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);

            using var www = new UnityEngine.Networking.UnityWebRequest(url, "POST");
            www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("X-Api-Key", apiKey);

            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError ||
                www.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(www.downloadHandler.text);
                onFailure?.Invoke(errorResponse);
            }
            else
            {
                var response = JsonConvert.DeserializeObject<PlayerAccountsResponse>(www.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            www.Dispose();
        }
    }
}
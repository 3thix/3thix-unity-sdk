# Unity SDK for 3thix Payment Integration

## Overview

`EthixManager.cs` is a Unity C# script that integrates the [3thix payment system](https://3thix.com/) into Unity games. It manages payment requests, cart operations, and payment status polling, with support for both WebGL and non-WebGL platforms. The script uses a singleton pattern to ensure a single instance handles all payment-related operations.

## Features

- **Cart Management**: Add products to a payment cart and calculate total amounts.
- **Payment Processing**: Creates and sends payment requests to the 3thix API.
- **Platform Support**:
  - Non-WebGL: Uses an in-game [Unity Web Browser](https://projects.voltstro.dev/UnityWebBrowser/latest/) for payment UI.
  - WebGL: Opens payment URLs in the default browser.
- **Polling**: Monitors payment status with callbacks for success or failure.
- **Error Handling**: Logs errors and provides detailed failure responses via callbacks.

## Requirements

- **Unity**: Compatible with Unity 2021.3 or later.
- **Dependencies**:
  - `Newtonsoft.Json` for JSON serialization/deserialization.
  - `UnityEngine.Networking` for HTTP requests.  
- **Non-WebGL Platforms**: Requires [Unity Web Browser](https://projects.voltstro.dev/UnityWebBrowser/latest/)

## Installation
- Add the following in Package Manager:

```
https://github.com/3thix/3thix-unity-sdk
```

## Setup

1. **Add to Scene**:
   - Attach `EthixManager.cs` to a GameObject in your Unity scene.
   - Configure the serialized fields in the Unity Inspector:
     - `_thirdPartyId`: Your 3thix third-party identifier (e.g., `my-company`).
     - `_sandboxApiKey`: Your 3thix API key.
2. **Non-WebGL Setup**:
   - Ensure a `WebBrowserUIBasic` component is present in the scene (can be inactive).
3. **Backend Integration**:
   - The script assumes a backend to store `PaymentRequest` and `PaymentRequestResponse` for persistence (not implemented in the script, and not needed for testing).

## Usage

### Adding Products to Cart

Add items to the payment cart before initiating a payment:

```csharp
EthixManager.Instance.AddProductToCart("Health Potion", 2, "5.00"); //Item Name, Quantity, Price
EthixManager.Instance.AddProductToCart("Mana Crystal", 1, "10.00"); //Item Name, Quantity, Price
```

### Creating a Payment

Initiate a payment with the cart contents:

```csharp
EthixManager.Instance.CreatePayment(
    rail: Rails.AVAX_C,
    currency: Currencies.USD,
    onPaymentSuccess: (response) => Debug.Log($"Payment successful for Invoice ID: {response.invoice.id}"),
    onPaymentFailure: (error) => Debug.LogError($"Payment failed: {error.message}")
);
```

### Key Methods

| Method | Description |
| --- | --- |
| `AddProductToCart(string productName, int quantity, string price)` | Adds a product to the payment cart. |
| `CreatePayment(Rails rail, Currencies currency, Action<PaymentDetailsResponse> onPaymentSuccess, Action<ErrorResponse> onPaymentFailure)` | Creates and sends a payment request to the 3thix API. |
| `SendPaymentRequest` (Coroutine) | Sends HTTP POST requests to the 3thix API and opens the payment URL. |
| `PollPaymentResult` (Coroutine) | Polls payment status every 5 seconds until completion or failure. |

## Platform-Specific Behavior

- **Non-WebGL**:
  - Uses `WebBrowserUIBasic` to display the payment UI in-game.
  - Waits for the browser to be ready before loading the payment URL.
  - Deactivates the browser UI on payment completion or failure.
- **WebGL**:
  - Opens the payment URL in the default browser using `Application.OpenURL`.

## Configuration

- **Serialized Fields**:
  - `_thirdPartyId`: Set to your 3thix third-party ID (e.g., `my-company`).
  - `_sandboxApiKey`: Set to your 3thix API key.
- **Constants** (defined in `Ethix.EthixData`):
  - `SandboxCreatePaymentUrl`: URL for creating payment requests.
  - `SandboxPaymentPayUrl`: URL for the payment page.
  - `SandboxPaymentResultUrl`: URL for polling payment status.

## Example Workflow

1. Add products to the cart:

   ```csharp
   EthixManager.Instance.AddProductToCart("Sword", 1, "15.00");
   ```

2. Create a payment:

   ```csharp
   EthixManager.Instance.CreatePayment(
       rail: Rails.AVAX_C,
       currency: Currencies.USD,
       onPaymentSuccess: response => Debug.Log("Payment succeeded!"),
       onPaymentFailure: error => Debug.LogError($"Payment failed: {error.message}")
   );
   ```

3. The script sends the request, opens the payment URL, and polls for status.
4. On success, the `onPaymentSuccess` callback is invoked; on failure or browser closure (non-WebGL), the `onPaymentFailure` callback is invoked.

## Notes

- **Backend Integration**: The script includes comments indicating where `PaymentRequest` and `PaymentRequestResponse` should be sent to your backend for persistence.
- **Error Handling**: Errors are logged to the Unity console, and failure callbacks provide detailed error responses.
- **Unimplemented Methods**:
  - `CreatePurchaseOrder()`: Placeholder for creating purchase orders.
  - `CreatePurchaseOrderByUser()`: Placeholder for user-initiated purchase orders.

## Troubleshooting

- **WebBrowserUIBasic Not Found**:
  - Ensure a `WebBrowserUIBasic` component is in the scene for non-WebGL builds.
- **Invalid Price Format**:
  - Ensure prices in `AddProductToCart` are valid numeric strings (e.g., `"5.00"`).
- **API Errors**:
  - Verify `_thirdPartyId` and `_sandboxApiKey` match your 3thix account credentials.

## License

This script is part of the 3thix Unity SDK. Refer to the [3thix documentation](https://3thix.com/) for licensing details.

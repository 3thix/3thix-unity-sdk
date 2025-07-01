using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Ethix
{
    public class EthixData
    {

        public const string SandboxCreatePaymentUrl = "https://sandbox-api.3thix.com/order/payment/create";
        public const string SandboxPaymentPayUrl = "https://sandbox-pay.3thix.com/?invoiceId=";
        public const string SandboxPaymentResultUrl = "https://sandbox-api.3thix.com/invoice/details/get";
        public const string SandboxSyncUserUrl = "https://sandbox-api.3thix.com/entity/game/user/autosync";

        public const string ProductionCreatePaymentUrl = "https://api.3thix.com/order/payment/create";
        public const string ProductionPaymentPayUrl = "https://pay.3thix.com/?invoiceId=";
        public const string ProductionPaymentResultUrl = "https://api.3thix.com/invoice/details/get";
        public const string ProductionSyncUserUrl = "https://api.3thix.com/entity/game/user/autosync";

        public enum Rails
        {
            CREDIT_CARD,
            ACH,
            KAKAOPAY,
            ETHEREUM,
            POLYGON,
            AVAX_C, //we convert to AVAX-C when creating a request
            TELOS,
            ARBITRUM,
            BNB_CHAIN,
            SKALE,
            PAYTM,
            PAYNOW,
            KONBINI,
            AUPAY,
            SEI,
        }

        public enum Currencies
        {
            USD,
            BRL,
            CAD,
            CNY,
            EUR
        }


        [Serializable]
        public struct SyncUserRequest
        {
            public string third_party_id { get; set; }
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
        }


        [Serializable]
        public struct SyncUserResponse
        {
            public string entity_id { get; set; }
            public string third_party_id { get; set; }
        }

        [Serializable]
        public struct PaymentRequest
        {
            public string rail { get; set; } //"CREDIT_CARD"
            public string currency { get; set; } // "USD"
            public string amount { get; set; } // "10.00"
            public List<PaymentRequestItem> cart { get; set; }
        }

        [Serializable]
        public struct PaymentRequestItem
        {
            public string product_name { get; set; } // "Test Product"
            public int qty_unit { get; set; } // 1
            public string price_unit { get; set; } // "10.00"
        }

        [Serializable]
        public struct PaymentRequestResponse
        {
            public string order_id { get; set; }
            public string invoice_id { get; set; }
            public string invoice_amount { get; set; }
            public string invoice_currency { get; set; }
        }

        [Serializable]
        public class Invoice
        {
            public string id { get; set; }
            public string order_id { get; set; }
            public string issuer_entity_id { get; set; }
            public string currency { get; set; }
            public string amount { get; set; }
            public string remaining_amount { get; set; }
            public string amount_paid { get; set; }
            public string fees { get; set; }
            public string remaining_fees { get; set; }
            public string fees_paid { get; set; }
            public string total { get; set; }
            public string total_remaining { get; set; }
            public string status { get; set; }
            public string created_at { get; set; }
        }

        [Serializable]
        public class Order
        {
            public string id { get; set; }
            public object fulfillment_game_user_id { get; set; }
            public object fulfillment_entity_id { get; set; }
            public string issuer_entity_id { get; set; }
            public string type { get; set; }
            public string destination_currency { get; set; }
            public string destination_amount { get; set; }
            public string destination_fees { get; set; }
            public string destination_total { get; set; }
            public string status { get; set; }
            public string created_at { get; set; }
        }

        [Serializable]
        public class PaymentDetailsResponse
        {
            public Invoice invoice { get; set; }
            public Order order { get; set; }
        }

        [Serializable]
        public class PaymentDetailsBody
        {
            public string id { get; set; } // "invoice_id"
        }

        [Serializable]
        public class ErrorResponse
        {
            public string message { get; set; }
            public string error_code { get; set; }
        }
    }
}
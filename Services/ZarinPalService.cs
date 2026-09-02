using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Services.Abstractions;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public sealed class ZarinPalService : IZarinPalService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ZarinPalService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<(bool Success, string Authority, string Message, string GatewayUrl)> RequestPaymentAsync(
            long amount, string description, string callbackUrl, string mobile = null, CancellationToken cancellationToken = default)
        {
            var merchantId = _configuration["ZarinPal:MerchantId"];
            var isSandbox = string.Equals(_configuration["ZarinPal:Sandbox"], "true", System.StringComparison.OrdinalIgnoreCase);
            var requestUrl = isSandbox
                ? "https://sandbox.zarinpal.com/pg/v4/payment/request.json"
                : "https://api.zarinpal.com/pg/v4/payment/request.json";
            var startPayBase = isSandbox
                ? "https://sandbox.zarinpal.com/pg/StartPay/"
                : "https://www.zarinpal.com/pg/StartPay/";

            if (string.IsNullOrWhiteSpace(merchantId) || merchantId == "YOUR_MERCHANT_ID")
            {
                var fakeAuthority = "A000000000000000000000000000000000000";
                return (true, fakeAuthority, "حالت تست بدون MerchantId", startPayBase + fakeAuthority);
            }

            var payload = new
            {
                merchant_id = merchantId,
                amount = amount,
                description = description,
                callback_url = callbackUrl,
                metadata = new { mobile }
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(requestUrl, content, cancellationToken);
                var body = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(body);
                var code = json["data"]?["code"]?.Value<int>() ?? json["errors"]?["code"]?.Value<int>() ?? -1;
                if (code == 100)
                {
                    var authority = json["data"]["authority"].ToString();
                    return (true, authority, "OK", startPayBase + authority);
                }

                var message = json["errors"]?["message"]?.ToString() ?? json["data"]?["message"]?.ToString() ?? body;
                return (false, null, message, null);
            }
            catch (System.Exception ex)
            {
                return (false, null, ex.Message, null);
            }
        }

        public async Task<(bool Success, string RefId, string Message)> VerifyPaymentAsync(
            string authority, long amount, CancellationToken cancellationToken = default)
        {
            var merchantId = _configuration["ZarinPal:MerchantId"];
            var isSandbox = string.Equals(_configuration["ZarinPal:Sandbox"], "true", System.StringComparison.OrdinalIgnoreCase);
            var verifyUrl = isSandbox
                ? "https://sandbox.zarinpal.com/pg/v4/payment/verify.json"
                : "https://api.zarinpal.com/pg/v4/payment/verify.json";

            if (string.IsNullOrWhiteSpace(merchantId) || merchantId == "YOUR_MERCHANT_ID")
                return (true, "TEST-REF-" + authority.Substring(0, 8), "تایید تستی");

            var payload = new
            {
                merchant_id = merchantId,
                amount = amount,
                authority = authority
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(verifyUrl, content, cancellationToken);
                var body = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(body);
                var code = json["data"]?["code"]?.Value<int>() ?? -1;
                if (code == 100 || code == 101)
                {
                    var refId = json["data"]?["ref_id"]?.ToString() ?? "";
                    return (true, refId, "OK");
                }

                var message = json["errors"]?["message"]?.ToString() ?? body;
                return (false, null, message);
            }
            catch (System.Exception ex)
            {
                return (false, null, ex.Message);
            }
        }
    }
}

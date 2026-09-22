using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    /// <summary>
    /// کلاینت هاب پرداخت نیکو سیستم آفرین (درگاه زرین‌پال روی دامنه نیکو).
    /// </summary>
    public sealed class NikooPaymentHubClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public NikooPaymentHubClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public bool IsEnabled =>
            string.Equals(_configuration["NikooPayment:Enabled"], "true", StringComparison.OrdinalIgnoreCase);

        public string BaseUrl => (_configuration["NikooPayment:BaseUrl"] ?? "https://nikoooo.ir").TrimEnd('/');
        public string AppId => _configuration["NikooPayment:AppId"] ?? "goodloan";
        public string ApiKey => _configuration["NikooPayment:ApiKey"] ?? "";

        public async Task<(bool Success, string Token, string PaymentUrl, string Message)> CreatePaymentAsync(
            long amountToman,
            string returnUrl,
            string description,
            string externalRef,
            string notifyUrl,
            string customerName,
            string customerMobile,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                return (false, null, null, "کلید API نیکو تنظیم نشده است");

            var payload = new
            {
                amount = amountToman,
                returnUrl,
                description,
                externalRef,
                notifyUrl,
                customerName,
                customerMobile
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/api/payments/create");
                request.Headers.TryAddWithoutValidation("X-App-Id", AppId);
                request.Headers.TryAddWithoutValidation("X-Api-Key", ApiKey);
                request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);

                if (json["success"]?.Value<bool>() != true)
                    return (false, null, null, json["message"]?.ToString() ?? "خطا در ساخت پرداخت نیکو");

                return (
                    true,
                    json["token"]?.ToString(),
                    json["paymentUrl"]?.ToString(),
                    "OK");
            }
            catch (Exception ex)
            {
                return (false, null, null, "ارتباط با درگاه نیکو برقرار نشد: " + ex.Message);
            }
        }

        public bool VerifyCallback(
            string token,
            string status,
            string amount,
            string externalRef,
            string refId,
            string timestamp,
            string signature)
        {
            return VerifySignature(ApiKey, token, status, amount, externalRef, refId, timestamp, signature);
        }

        public static bool VerifySignature(
            string apiKey,
            string token,
            string status,
            string amount,
            string externalRef,
            string refId,
            string timestamp,
            string signature,
            int maxAgeSeconds = 3600)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(signature))
                return false;

            if (!long.TryParse(timestamp, out var ts))
                return false;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(now - ts) > maxAgeSeconds)
                return false;

            var payload = string.Join("|", new[]
            {
                token ?? "",
                status ?? "",
                amount ?? "",
                externalRef ?? "",
                refId ?? "",
                timestamp ?? ""
            });

            return FixedTimeEquals(HmacSha256Hex(apiKey, payload), signature);
        }

        private static string HmacSha256Hex(string secret, string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? ""));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data ?? ""));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}

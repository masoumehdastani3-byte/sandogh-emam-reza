using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    /// <summary>
    /// کلاینت هاب پرداخت نیکو سیستم آفرین (درگاه زرین‌پال روی دامنه نیکو).
    /// </summary>
    public sealed class NikooPaymentHubClient
    {
        private readonly IConfiguration _configuration;

        public NikooPaymentHubClient(IConfiguration configuration)
        {
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

            var json = JsonConvert.SerializeObject(payload);
            var primary = BuildCreateUrl();
            var attempts = new List<(string Url, bool Loopback)> { (primary, false) };
            if (!string.Equals(primary, CanonicalCreateUrl, StringComparison.OrdinalIgnoreCase))
                attempts.Add((CanonicalCreateUrl, false));
            // صندوق و نیکو روی یک سرور هستند. درخواست به آدرس عمومی گاهی صفحه HTML می‌گیرد
            // (پروکسی سیستم، یا تبدیل POST به GET بعد از هدایت). اتصال به خود سرور این را دور می‌زند.
            attempts.Add((CanonicalCreateUrl, true));
            attempts.Add(("http://nikoooo.ir/api/payments/create", true));

            string lastDetail = null;
            foreach (var attempt in attempts)
            {
                try
                {
                    var result = await PostCreateAsync(attempt.Url, json, attempt.Loopback, cancellationToken);
                    if (result.Done)
                        return (result.Success, result.Token, result.PaymentUrl, result.Message);
                    lastDetail = result.Message;
                }
                catch (Exception ex)
                {
                    lastDetail = DescribeException(ex);
                }
            }

            return (false, null, null, "ارتباط با درگاه نیکو برقرار نشد: " + (lastDetail ?? "پاسخ نامعتبر"));
        }

        private const string CanonicalCreateUrl = "https://nikoooo.ir/api/payments/create";

        private string BuildCreateUrl()
        {
            var configured = BaseUrl;
            if (string.IsNullOrWhiteSpace(configured))
                configured = "https://nikoooo.ir";
            if (!configured.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !configured.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                configured = "https://" + configured.TrimStart('/');

            if (!Uri.TryCreate(configured, UriKind.Absolute, out var uri))
                return CanonicalCreateUrl;

            return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/') + "/api/payments/create";
        }

        private async Task<(bool Done, bool Success, string Token, string PaymentUrl, string Message)> PostCreateAsync(
            string url,
            string json,
            bool loopback,
            CancellationToken cancellationToken)
        {
            using var client = new HttpClient(CreateHandler(loopback)) { Timeout = TimeSpan.FromSeconds(20) };
            var current = url;

            for (var hop = 0; hop < 4; hop++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, current);
                request.Version = HttpVersion.Version11;
                request.Headers.TryAddWithoutValidation("X-App-Id", AppId);
                request.Headers.TryAddWithoutValidation("X-Api-Key", ApiKey);
                request.Headers.TryAddWithoutValidation("User-Agent", "GoodLoan-NikooPayment");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await client.SendAsync(request, cancellationToken);
                if (IsRedirect(response.StatusCode))
                {
                    var next = ResolveRedirect(current, response.Headers.Location);
                    if (next == null)
                        return (false, false, null, null, "هدایت درگاه نیکو نامعتبر بود");
                    current = next;
                    continue;
                }

                var body = await response.Content.ReadAsStringAsync();
                if (!LooksLikeJson(body))
                    return (false, false, null, null, DescribeHtml(response.StatusCode, body));

                JObject parsed;
                try
                {
                    parsed = JObject.Parse(body);
                }
                catch (JsonException)
                {
                    return (false, false, null, null, DescribeHtml(response.StatusCode, body));
                }

                if (parsed["success"]?.Value<bool>() != true)
                    return (true, false, null, null, parsed["message"]?.ToString() ?? "خطا در ساخت پرداخت نیکو");

                return (true, true, parsed["token"]?.ToString(), parsed["paymentUrl"]?.ToString(), "OK");
            }

            return (false, false, null, null, "هدایت درگاه نیکو بیش از حد تکرار شد");
        }

        private static SocketsHttpHandler CreateHandler(bool loopback)
        {
            var handler = new SocketsHttpHandler
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                SslOptions = new SslClientAuthenticationOptions
                {
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                }
            };

            if (!loopback)
                return handler;

            handler.ConnectCallback = async (context, token) =>
            {
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(IPAddress.Loopback, context.DnsEndPoint.Port, token).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            };
            return handler;
        }

        private static bool IsRedirect(HttpStatusCode status)
        {
            var code = (int)status;
            return code == 301 || code == 302 || code == 303 || code == 307 || code == 308;
        }

        private static string ResolveRedirect(string current, Uri location)
        {
            if (location == null || !Uri.TryCreate(current, UriKind.Absolute, out var currentUri))
                return null;

            var next = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
            if (next.Scheme != Uri.UriSchemeHttp && next.Scheme != Uri.UriSchemeHttps)
                return null;

            var sameHost = string.Equals(next.Host, currentUri.Host, StringComparison.OrdinalIgnoreCase)
                || string.Equals(next.Host, "nikoooo.ir", StringComparison.OrdinalIgnoreCase)
                || string.Equals(next.Host, "www.nikoooo.ir", StringComparison.OrdinalIgnoreCase);
            return sameHost ? next.ToString() : null;
        }

        private static bool LooksLikeJson(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return false;
            foreach (var ch in body.TrimStart('\uFEFF', ' ', '\t', '\r', '\n'))
                return ch == '{' || ch == '[';
            return false;
        }

        private static string DescribeHtml(HttpStatusCode status, string body)
        {
            var title = "";
            if (!string.IsNullOrEmpty(body))
            {
                var match = Regex.Match(body, "<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (match.Success)
                    title = Regex.Replace(match.Groups[1].Value, "\\s+", " ").Trim();
            }

            if (title.Length > 120)
                title = title.Substring(0, 120);

            var text = "پاسخ درگاه صفحه وب بود (" + (int)status + ")";
            if (!string.IsNullOrEmpty(title))
                text += ": " + title;
            return text;
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

        private static string DescribeException(Exception ex)
        {
            if (ex == null)
                return "";

            var parts = new System.Collections.Generic.List<string>();
            for (var cur = ex; cur != null && parts.Count < 5; cur = cur.InnerException)
                parts.Add(cur.Message);

            return string.Join(" → ", parts);
        }
    }
}

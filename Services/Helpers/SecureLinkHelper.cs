using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Services.Helpers
{
    /// <summary>
    /// توکن امضاشده برای لینک‌های بازشدنی از بله (پرداخت قسط بدون ورود).
    /// </summary>
    public static class SecureLinkHelper
    {
        private const string PayPurpose = "inst-pay";

        public static string CreateInstallmentPayToken(int installmentId, int userId, TimeSpan validFor, string signingKey)
        {
            var exp = DateTimeOffset.UtcNow.Add(validFor).ToUnixTimeSeconds();
            var payload = $"{PayPurpose}:{installmentId}:{userId}:{exp}";
            var sig = Sign(payload, signingKey);
            return Base64UrlEncode(Encoding.UTF8.GetBytes($"{payload}|{sig}"));
        }

        public static bool TryParseInstallmentPayToken(
            string token,
            string signingKey,
            out int installmentId,
            out int userId,
            out string error)
        {
            installmentId = 0;
            userId = 0;
            error = null;

            if (string.IsNullOrWhiteSpace(token))
            {
                error = "توکن پرداخت نامعتبر است";
                return false;
            }

            try
            {
                var raw = Encoding.UTF8.GetString(Base64UrlDecode(token.Trim()));
                var parts = raw.Split('|');
                if (parts.Length != 2)
                {
                    error = "توکن پرداخت نامعتبر است";
                    return false;
                }

                var payload = parts[0];
                var sig = parts[1];
                if (!FixedEquals(Sign(payload, signingKey), sig))
                {
                    error = "توکن پرداخت نامعتبر است";
                    return false;
                }

                var fields = payload.Split(':');
                if (fields.Length != 4 || fields[0] != PayPurpose)
                {
                    error = "توکن پرداخت نامعتبر است";
                    return false;
                }

                if (!int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out installmentId) ||
                    !int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out userId) ||
                    !long.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var exp))
                {
                    error = "توکن پرداخت نامعتبر است";
                    return false;
                }

                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp)
                {
                    error = "لینک پرداخت منقضی شده است. از پنل وارد شوید یا منتظر یادآوری جدید بمانید.";
                    return false;
                }

                return true;
            }
            catch
            {
                error = "توکن پرداخت نامعتبر است";
                return false;
            }
        }

        public static string CreateOpaqueToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            var text = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                text.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return text.ToString();
        }

        public static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(token ?? string.Empty)));
        }

        private static string Sign(string payload, string signingKey)
        {
            var key = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(signingKey)
                ? "GoodLoan-Default-Link-Key-Change-Me"
                : signingKey);
            using var hmac = new HMACSHA256(key);
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        private static bool FixedEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static string Base64UrlEncode(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] Base64UrlDecode(string input)
        {
            var s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}

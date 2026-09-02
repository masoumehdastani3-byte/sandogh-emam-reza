using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Services.Helpers
{
    public static class PersianDateHelper
    {
        private static readonly PersianCalendar Pc = new PersianCalendar();

        public static string ToShamsi(DateTime date)
        {
            return $"{Pc.GetYear(date):0000}/{Pc.GetMonth(date):00}/{Pc.GetDayOfMonth(date):00}";
        }

        public static DateTime? FromShamsi(string shamsi)
        {
            if (string.IsNullOrWhiteSpace(shamsi))
                return null;

            var parts = shamsi.Replace("-", "/").Split('/');
            if (parts.Length != 3)
                return null;

            if (!int.TryParse(parts[0], out var y) ||
                !int.TryParse(parts[1], out var m) ||
                !int.TryParse(parts[2], out var d))
                return null;

            try
            {
                return Pc.ToDateTime(y, m, d, 0, 0, 0, 0);
            }
            catch
            {
                return null;
            }
        }

        public static string GeneratePassword(int length = 8)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rnd = new Random();
            var sb = new StringBuilder(length);
            for (var i = 0; i < length; i++)
                sb.Append(chars[rnd.Next(chars.Length)]);
            return sb.ToString();
        }

        public static string GenerateOtpCode(int length = 6)
        {
            var rnd = new Random();
            var sb = new StringBuilder(length);
            for (var i = 0; i < length; i++)
                sb.Append(rnd.Next(0, 10));
            return sb.ToString();
        }

        public static bool IsValidNationalCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length != 10 || !code.All(char.IsDigit))
                return false;

            if (code.Distinct().Count() == 1)
                return false;

            var check = int.Parse(code[9].ToString());
            var sum = 0;
            for (var i = 0; i < 9; i++)
                sum += int.Parse(code[i].ToString()) * (10 - i);

            var remainder = sum % 11;
            return (remainder < 2 && check == remainder) || (remainder >= 2 && check + remainder == 11);
        }

        public static bool IsValidMobile(string mobile) =>
            !string.IsNullOrWhiteSpace(mobile) &&
            mobile.Length == 11 &&
            mobile.StartsWith("09") &&
            mobile.All(char.IsDigit);

        public static string ToEnglishDigits(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (ch >= '۰' && ch <= '۹')
                    sb.Append((char)('0' + (ch - '۰')));
                else if (ch >= '٠' && ch <= '٩')
                    sb.Append((char)('0' + (ch - '٠')));
                else
                    sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}

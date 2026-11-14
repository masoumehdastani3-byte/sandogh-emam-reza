using System.Globalization;

namespace Services.Helpers
{
    public static class MoneyText
    {
        private static readonly CultureInfo Fa = CreateCulture();

        public static string Toman(long rial)
        {
            var toman = rial / 10;
            return toman.ToString("#,0", Fa) + " تومان";
        }

        private static CultureInfo CreateCulture()
        {
            try
            {
                return CultureInfo.GetCultureInfo("fa-IR");
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }
    }
}

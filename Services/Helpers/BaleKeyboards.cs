namespace Services.Helpers
{
    public static class BaleKeyboards
    {
        public static object For(bool isManager, bool isDonator)
        {
            if (isDonator)
            {
                return new
                {
                    keyboard = new[]
                    {
                        new[] { Btn("وضعیت من"), Btn("مانده من") },
                        new[] { Btn("پرداخت کمک") }
                    },
                    resize_keyboard = true
                };
            }

            if (isManager)
            {
                return new
                {
                    keyboard = new[]
                    {
                        new[] { Btn("وضعیت من"), Btn("مانده من") },
                        new[] { Btn("پرداخت قسط"), Btn("درخواست وام") },
                        new[] { Btn("صف وام") }
                    },
                    resize_keyboard = true
                };
            }

            return new
            {
                keyboard = new[]
                {
                    new[] { Btn("وضعیت من"), Btn("مانده من") },
                    new[] { Btn("پرداخت قسط"), Btn("درخواست وام") }
                },
                resize_keyboard = true
            };
        }

        public static object InlineUrl(string label, string url)
        {
            return new
            {
                inline_keyboard = new[]
                {
                    new[] { new { text = label, url } }
                }
            };
        }

        private static object Btn(string text) => new { text };
    }
}

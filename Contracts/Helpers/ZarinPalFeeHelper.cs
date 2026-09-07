using System;

namespace Contracts.Helpers
{
    /// <summary>
    /// کارمزد زرین‌پال: ۰٫۵٪ تا سقف ۱۶٬۰۰۰ تومان + ۵۰۰ تومان ثابت (مبالغ سیستم ریال است).
    /// مبلغ درگاه طوری محاسبه می‌شود که پس از کسر کارمزد، مبلغ خالص قسط به صندوق برسد.
    /// </summary>
    public static class ZarinPalFeeHelper
    {
        public const double FeePercent = 0.005;
        public const long FeeCapTomans = 16000;
        public const long FeeFixedTomans = 500;
        public const long FeeCapRials = FeeCapTomans * 10;
        public const long FeeFixedRials = FeeFixedTomans * 10;

        public static long CalculateFeeOnGatewayAmount(long gatewayAmount)
        {
            if (gatewayAmount <= 0) return 0;

            var percentPart = (long)Math.Ceiling(gatewayAmount * FeePercent);
            if (percentPart > FeeCapRials)
                percentPart = FeeCapRials;

            return percentPart + FeeFixedRials;
        }

        /// <summary>
        /// مبلغ قابل پرداخت در درگاه و کارمزد، برای اینکه مبلغ خالص دریافتی برابر baseAmount بماند.
        /// </summary>
        public static (long PayableAmount, long FeeAmount) CalculatePayable(long baseAmount)
        {
            if (baseAmount <= 0)
                return (0, 0);

            var payableUncapped = (long)Math.Ceiling((baseAmount + FeeFixedRials) / (1.0 - FeePercent));
            var percentFee = (long)Math.Ceiling(payableUncapped * FeePercent);

            long payable;
            if (percentFee <= FeeCapRials)
            {
                payable = baseAmount + percentFee + FeeFixedRials;
            }
            else
            {
                payable = baseAmount + FeeCapRials + FeeFixedRials;
            }

            var fee = CalculateFeeOnGatewayAmount(payable);
            while (payable - fee < baseAmount)
            {
                payable++;
                fee = CalculateFeeOnGatewayAmount(payable);
            }

            return (payable, payable - baseAmount);
        }

        public static long GetFeeForBaseAmount(long baseAmount) => CalculatePayable(baseAmount).FeeAmount;

        public static long GetPayableAmount(long baseAmount) => CalculatePayable(baseAmount).PayableAmount;
    }
}

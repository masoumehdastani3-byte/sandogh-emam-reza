using Contracts.Helpers;
using System;

namespace Contracts.Models
{
   public class InstallmentDto : MainEntitiesDto
    {
        public int InstallmentId { get; set; }
        public int LoanId { get; set; }
        public int? PaymentId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime InstallmentDate { get; set; }

        public string PaymentDate_shansi { get; set; }
        public string InstallmentDate_shansi { get; set; }
        public long InstallmentAmount { get; set; }
        public byte InstallmentStatus { get; set; }

        /// <summary>کارمزد درگاه زرین‌پال (ریال) برای پرداخت آنلاین این قسط.</summary>
        public long GatewayFeeAmount => ZarinPalFeeHelper.GetFeeForBaseAmount(InstallmentAmount);

        /// <summary>مبلغ قسط + کارمزد درگاه که در زرین‌پال پرداخت می‌شود.</summary>
        public long PayableWithGatewayFee => ZarinPalFeeHelper.GetPayableAmount(InstallmentAmount);

        public virtual LoanDto Loan { get; set; }
        public virtual PaymentDto Payment { get; set; }
    }
}

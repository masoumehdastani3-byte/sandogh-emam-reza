using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Payment : MainEntities
    {
        public Payment()
        {
            Installments = new HashSet<Installment>();
            Loans = new HashSet<Loan>();
        }

        public int PaymentId { get; set; }
        public int? UserId { get; set; }
        public int? InstallmentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentDate_Shamsi { get; set; }
        public long Amount { get; set; }
        /// <summary>کارمزد درگاه (ریال) که به مبلغ پایه اضافه شده است.</summary>
        public long GatewayFeeAmount { get; set; }
        public byte PaymentStatus { get; set; }
        public byte PaymentType { get; set; }
        public byte PaymentMethod { get; set; }
        public byte GatewayStatus { get; set; }
        public string CardNumber { get; set; }
        public string FileUload { get; set; }
        public string RefrenceNumber { get; set; }
        public string ZarinPalAuthority { get; set; }
        public string ZarinPalRefId { get; set; }
        public bool Confirm { get; set; }
        public int? ConfirmUserId { get; set; }
        public string ConfirmUserDescription { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<Installment> Installments { get; set; }
        public virtual ICollection<Loan> Loans { get; set; }
    }
}

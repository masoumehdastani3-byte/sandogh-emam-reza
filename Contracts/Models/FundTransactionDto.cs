using Contracts.Enums;
using System;

namespace Contracts.Models
{
    public class FundTransactionDto : MainEntitiesDto
    {
        public int FundTransactionId { get; set; }
        public byte TransactionType { get; set; }
        public long Amount { get; set; }
        public long BalanceAfter { get; set; }
        public int? UserId { get; set; }
        public int? PaymentId { get; set; }
        public int? LoanId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionDate_Shamsi { get; set; }
        public string TransactionTypeName => EnumDescription.GetEnumDescription((FundTransactionType)TransactionType);
    }
}
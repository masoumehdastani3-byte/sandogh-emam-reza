using Contracts.Enums;
using System;
using System.Collections.Generic;

namespace Contracts.Models
{
    public class LoanDto : MainEntitiesDto
    {
        public LoanDto()
        {
            Installments = new HashSet<InstallmentDto>();
        }

        public int LoanId { get; set; }
        public int? PaymentId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentDate_Shamsi { get; set; }
        public DateTime RequestDate { get; set; }
        public string RequestDate_Shamsi { get; set; }
        public int QueuePriority { get; set; }
        public int UserId { get; set; }
        public int? UserSponsorId { get; set; }
        public long LoanAmount { get; set; }
        public long? RemainAmount { get; set; }
        public long? InstallmentAmount { get; set; }
        public int? InstallmentCount { get; set; }
        public byte LoanStatus { get; set; }
        public byte LoanRequestStatus { get; set; }
        public string RejectReason { get; set; }
        public int QueuePosition { get; set; }

        public string LoanStatusName => EnumDescription.GetEnumDescription((LoanStatus)LoanStatus);
        public string LoanRequestStatusName => EnumDescription.GetEnumDescription((LoanRequestStatus)LoanRequestStatus);

        public virtual UserDto User { get; set; }
        public virtual PaymentDto Payment { get; set; }
        public virtual ICollection<InstallmentDto> Installments { get; set; }
    }
}

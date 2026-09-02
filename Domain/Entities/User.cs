using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class User : MainEntities
    {
        public User()
        {
            Loans = new HashSet<Loan>();
            Payments = new HashSet<Payment>();
            Notifications = new HashSet<NotificationLog>();
            FundTransactions = new HashSet<FundTransaction>();
        }

        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string NationalCode { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string PostalCode { get; set; }
        public string Address { get; set; }
        public DateTime? BirthDate { get; set; }
        public string BirthDate_Shamsi { get; set; }
        public byte UserGender { get; set; }
        public byte UserStatusLife { get; set; }
        public byte UserActiveType { get; set; }
        public byte UserStatus { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string CardNumber { get; set; }
        public string BankName { get; set; }
        public string NationalIdCardPath { get; set; }
        public bool? NationalIdVerified { get; set; }
        public string NationalIdVerifyMessage { get; set; }
        public string BaleChatId { get; set; }
        public bool HasBaleApp { get; set; }
        public long? MonthlyDonationAmount { get; set; }

        public virtual UserRole UserRole { get; set; }
        public virtual ICollection<Loan> Loans { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
        public virtual ICollection<NotificationLog> Notifications { get; set; }
        public virtual ICollection<FundTransaction> FundTransactions { get; set; }
    }
}

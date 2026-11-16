using Contracts.Enums;
using System;
using System.Collections.Generic;

namespace Contracts.Models
{
    public class UserDto : MainEntitiesDto
    {
        public UserDto()
        {
            Loans = new HashSet<LoanDto>();
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
        public UserRoleDto UserRole { get; set; }
        public virtual ICollection<LoanDto> Loans { get; set; }

        public string RoleName { get; set; }
        public bool HasOpenLoan { get; set; }
        public string UserStatusLifeName => EnumDescription.GetEnumDescription((UserStatusLife)UserStatusLife);
        public string UserActiveTypeName => EnumDescription.GetEnumDescription((UserActiveType)UserActiveType);
        public string UserStatusName => EnumDescription.GetEnumDescription((UserStatus)UserStatus);
        public string UserGenderName => EnumDescription.GetEnumDescription((UserGender)UserGender);
    }
}

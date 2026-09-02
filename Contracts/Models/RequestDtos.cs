namespace Contracts.Models
{
    using System.Collections.Generic;

    public class RegisterMemberDto
    {
        public string NationalCode { get; set; }
        public string Mobile { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public byte RoleType { get; set; }
        public string BaleChatId { get; set; }
        public bool HasBaleApp { get; set; } = true;
        public long? MonthlyDonationAmount { get; set; }
    }

    public class LoginDto
    {
        public string NationalCode { get; set; }
        public string Password { get; set; }
    }

    public class RequestLoginOtpDto
    {
        public string NationalCode { get; set; }
    }

    public class VerifyLoginOtpDto
    {
        public string NationalCode { get; set; }
        public string OtpCode { get; set; }
    }

    public class UpdateProfileDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PostalCode { get; set; }
        public string Address { get; set; }
        public byte UserGender { get; set; }
        public string BirthDate_Shamsi { get; set; }
        public string Email { get; set; }
        public string CardNumber { get; set; }
        public string BankName { get; set; }
        public string BaleChatId { get; set; }
        public long? MonthlyDonationAmount { get; set; }
    }

    public class LoanRequestDto
    {
        public int UserId { get; set; }
        public long LoanAmount { get; set; }
        public string Description { get; set; }
    }

    public class ManualPaymentDto
    {
        public int UserId { get; set; }
        public long Amount { get; set; }
        public int? InstallmentId { get; set; }
        public byte PaymentType { get; set; }
        public string RefrenceNumber { get; set; }
        public string CardNumber { get; set; }
        public string Description { get; set; }
        public string FileUpload { get; set; }
        public int ConfirmUserId { get; set; }
    }

    public class ZarinPalRequestDto
    {
        public int UserId { get; set; }
        public long Amount { get; set; }
        public int? InstallmentId { get; set; }
        public byte PaymentType { get; set; }
        public string Description { get; set; }
        public string CallbackUrl { get; set; }
    }

    public class MemberPanelDto
    {
        public UserDto User { get; set; }
        public long MaxLoanAmount { get; set; }
        public long FundBalance { get; set; }
        public int WaitingQueueCount { get; set; }
        public int MyQueuePosition { get; set; }
        public long TotalDebt { get; set; }
        public long PaidInstallmentsAmount { get; set; }
        public long RemainInstallmentsAmount { get; set; }
        public int PaidInstallmentsCount { get; set; }
        public int RemainInstallmentsCount { get; set; }
        public long TotalDonations { get; set; }
        public System.Collections.Generic.List<LoanDto> Loans { get; set; }
        public System.Collections.Generic.List<InstallmentDto> Installments { get; set; }
        public System.Collections.Generic.List<PaymentDto> Payments { get; set; }
    }

    public class DisburseLoanDto
    {
        public int LoanId { get; set; }
        public int ManagerUserId { get; set; }
        public string Description { get; set; }
    }

    public class RejectLoanDto
    {
        public int LoanId { get; set; }
        public string Reason { get; set; }
        public int ManagerUserId { get; set; }
    }

    public class FundDepositDto
    {
        public int ManagerUserId { get; set; }
        public long Amount { get; set; }
        public string Description { get; set; }
    }

    public class AdminActionDto
    {
        public int ManagerUserId { get; set; }
    }

    public class AdminDashboardDto
    {
        public long FundBalance { get; set; }
        public int WaitingQueueCount { get; set; }
        public int TodayPaymentsCount { get; set; }
        public long TodayPaymentsAmount { get; set; }
    }

    public class SaveRoleDto
    {
        public int ManagerUserId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
    }

    public class UpdateRoleDto
    {
        public int ManagerUserId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public bool? Active { get; set; }
    }

    public class SaveMenuDto
    {
        public int ManagerUserId { get; set; }
        public string ControlId { get; set; }
        public string SelectorId { get; set; }
        public string MenuTitle { get; set; }
        public string Description { get; set; }
    }

    public class SaveRoleMenuAccessDto
    {
        public int ManagerUserId { get; set; }
        public int RoleId { get; set; }
        public List<int> MenuIds { get; set; }
    }

    public class RoleWithMenusDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public bool? Active { get; set; }
        public List<int> MenuIds { get; set; }
        public int UserCount { get; set; }
    }

    public class ApiResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }

        public static ApiResultDto Ok(string message = "0", object data = null) =>
            new ApiResultDto { Success = true, Message = message, Data = data };

        public static ApiResultDto Fail(string message) =>
            new ApiResultDto { Success = false, Message = message };
    }
}

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

    public class VerifyLoginTokenDto
    {
        public string LoginToken { get; set; }
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
        /// <summary>توکن لینک پرداخت از بله؛ در صورت وجود، ورود لازم نیست.</summary>
        public string PayToken { get; set; }
    }

    /// <summary>بدنه نوتیفیکیشن هاب پرداخت نیکو سیستم.</summary>
    public class NikooNotifyDto
    {
        public string Token { get; set; }
        public string Status { get; set; }
        public object Amount { get; set; }
        public string ExternalRef { get; set; }
        public object RefId { get; set; }
        public string Timestamp { get; set; }
        public string Signature { get; set; }
        public string Message { get; set; }
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

    public class UpdateSettingDto
    {
        public int ManagerUserId { get; set; }
        public string SettingName { get; set; }
        public string SettingValue { get; set; }
    }

    public class AdminDashboardDto
    {
        public long FundBalance { get; set; }
        public int WaitingQueueCount { get; set; }
        public int TodayPaymentsCount { get; set; }
        public long TodayPaymentsAmount { get; set; }
        public int OverdueInstallmentsCount { get; set; }
        public int DueInThreeDaysInstallmentsCount { get; set; }
    }

    public class InstallmentCollectionItemDto
    {
        public int InstallmentId { get; set; }
        public int LoanId { get; set; }
        public int UserId { get; set; }
        public string MemberName { get; set; }
        public string Mobile { get; set; }
        public long InstallmentAmount { get; set; }
        public string InstallmentDate_Shamsi { get; set; }
        public int DaysUntilDue { get; set; }
        public string StatusLabel { get; set; }
    }

    public class InstallmentCollectionReportDto
    {
        public int OverdueCount { get; set; }
        public int DueInThreeDaysCount { get; set; }
        public List<InstallmentCollectionItemDto> OverdueInstallments { get; set; }
        public List<InstallmentCollectionItemDto> DueInThreeDaysInstallments { get; set; }
    }

    public class SendRemindersResultDto
    {
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<NotificationLogDto> Logs { get; set; }
    }

    public class MonthlyFundInflowDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthLabel { get; set; }
        public long TotalAmount { get; set; }
        public int TransactionCount { get; set; }
    }

    public class ManagementReportDto
    {
        public long FundBalance { get; set; }
        public int DebtorLoansCount { get; set; }
        public long TotalDebtAmount { get; set; }
        public int DisbursedLoansCount { get; set; }
        public long TotalDisbursedAmount { get; set; }
        public long CurrentMonthInflow { get; set; }
        public string ReportDate_Shamsi { get; set; }
        public List<MonthlyFundInflowDto> MonthlyInflows { get; set; }
    }

    public class AdminUpdateMemberDto
    {
        public int ManagerUserId { get; set; }
        public int UserId { get; set; }
        public bool? Active { get; set; }
        public string Description { get; set; }
        public bool UpdateDescription { get; set; }
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

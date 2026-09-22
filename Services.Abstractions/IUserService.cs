using Contracts.Models;
using Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Abstractions
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<UserDto> GetByUserId(int userId, CancellationToken cancellationToken = default);
        Task<List<UserDto>> GetFilterAsync(string userName, int? userId, string fullName, string cardNumber, string mobile, CancellationToken cancellationToken = default);
        Task<ApiResultDto> RegisterAsync(RegisterMemberDto dto, CancellationToken cancellationToken = default);
        Task<ApiResultDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
        Task<ApiResultDto> RequestLoginOtpAsync(RequestLoginOtpDto dto, CancellationToken cancellationToken = default);
        Task<ApiResultDto> VerifyLoginOtpAsync(VerifyLoginOtpDto dto, CancellationToken cancellationToken = default);
        Task<ApiResultDto> VerifyLoginTokenAsync(VerifyLoginTokenDto dto, CancellationToken cancellationToken = default);
        Task<ApiResultDto> UpdateProfileAsync(UpdateProfileDto dto, CancellationToken cancellationToken = default);
        Task<ApiResultDto> UploadNationalIdAsync(int userId, string filePath, CancellationToken cancellationToken = default);
        Task<string> Insert(User user, CancellationToken cancellationToken = default);
        Task<string> Update(User user, CancellationToken cancellationToken = default);
        Task<string> Delete(int userId, CancellationToken cancellationToken = default);
    }

    public interface ILoanService
    {
        Task<ApiResultDto> RequestLoanAsync(LoanRequestDto dto, CancellationToken cancellationToken = default);
        Task<ApiResultDto> DisburseLoanAsync(DisburseLoanDto dto, CancellationToken cancellationToken = default);
        Task<ApiResultDto> RejectLoanAsync(int loanId, string reason, int managerUserId, CancellationToken cancellationToken = default);
        Task<List<LoanDto>> GetQueueAsync(CancellationToken cancellationToken = default);
        Task<List<LoanDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<LoanDto>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<LoanDto> GetByIdAsync(int loanId, CancellationToken cancellationToken = default);
        Task<int> GetWaitingCountAsync(CancellationToken cancellationToken = default);
        Task<int> GetDisbursedCountAsync(CancellationToken cancellationToken = default);
    }

    public interface IPaymentService
    {
        Task<ApiResultDto> StartZarinPalPaymentAsync(ZarinPalRequestDto dto, CancellationToken cancellationToken = default);
        Task<ApiResultDto> GetInstallmentPayLinkAsync(string payToken, CancellationToken cancellationToken = default);
        Task<ApiResultDto> VerifyZarinPalPaymentAsync(string authority, string status, CancellationToken cancellationToken = default);
        Task<ApiResultDto> CompleteNikooPaymentAsync(
            string token,
            string status,
            string amount,
            string externalRef,
            string refId,
            string timestamp,
            string signature,
            CancellationToken cancellationToken = default);
        Task<ApiResultDto> SubmitManualPaymentAsync(ManualPaymentDto dto, CancellationToken cancellationToken = default);
        Task<List<PaymentDto>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<PaymentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    }

    public interface IMemberPanelService
    {
        Task<MemberPanelDto> GetPanelAsync(int userId, CancellationToken cancellationToken = default);
    }

    public interface IZarinPalService
    {
        Task<(bool Success, string Authority, string Message, string GatewayUrl)> RequestPaymentAsync(long amount, string description, string callbackUrl, string mobile = null, CancellationToken cancellationToken = default);
        Task<(bool Success, string RefId, string Message)> VerifyPaymentAsync(string authority, long amount, CancellationToken cancellationToken = default);
    }

    public interface INationalIdVerifyService
    {
        Task<(bool IsValid, string Message)> VerifyAsync(string nationalCode, string imagePath, CancellationToken cancellationToken = default);
    }

    public interface INotificationSchedulerService
    {
        Task<SendRemindersResultDto> SendMonthlyRemindersAsync(CancellationToken cancellationToken = default);
        Task ProcessLoanQueueAsync(CancellationToken cancellationToken = default);
    }

    public interface IAdminService
    {
        Task<bool> IsManagerAsync(int userId, CancellationToken cancellationToken = default);
        Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
        Task<ManagementReportDto> GetManagementReportAsync(CancellationToken cancellationToken = default);
        Task<InstallmentCollectionReportDto> GetInstallmentCollectionReportAsync(CancellationToken cancellationToken = default);
        Task<List<NotificationLogDto>> GetNotificationLogsAsync(CancellationToken cancellationToken = default);
        Task<ApiResultDto> DepositFundAsync(FundDepositDto dto, CancellationToken cancellationToken = default);
        Task<List<FundTransactionDto>> GetFundTransactionsAsync(CancellationToken cancellationToken = default);
        Task<List<SettingDto>> GetSettingsAsync(CancellationToken cancellationToken = default);
        Task<ApiResultDto> UpdateSettingAsync(UpdateSettingDto dto, CancellationToken cancellationToken = default);
        Task<List<UserDto>> GetMembersAsync(CancellationToken cancellationToken = default);
        Task<ApiResultDto> UpdateMemberAsync(AdminUpdateMemberDto dto, CancellationToken cancellationToken = default);
    }

    public interface IRoleMenuService
    {
        Task<List<RoleWithMenusDto>> GetRolesWithMenusAsync(CancellationToken cancellationToken = default);
        Task<List<UserSubMenuDto>> GetMenusAsync(CancellationToken cancellationToken = default);
        Task<List<UserSubMenuDto>> GetMenusForUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<ApiResultDto> CreateRoleAsync(SaveRoleDto dto, CancellationToken cancellationToken = default);
        Task<ApiResultDto> UpdateRoleAsync(int roleId, UpdateRoleDto dto, CancellationToken cancellationToken = default);
        Task<ApiResultDto> DeleteRoleAsync(int roleId, int managerUserId, CancellationToken cancellationToken = default);
        Task<ApiResultDto> SaveRoleMenuAccessAsync(SaveRoleMenuAccessDto dto, CancellationToken cancellationToken = default);
        Task<ApiResultDto> CreateMenuAsync(SaveMenuDto dto, CancellationToken cancellationToken = default);
    }
}

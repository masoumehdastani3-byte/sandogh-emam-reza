using Contracts.Models;
using Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Abstractions
{
    public interface IBaleService
    {
        Task<bool> SendMessageAsync(string chatId, string message, CancellationToken cancellationToken = default);
        Task NotifyPasswordAsync(User user, string password, CancellationToken cancellationToken = default);
        Task<bool> NotifyLoginOtpAsync(User user, string otpCode, string oneClickLoginLink = null, CancellationToken cancellationToken = default);
        Task<NotificationLog> NotifyInstallmentReminderAsync(User user, Installment installment, string paymentLink, bool isOverdue = false, CancellationToken cancellationToken = default);
        Task<NotificationLog> NotifyDonationLinkAsync(User user, string paymentLink, CancellationToken cancellationToken = default);
        Task NotifyManagersPaymentAsync(Payment payment, User payer, CancellationToken cancellationToken = default);

        /// <summary>پردازش آپدیت‌های دریافتی از webhook یا getUpdates و ذخیره chat_id</summary>
        Task<ApiResultDto> ProcessIncomingUpdateJsonAsync(string json, CancellationToken cancellationToken = default);

        /// <summary>یک‌بار getUpdates را صدا می‌زند و chat_idها را ذخیره می‌کند</summary>
        Task<ApiResultDto> PollAndBindChatIdsAsync(CancellationToken cancellationToken = default);

        string GetBotStartLink(int userId);
        Task<ApiResultDto> SetWebhookAsync(string webhookUrl, CancellationToken cancellationToken = default);
    }
}

using Contracts.Enums;
using Contracts.Models;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Services.Abstractions;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public sealed class BaleService : IBaleService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IRepositoryManager _repositoryManager;
        private static long _lastUpdateId;

        public BaleService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IRepositoryManager repositoryManager)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _repositoryManager = repositoryManager;
        }

        private string Token => _configuration["Bale:BotToken"];
        private string ApiBase => (_configuration["Bale:ApiBaseUrl"] ?? "https://tapi.bale.ai").TrimEnd('/');
        private bool IsTokenConfigured =>
            !string.IsNullOrWhiteSpace(Token) && Token != "YOUR_BALE_BOT_TOKEN";

        public string GetBotStartLink(int userId)
        {
            var username = _configuration["Bale:BotUsername"]?.Trim().TrimStart('@');
            if (string.IsNullOrWhiteSpace(username))
                return null;
            return $"https://ble.ir/{username}?start=u{userId}";
        }

        public async Task<bool> SendMessageAsync(string chatId, string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(message))
                return false;

            if (!IsTokenConfigured)
            {
                await LogAsync(null, NotificationType.AdminPaymentNotice, NotificationStatus.Pending, message, "Bale token not configured");
                return false;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{ApiBase}/bot{Token}/sendMessage";
                var payload = new { chat_id = long.TryParse(chatId, out var id) ? (object)id : chatId, text = message };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await LogAsync(null, NotificationType.AdminPaymentNotice, NotificationStatus.Failed, message, ex.Message);
                return false;
            }
        }

        public async Task NotifyPasswordAsync(User user, string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(user?.BaleChatId))
                return;

            var msg = $"عضویت شما در صندوق قرض‌الحسنه انجام شد.\nکد ملی (نام کاربری): {user.NationalCode}\nرمز عبور: {password}\nلطفاً رمز را محفوظ نگه دارید.";
            var ok = await SendMessageAsync(user.BaleChatId, msg, cancellationToken);
            await LogAsync(user.UserId, NotificationType.Password, ok ? NotificationStatus.Sent : NotificationStatus.Failed, msg);
        }

        public async Task<bool> NotifyLoginOtpAsync(User user, string otpCode, string oneClickLoginLink = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(user?.BaleChatId))
                return false;

            var msg = $"کد ورود یک‌بارمصرف صندوق قرض‌الحسنه:\n{otpCode}\n\n";
            if (!string.IsNullOrWhiteSpace(oneClickLoginLink))
            {
                msg += "برای ورود سریع بدون تایپ کد، روی لینک زیر بزنید:\n" +
                       $"{oneClickLoginLink}\n\n";
            }
            msg += "این کد/لینک تا ۱۰ دقیقه معتبر است. آن را با کسی به اشتراک نگذارید.";

            var ok = await SendMessageAsync(user.BaleChatId, msg, cancellationToken);
            await LogAsync(user.UserId, NotificationType.OtpLogin, ok ? NotificationStatus.Sent : NotificationStatus.Failed, "کد ورود یک‌بارمصرف ارسال شد");
            return ok;
        }

        public async Task<NotificationLog> NotifyInstallmentReminderAsync(User user, Installment installment, string paymentLink, bool isOverdue = false, CancellationToken cancellationToken = default)
        {
            var header = isOverdue
                ? "یادآوری پرداخت قسط عقب\u200cافتاده"
                : "یادآوری پرداخت قسط (۳ روز مانده به سررسید)";
            var msg = $"{header}\n" +
                      $"مبلغ: {installment.InstallmentAmount:N0} ریال\n" +
                      $"سررسید: {installment.InstallmentDate_shansi}\n\n" +
                      $"بدون نیاز به ورود، برای پرداخت آنلاین همین قسط روی لینک زیر بزنید:\n{paymentLink}";

            if (string.IsNullOrWhiteSpace(user?.BaleChatId))
                return await LogAsync(user?.UserId, NotificationType.InstallmentReminder, NotificationStatus.Failed, msg, "کاربر به بله متصل نیست", relatedEntity: "Installment", relatedId: installment.InstallmentId);

            var ok = await SendMessageAsync(user.BaleChatId, msg, cancellationToken);
            return await LogAsync(user.UserId, NotificationType.InstallmentReminder, ok ? NotificationStatus.Sent : NotificationStatus.Failed, msg, ok ? null : "ارسال پیام بله ناموفق بود", relatedEntity: "Installment", relatedId: installment.InstallmentId);
        }

        public async Task<NotificationLog> NotifyDonationLinkAsync(User user, string paymentLink, CancellationToken cancellationToken = default)
        {
            var amount = user.MonthlyDonationAmount ?? 0;
            var msg = $"لینک پرداخت ماهانه کمک خیرین\nمبلغ: {amount:N0} ریال\nلینک: {paymentLink}";

            if (string.IsNullOrWhiteSpace(user?.BaleChatId))
                return await LogAsync(user?.UserId, NotificationType.DonationLink, NotificationStatus.Failed, msg, "کاربر به بله متصل نیست");

            var ok = await SendMessageAsync(user.BaleChatId, msg, cancellationToken);
            return await LogAsync(user.UserId, NotificationType.DonationLink, ok ? NotificationStatus.Sent : NotificationStatus.Failed, msg, ok ? null : "ارسال پیام بله ناموفق بود");
        }

        public async Task NotifyManagersPaymentAsync(Payment payment, User payer, CancellationToken cancellationToken = default)
        {
            var managers = await _repositoryManager.UserRepository.GetManagersAsync(cancellationToken);
            var payerName = payer?.FullName ?? "-";
            var typeName = EnumDescription.GetEnumDescription((PaymentType)payment.PaymentType);
            var msg = $"واریزی جدید در صندوق\nپرداخت‌کننده: {payerName}\nنوع: {typeName}\nمبلغ: {payment.Amount:N0} ریال\nکد پیگیری: {payment.RefrenceNumber ?? payment.ZarinPalRefId}";

            foreach (var manager in managers)
            {
                if (string.IsNullOrWhiteSpace(manager.BaleChatId)) continue;
                var ok = await SendMessageAsync(manager.BaleChatId, msg, cancellationToken);
                await LogAsync(manager.UserId, NotificationType.AdminPaymentNotice, ok ? NotificationStatus.Sent : NotificationStatus.Failed, msg, relatedEntity: "Payment", relatedId: payment.PaymentId);
            }
        }

        public async Task<ApiResultDto> SetWebhookAsync(string webhookUrl, CancellationToken cancellationToken = default)
        {
            if (!IsTokenConfigured)
                return ApiResultDto.Fail("توکن ربات بله تنظیم نشده است");

            var client = _httpClientFactory.CreateClient();
            var url = $"{ApiBase}/bot{Token}/setWebhook";
            var payload = new { url = webhookUrl };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode
                ? ApiResultDto.Ok("Webhook ثبت شد", body)
                : ApiResultDto.Fail(body);
        }

        public async Task<ApiResultDto> PollAndBindChatIdsAsync(CancellationToken cancellationToken = default)
        {
            if (!IsTokenConfigured)
                return ApiResultDto.Fail("توکن ربات بله تنظیم نشده است");

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{ApiBase}/bot{Token}/getUpdates?timeout=25&offset={_lastUpdateId + 1}";
                var response = await client.GetAsync(url, cancellationToken);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    return ApiResultDto.Fail(body);

                var root = JObject.Parse(body);
                if (root["ok"]?.Value<bool>() != true)
                    return ApiResultDto.Fail(body);

                var updates = root["result"] as JArray;
                if (updates == null || updates.Count == 0)
                    return ApiResultDto.Ok("آپدیت جدیدی نبود", new { bound = 0 });

                var bound = 0;
                foreach (var update in updates)
                {
                    var updateId = update["update_id"]?.Value<long>() ?? 0;
                    if (updateId > _lastUpdateId)
                        _lastUpdateId = updateId;

                    if (await TryBindFromUpdateAsync(update, cancellationToken))
                        bound++;
                }

                return ApiResultDto.Ok($"تعداد {bound} چت‌آیدی ذخیره شد", new { bound, lastUpdateId = _lastUpdateId });
            }
            catch (Exception ex)
            {
                return ApiResultDto.Fail(ex.Message);
            }
        }

        public async Task<ApiResultDto> ProcessIncomingUpdateJsonAsync(string json, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(json))
                return ApiResultDto.Fail("بدنه خالی است");

            try
            {
                var token = JToken.Parse(json);
                // webhook ممکن است یک update یا آرایه بفرستد
                if (token is JArray arr)
                {
                    var count = 0;
                    foreach (var item in arr)
                        if (await TryBindFromUpdateAsync(item, cancellationToken)) count++;
                    return ApiResultDto.Ok("پردازش شد", new { bound = count });
                }

                // گاهی getUpdates داخل result است
                if (token["result"] is JArray results)
                {
                    var count = 0;
                    foreach (var item in results)
                        if (await TryBindFromUpdateAsync(item, cancellationToken)) count++;
                    return ApiResultDto.Ok("پردازش شد", new { bound = count });
                }

                var ok = await TryBindFromUpdateAsync(token, cancellationToken);
                return ApiResultDto.Ok(ok ? "chat_id ذخیره شد" : "پیام پردازش شد ولی کاربر متناظر پیدا نشد");
            }
            catch (Exception ex)
            {
                return ApiResultDto.Fail(ex.Message);
            }
        }

        private async Task<bool> TryBindFromUpdateAsync(JToken update, CancellationToken cancellationToken)
        {
            var message = update["message"] ?? update["edited_message"];
            if (message == null)
                return false;

            var chatId = message["chat"]?["id"]?.ToString();
            if (string.IsNullOrWhiteSpace(chatId))
                return false;

            var text = (message["text"]?.ToString() ?? "").Trim();

            // 1) /start u123 یا /start 123
            var startMatch = Regex.Match(text, @"^/start(?:@\w+)?\s+(?:u)?(\d+)\s*$", RegexOptions.IgnoreCase);
            if (startMatch.Success && int.TryParse(startMatch.Groups[1].Value, out var userId))
            {
                return await BindUserAsync(await _repositoryManager.UserRepository.GetByUserIdAsync(userId, cancellationToken), chatId, cancellationToken);
            }

            // 2) کد ملی ۱۰ رقمی
            var nationalMatch = Regex.Match(text, @"\b(\d{10})\b");
            if (nationalMatch.Success)
            {
                var user = await _repositoryManager.UserRepository.GetByNationalCodeAsync(nationalMatch.Groups[1].Value, cancellationToken);
                if (user != null)
                    return await BindUserAsync(user, chatId, cancellationToken);
            }

            // 3) موبایل 09xxxxxxxxx
            var mobileMatch = Regex.Match(text, @"\b(09\d{9})\b");
            if (mobileMatch.Success)
            {
                var user = await _repositoryManager.UserRepository.GetByMobileAsync(mobileMatch.Groups[1].Value, cancellationToken);
                if (user != null)
                    return await BindUserAsync(user, chatId, cancellationToken);
            }

            // 4) اگر فقط /start بدون پارامتر بود
            if (Regex.IsMatch(text, @"^/start", RegexOptions.IgnoreCase))
            {
                await SendMessageAsync(chatId,
                    "سلام\nبرای اتصال حساب، یکی از این‌ها را بفرستید:\n• لینک عضویت سایت را باز کنید و Start بزنید\n• یا کد ملی خود را ارسال کنید\n• یا شماره موبایل ثبت‌شده را ارسال کنید",
                    cancellationToken);
            }

            return false;
        }

        private async Task<bool> BindUserAsync(User user, string chatId, CancellationToken cancellationToken)
        {
            if (user == null)
                return false;

            var alreadyHad = !string.IsNullOrWhiteSpace(user.BaleChatId) && user.BaleChatId == chatId;
            user.BaleChatId = chatId;
            user.HasBaleApp = true;
            user.UpdateDate = DateTime.Now;
            _repositoryManager.UserRepository.Update(user);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);

            if (!alreadyHad && !string.IsNullOrWhiteSpace(user.Password))
                await NotifyPasswordAsync(user, user.Password, cancellationToken);
            else
                await SendMessageAsync(chatId, $"حساب شما متصل شد.\n{user.FullName}\nاز این پس پیام‌های صندوق برای شما ارسال می‌شود.", cancellationToken);

            return true;
        }

        private async Task<NotificationLog> LogAsync(int? userId, NotificationType type, NotificationStatus status, string message, string error = null, string relatedEntity = null, int? relatedId = null)
        {
            var log = new NotificationLog
            {
                UserId = userId,
                NotificationType = (byte)type,
                NotificationStatus = (byte)status,
                Message = message,
                Channel = "Bale",
                SentDate = DateTime.Now,
                ErrorMessage = error,
                RelatedEntity = relatedEntity,
                RelatedId = relatedId,
                CreateDate = DateTime.Now,
                Active = true
            };
            _repositoryManager.NotificationLogRepository.Insert(log);
            await _repositoryManager.UnitOfWork.SaveChangesAsync();
            return log;
        }
    }
}

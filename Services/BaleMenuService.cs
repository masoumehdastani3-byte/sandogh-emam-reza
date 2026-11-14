using Contracts.Enums;
using Contracts.Models;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Services.Abstractions;
using Services.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public sealed class BaleMenuService : IBaleMenuService
    {
        private readonly BaleService _bale;
        private readonly IRepositoryManager _repository;
        private readonly IConfiguration _configuration;
        private readonly ILoanService _loanService;
        private readonly IPaymentService _paymentService;
        private static readonly ConcurrentDictionary<string, string> Pending = new ConcurrentDictionary<string, string>();

        public BaleMenuService(
            BaleService bale,
            IRepositoryManager repository,
            IConfiguration configuration,
            ILoanService loanService,
            IPaymentService paymentService)
        {
            _bale = bale;
            _repository = repository;
            _configuration = configuration;
            _loanService = loanService;
            _paymentService = paymentService;
        }

        public async Task<ApiResultDto> PollAsync(CancellationToken cancellationToken = default)
        {
            var fetched = await _bale.FetchUpdatesAsync(cancellationToken);
            if (!fetched.Ok)
                return ApiResultDto.Fail(fetched.Error ?? "خواندن پیام بله ناموفق بود");

            if (fetched.Updates == null || fetched.Updates.Count == 0)
                return ApiResultDto.Ok("آپدیت جدیدی نبود");

            foreach (var update in fetched.Updates)
            {
                try
                {
                    await HandleUpdateAsync(update, cancellationToken);
                }
                catch
                {
                    // یک پیام خراب بقیه صف را متوقف نکند
                }
            }

            return ApiResultDto.Ok("پیام‌های بله پردازش شد", new { count = fetched.Updates.Count });
        }

        public async Task<ApiResultDto> ProcessIncomingUpdateJsonAsync(string json, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(json))
                return ApiResultDto.Fail("بدنه خالی است");

            try
            {
                var token = JToken.Parse(json);
                if (token is JArray arr)
                {
                    foreach (var item in arr)
                        await HandleUpdateAsync(item, cancellationToken);
                    return ApiResultDto.Ok("پردازش شد");
                }

                if (token["result"] is JArray results)
                {
                    foreach (var item in results)
                        await HandleUpdateAsync(item, cancellationToken);
                    return ApiResultDto.Ok("پردازش شد");
                }

                await HandleUpdateAsync(token, cancellationToken);
                return ApiResultDto.Ok("پردازش شد");
            }
            catch (Exception ex)
            {
                return ApiResultDto.Fail(ex.Message);
            }
        }

        private async Task HandleUpdateAsync(JToken update, CancellationToken cancellationToken)
        {
            var callback = update["callback_query"];
            if (callback != null)
            {
                await HandleCallbackAsync(callback, cancellationToken);
                return;
            }

            var bound = await _bale.TryBindFromUpdateAsync(update, cancellationToken);
            var message = update["message"] ?? update["edited_message"];
            var chatId = message?["chat"]?["id"]?.ToString();
            if (string.IsNullOrWhiteSpace(chatId))
                return;

            if (bound)
            {
                var linked = await _repository.UserRepository.GetByBaleChatIdAsync(chatId, cancellationToken);
                await SendHomeAsync(linked, chatId, "حساب شما وصل شد. از این به بعد با دکمه‌های پایین کار کنید و لازم نیست هر بار وارد سایت شوید.", cancellationToken);
                return;
            }

            var text = (message["text"]?.ToString() ?? "").Trim();
            var user = await _repository.UserRepository.GetByBaleChatIdAsync(chatId, cancellationToken);
            if (user == null)
            {
                var baseUrl = await AppUrlHelper.GetBaseUrlAsync(_repository, _configuration, cancellationToken);
                await _bale.SendMessageAsync(chatId,
                    "سلام. هنوز حساب شما وصل نیست.\nکد ملی ۱۰ رقمی را همین‌جا بفرستید.\nاگر هنوز عضو نشده‌اید، یک بار این صفحه را باز کنید و ثبت‌نام کنید:\n" + baseUrl.TrimEnd('/') + "/index.html#register",
                    cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                await SendHomeAsync(user, chatId, "لطفاً یکی از دکمه‌های پایین را بزنید.", cancellationToken);
                return;
            }

            await DispatchTextAsync(user, chatId, text, cancellationToken);
        }

        private async Task HandleCallbackAsync(JToken callback, CancellationToken cancellationToken)
        {
            var callbackId = callback["id"]?.ToString();
            var data = (callback["data"]?.ToString() ?? "").Trim();
            var chatId = callback["message"]?["chat"]?["id"]?.ToString();
            await _bale.AnswerCallbackAsync(callbackId, "باشه", cancellationToken);
            if (string.IsNullOrWhiteSpace(chatId))
                return;

            var user = await _repository.UserRepository.GetByBaleChatIdAsync(chatId, cancellationToken);
            if (user == null)
            {
                await _bale.SendMessageAsync(chatId, "اول کد ملی خود را بفرستید تا حساب وصل شود.", cancellationToken);
                return;
            }

            if (data.StartsWith("la:", StringComparison.Ordinal))
            {
                if (long.TryParse(data.Substring(3), out var amount))
                    await RequestLoanAmountAsync(user, chatId, amount, cancellationToken);
                return;
            }

            if (data.StartsWith("q:", StringComparison.Ordinal))
            {
                await MoveFromCallbackAsync(user, chatId, data.Substring(2), cancellationToken);
                return;
            }

            if (data.StartsWith("d:", StringComparison.Ordinal) && int.TryParse(data.Substring(2), out var disburseId))
            {
                await DisburseFromBaleAsync(user, chatId, disburseId, cancellationToken);
                return;
            }

            if (data.StartsWith("x:", StringComparison.Ordinal) && int.TryParse(data.Substring(2), out var rejectId))
            {
                Pending[chatId] = "reject:" + rejectId;
                await _bale.SendWithKeyboardAsync(chatId,
                    "دلیل رد را انتخاب کنید، یا دلیل را بنویسید.",
                    RejectReasonKeyboard(rejectId),
                    cancellationToken);
                return;
            }

            if (data.StartsWith("rr:", StringComparison.Ordinal))
            {
                await RejectFromCallbackAsync(user, chatId, data, cancellationToken);
            }
        }

        private async Task DispatchTextAsync(User user, string chatId, string text, CancellationToken cancellationToken)
        {
            var norm = Normalize(text);
            if (!IsMenuCommand(norm) && Pending.TryGetValue(chatId, out var pending))
            {
                if (pending.StartsWith("reject:", StringComparison.Ordinal) && int.TryParse(pending.Substring(7), out var loanId))
                {
                    Pending.TryRemove(chatId, out _);
                    await RejectFromBaleAsync(user, chatId, loanId, text.Trim(), cancellationToken);
                    return;
                }

                if (pending == "donation" && TryReadMoney(text, out var toman))
                {
                    Pending.TryRemove(chatId, out _);
                    await StartDonationAsync(user, chatId, toman * 10, cancellationToken);
                    return;
                }
            }

            if (norm == "وضعیت من" || norm == "وضعیت" || norm.StartsWith("/start"))
            {
                await SendStatusAsync(user, chatId, cancellationToken);
                return;
            }

            if (norm == "مانده من" || norm == "مانده" || norm == "مانده و قسطها" || norm == "مانده و قسط ها")
            {
                await SendBalanceAsync(user, chatId, cancellationToken);
                return;
            }

            if (norm == "پرداخت قسط" || norm == "قسط")
            {
                await SendPayInstallmentAsync(user, chatId, cancellationToken);
                return;
            }

            if (norm == "درخواست وام" || norm == "وام")
            {
                await SendLoanChoicesAsync(user, chatId, cancellationToken);
                return;
            }

            if (norm == "پرداخت کمک" || norm == "کمک")
            {
                await BeginDonationAsync(user, chatId, cancellationToken);
                return;
            }

            if (norm == "صف وام" || norm == "صف")
            {
                await SendQueueAsync(user, chatId, cancellationToken);
                return;
            }

            if (norm == "راهنما")
            {
                await SendHomeAsync(user, chatId, "با دکمه‌های پایین کار کنید: وضعیت، مانده، پرداخت قسط و درخواست وام.", cancellationToken);
                return;
            }

            if (TryReadMoney(text, out _))
            {
                await SendHomeAsync(user, chatId, "مبلغ را از دکمه‌ها انتخاب کنید. دکمه «درخواست وام» را بزنید.", cancellationToken);
                return;
            }

            await SendHomeAsync(user, chatId, "این پیام را نفهمیدم. یکی از دکمه‌های پایین را بزنید.", cancellationToken);
        }

        private async Task SendHomeAsync(User user, string chatId, string text, CancellationToken cancellationToken)
        {
            var manager = user != null && await IsManagerAsync(user, cancellationToken);
            var donator = user != null && user.RoleId == (int)UserRoleType.Donator;
            await _bale.SendWithKeyboardAsync(chatId, text, BaleKeyboards.For(manager, donator), cancellationToken);
        }

        private async Task SendStatusAsync(User user, string chatId, CancellationToken cancellationToken)
        {
            var name = string.IsNullOrWhiteSpace(user.FullName) ? "" : user.FullName + "\n";
            var loans = (await _repository.LoanRepository.GetByUserIdAsync(user.UserId, cancellationToken)).ToList();
            var waiting = loans.FirstOrDefault(l => l.LoanRequestStatus == (byte)LoanRequestStatus.WaitingInQueue && l.Active != false);
            var active = loans.FirstOrDefault(l =>
                l.LoanRequestStatus == (byte)LoanRequestStatus.Disbursed &&
                l.LoanStatus != (byte)LoanStatus.Cleared &&
                l.Active != false);

            string body;
            if (waiting != null)
            {
                body = name + "درخواست وام شما ثبت شده و در نوبت است.\nهر وقت نوبت شما شود، همین‌جا خبر می‌دهیم.\nمبلغ درخواست: " + MoneyText.Toman(waiting.LoanAmount);
            }
            else if (active != null)
            {
                var remain = active.RemainAmount ?? active.LoanAmount;
                body = name + "وام شما پرداخت شده است.\nمانده بدهی: " + MoneyText.Toman(remain) + "\nبرای پرداخت، دکمه «پرداخت قسط» را بزنید.";
            }
            else if (user.RoleId == (int)UserRoleType.Donator)
            {
                body = name + "شما جزو خیرین صندوق هستید.\nبرای کمک، دکمه «پرداخت کمک» را بزنید.";
            }
            else
            {
                body = name + "هنوز وام فعالی ندارید.\nاگر نیاز دارید، دکمه «درخواست وام» را بزنید.";
            }

            await SendHomeAsync(user, chatId, body, cancellationToken);
        }

        private async Task SendBalanceAsync(User user, string chatId, CancellationToken cancellationToken)
        {
            var installments = (await _repository.InstallmentRepository.GetByUserIdAsync(user.UserId, cancellationToken))
                .Where(i => i.Active != false)
                .ToList();
            var open = installments.Where(i => i.InstallmentStatus != (byte)InstallmentStatus.Cleared).OrderBy(i => i.InstallmentDate).ToList();
            var paid = installments.Count - open.Count;
            var debt = open.Sum(i => i.InstallmentAmount);

            var sb = new StringBuilder();
            sb.AppendLine("مانده بدهی شما: " + MoneyText.Toman(debt));
            sb.AppendLine("قسط پرداخت‌شده: " + paid);
            sb.AppendLine("قسط مانده: " + open.Count);
            if (open.Count > 0)
            {
                var next = open[0];
                sb.AppendLine("قسط بعدی: " + MoneyText.Toman(next.InstallmentAmount));
                sb.Append("سررسید: " + (next.InstallmentDate_shansi ?? PersianDateHelper.ToShamsi(next.InstallmentDate)));
            }
            else
            {
                sb.Append("قسط بازی برای پرداخت ندارید.");
            }

            await SendHomeAsync(user, chatId, sb.ToString().Trim(), cancellationToken);
        }

        private async Task SendPayInstallmentAsync(User user, string chatId, CancellationToken cancellationToken)
        {
            var open = await OpenInstallmentsAsync(user.UserId, cancellationToken);
            if (open.Count == 0)
            {
                await SendHomeAsync(user, chatId, "قسطی برای پرداخت ندارید.", cancellationToken);
                return;
            }

            var next = open[0];
            var due = next.InstallmentDate_shansi ?? PersianDateHelper.ToShamsi(next.InstallmentDate);
            var result = await _paymentService.StartZarinPalPaymentAsync(new ZarinPalRequestDto
            {
                UserId = user.UserId,
                Amount = next.InstallmentAmount,
                InstallmentId = next.InstallmentId,
                PaymentType = (byte)PaymentType.Installment,
                Description = "پرداخت قسط از بله"
            }, cancellationToken);

            var intro = "قسط بعدی شما\nمبلغ: " + MoneyText.Toman(next.InstallmentAmount) +
                        "\nسررسید: " + due +
                        "\nقسط مانده: " + open.Count +
                        "\nمبلغ کمی هم کارمزد بانک اضافه می‌شود.\nبعد از پرداخت، نتیجه همین‌جا می‌آید.";

            if (!result.Success)
            {
                var fallback = await BuildInstallmentPageLinkAsync(user, next, cancellationToken);
                await _bale.SendWithKeyboardAsync(chatId,
                    intro + "\n" + (result.Message ?? "درگاه الان باز نشد.") + "\nاین لینک را بزنید:\n" + fallback,
                    BaleKeyboards.InlineUrl("پرداخت قسط", fallback),
                    cancellationToken);
                return;
            }

            var url = ReadGatewayUrl(result.Data);
            if (string.IsNullOrWhiteSpace(url))
            {
                await SendHomeAsync(user, chatId, result.Message ?? "لینک بانک ساخته نشد.", cancellationToken);
                return;
            }

            await _bale.SendWithKeyboardAsync(chatId, intro + "\nدکمه زیر را بزنید. صفحه بانک باز می‌شود.", BaleKeyboards.InlineUrl("رفتن به صفحه بانک", url), cancellationToken);
        }

        private async Task SendLoanChoicesAsync(User user, string chatId, CancellationToken cancellationToken)
        {
            if (user.RoleId == (int)UserRoleType.Donator)
            {
                await SendHomeAsync(user, chatId, "خیرین وام نمی‌گیرند. برای کمک، دکمه «پرداخت کمک» را بزنید.", cancellationToken);
                return;
            }

            var active = await _repository.LoanRepository.GetActiveLoanByUserIdAsync(user.UserId, cancellationToken);
            if (active != null && active.LoanRequestStatus == (byte)LoanRequestStatus.WaitingInQueue)
            {
                await SendHomeAsync(user, chatId, "درخواست شما ثبت شده و در نوبت است. هر وقت نوبت شما شود خبر می‌دهیم.", cancellationToken);
                return;
            }

            if (active != null)
            {
                var remain = active.RemainAmount ?? active.LoanAmount;
                await SendHomeAsync(user, chatId, "یک وام باز دارید. تا تمام نشود درخواست جدید نمی‌توانید بدهید.\nمانده: " + MoneyText.Toman(remain), cancellationToken);
                return;
            }

            var max = await MaxLoanRialAsync(cancellationToken);
            var amounts = LoanAmountChoices(max);
            if (amounts.Count == 0)
            {
                await SendHomeAsync(user, chatId, "الان سقف وام تنظیم نشده است. با مسئول صندوق صحبت کنید.", cancellationToken);
                return;
            }

            var rows = amounts.Select(rial => new[] { new { text = MoneyText.Toman(rial), callback_data = "la:" + rial } }).ToArray();
            await _bale.SendWithKeyboardAsync(chatId,
                "مبلغ وام را انتخاب کنید. سقف: " + MoneyText.Toman(max),
                new { inline_keyboard = rows },
                cancellationToken);
        }

        private async Task RequestLoanAmountAsync(User user, string chatId, long amountRial, CancellationToken cancellationToken)
        {
            var result = await _loanService.RequestLoanAsync(new LoanRequestDto
            {
                UserId = user.UserId,
                LoanAmount = amountRial,
                Description = "درخواست از بله"
            }, cancellationToken);

            await SendHomeAsync(user, chatId, result.Message ?? (result.Success ? "درخواست ثبت شد." : "درخواست ثبت نشد."), cancellationToken);
        }

        private async Task BeginDonationAsync(User user, string chatId, CancellationToken cancellationToken)
        {
            var amount = user.MonthlyDonationAmount ?? 0;
            if (amount <= 0)
            {
                Pending[chatId] = "donation";
                await SendHomeAsync(user, chatId, "مبلغ کمک را به تومان بنویسید. مثال: 100000", cancellationToken);
                return;
            }

            await StartDonationAsync(user, chatId, amount, cancellationToken);
        }

        private async Task StartDonationAsync(User user, string chatId, long amountRial, CancellationToken cancellationToken)
        {
            if (amountRial <= 0)
            {
                await SendHomeAsync(user, chatId, "مبلغ کمک درست نیست.", cancellationToken);
                return;
            }

            var result = await _paymentService.StartZarinPalPaymentAsync(new ZarinPalRequestDto
            {
                UserId = user.UserId,
                Amount = amountRial,
                PaymentType = (byte)PaymentType.Donation,
                Description = "کمک خیرین از بله"
            }, cancellationToken);

            if (!result.Success)
            {
                await SendHomeAsync(user, chatId, result.Message ?? "پرداخت کمک باز نشد.", cancellationToken);
                return;
            }

            var url = ReadGatewayUrl(result.Data);
            if (string.IsNullOrWhiteSpace(url))
            {
                await SendHomeAsync(user, chatId, "لینک بانک ساخته نشد.", cancellationToken);
                return;
            }

            await _bale.SendWithKeyboardAsync(chatId,
                "مبلغ کمک: " + MoneyText.Toman(amountRial) + "\nدکمه زیر را بزنید. صفحه بانک باز می‌شود.",
                BaleKeyboards.InlineUrl("پرداخت کمک", url),
                cancellationToken);
        }

        private async Task SendQueueAsync(User user, string chatId, CancellationToken cancellationToken)
        {
            if (!await IsManagerAsync(user, cancellationToken))
            {
                await SendHomeAsync(user, chatId, "صف وام فقط برای مسئول صندوق است.", cancellationToken);
                return;
            }

            var queue = (await _repository.LoanRepository.GetWaitingQueueAsync(cancellationToken)).ToList();
            if (queue.Count == 0)
            {
                await SendHomeAsync(user, chatId, "الان کسی در صف وام نیست.", cancellationToken);
                return;
            }

            await _bale.SendWithKeyboardAsync(chatId,
                "صف وام: " + queue.Count + " نفر.\nنفر بالاتر زودتر وام می‌گیرد. با دکمه‌ها نوبت را عوض کنید، وام بدهید یا رد کنید.",
                BaleKeyboards.For(true, false),
                cancellationToken);

            var shown = queue.Take(8).ToList();
            for (var i = 0; i < shown.Count; i++)
            {
                var loan = shown[i];
                var name = loan.User?.FullName;
                if (string.IsNullOrWhiteSpace(name))
                    name = "عضو";
                var text = (i + 1) + ". " + name + "\n" + MoneyText.Toman(loan.LoanAmount);
                await _bale.SendWithKeyboardAsync(chatId, text, QueueItemKeyboard(loan.LoanId), cancellationToken);
            }

            if (queue.Count > shown.Count)
                await _bale.SendMessageAsync(chatId, "بقیه صف را در سایت، بخش «صف وام» ببینید.", cancellationToken);
        }

        private async Task MoveFromCallbackAsync(User user, string chatId, string payload, CancellationToken cancellationToken)
        {
            if (!await IsManagerAsync(user, cancellationToken))
            {
                await SendHomeAsync(user, chatId, "این کار فقط برای مسئول صندوق است.", cancellationToken);
                return;
            }

            var dir = payload.StartsWith("-") ? -1 : payload.StartsWith("+") ? 1 : 0;
            var idPart = payload.TrimStart('+', '-');
            if (dir == 0 || !int.TryParse(idPart, out var loanId))
                return;

            var result = await _loanService.MoveInQueueAsync(loanId, dir, user.UserId, cancellationToken);
            await _bale.SendMessageAsync(chatId, result.Message ?? "انجام شد.", cancellationToken);
            if (result.Success)
                await SendQueueNamesAsync(chatId, cancellationToken);
        }

        private async Task DisburseFromBaleAsync(User user, string chatId, int loanId, CancellationToken cancellationToken)
        {
            if (!await IsManagerAsync(user, cancellationToken))
            {
                await SendHomeAsync(user, chatId, "این کار فقط برای مسئول صندوق است.", cancellationToken);
                return;
            }

            var result = await _loanService.DisburseLoanAsync(new DisburseLoanDto
            {
                LoanId = loanId,
                ManagerUserId = user.UserId,
                Description = "پرداخت توسط مسئول صندوق از بله"
            }, cancellationToken);

            await _bale.SendMessageAsync(chatId, result.Message ?? (result.Success ? "وام پرداخت شد." : "وام پرداخت نشد."), cancellationToken);
        }

        private async Task RejectFromCallbackAsync(User user, string chatId, string data, CancellationToken cancellationToken)
        {
            var parts = data.Split(':');
            if (parts.Length < 3 || !int.TryParse(parts[1], out var loanId))
                return;

            Pending.TryRemove(chatId, out _);
            var reason = parts[2] == "1"
                ? "مدارک کامل نیست"
                : parts[2] == "2"
                    ? "فعلاً صندوق نمی‌تواند این وام را بدهد"
                    : "لطفاً بعداً دوباره درخواست کنید";
            await RejectFromBaleAsync(user, chatId, loanId, reason, cancellationToken);
        }

        private async Task RejectFromBaleAsync(User user, string chatId, int loanId, string reason, CancellationToken cancellationToken)
        {
            if (!await IsManagerAsync(user, cancellationToken))
            {
                await SendHomeAsync(user, chatId, "این کار فقط برای مسئول صندوق است.", cancellationToken);
                return;
            }

            var result = await _loanService.RejectLoanAsync(loanId, reason, user.UserId, cancellationToken);
            await _bale.SendMessageAsync(chatId, result.Success ? "درخواست رد شد و به متقاضی در بله خبر داده شد." : (result.Message ?? "رد نشد."), cancellationToken);
        }

        private async Task SendQueueNamesAsync(string chatId, CancellationToken cancellationToken)
        {
            var queue = (await _repository.LoanRepository.GetWaitingQueueAsync(cancellationToken)).Take(8).ToList();
            if (queue.Count == 0)
            {
                await _bale.SendMessageAsync(chatId, "صف خالی شد.", cancellationToken);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("ترتیب جدید صف:");
            for (var i = 0; i < queue.Count; i++)
            {
                var name = queue[i].User?.FullName;
                if (string.IsNullOrWhiteSpace(name))
                    name = "عضو";
                sb.AppendLine((i + 1) + ". " + name);
            }
            sb.Append("برای دکمه‌ها دوباره «صف وام» را بزنید.");
            await _bale.SendMessageAsync(chatId, sb.ToString().Trim(), cancellationToken);
        }

        private async Task<List<Installment>> OpenInstallmentsAsync(int userId, CancellationToken cancellationToken)
        {
            return (await _repository.InstallmentRepository.GetByUserIdAsync(userId, cancellationToken))
                .Where(i => i.Active != false && i.InstallmentStatus != (byte)InstallmentStatus.Cleared)
                .OrderBy(i => i.InstallmentDate)
                .ToList();
        }

        private async Task<string> BuildInstallmentPageLinkAsync(User user, Installment installment, CancellationToken cancellationToken)
        {
            var baseUrl = await AppUrlHelper.GetBaseUrlAsync(_repository, _configuration, cancellationToken);
            var signingKey = _configuration["App:LinkSigningKey"] ?? _configuration["ZarinPal:MerchantId"];
            var token = SecureLinkHelper.CreateInstallmentPayToken(installment.InstallmentId, user.UserId, TimeSpan.FromDays(14), signingKey);
            return baseUrl.TrimEnd('/') + "/pay-installment.html?token=" + Uri.EscapeDataString(token);
        }

        private async Task<long> MaxLoanRialAsync(CancellationToken cancellationToken)
        {
            var maxLoanStr = await _repository.SettingRepository.GetValueAsync("MaxLoanAmount", "50000000", cancellationToken);
            long.TryParse(maxLoanStr, out var maxLoan);
            return maxLoan <= 0 ? 50000000 : maxLoan;
        }

        private async Task<bool> IsManagerAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null)
                return false;
            if (user.UserStatus == (byte)UserStatus.Manager || user.RoleId == (int)UserRoleType.Manager)
                return true;
            var managers = await _repository.UserRepository.GetManagersAsync(cancellationToken);
            return managers.Any(m => m.UserId == user.UserId);
        }

        private static List<long> LoanAmountChoices(long maxRial)
        {
            long[] tomanSteps = { 1000000, 2000000, 3000000, 5000000, 10000000 };
            var list = new List<long>();
            foreach (var toman in tomanSteps)
            {
                var rial = toman * 10;
                if (rial <= maxRial)
                    list.Add(rial);
            }

            if (maxRial > 0 && !list.Contains(maxRial))
                list.Add(maxRial);
            return list;
        }

        private static object QueueItemKeyboard(int loanId)
        {
            return new
            {
                inline_keyboard = new[]
                {
                    new[]
                    {
                        new { text = "جلوتر", callback_data = "q:-" + loanId },
                        new { text = "عقب‌تر", callback_data = "q:+" + loanId }
                    },
                    new[]
                    {
                        new { text = "وام را بده", callback_data = "d:" + loanId },
                        new { text = "رد کن", callback_data = "x:" + loanId }
                    }
                }
            };
        }

        private static object RejectReasonKeyboard(int loanId)
        {
            return new
            {
                inline_keyboard = new[]
                {
                    new[] { new { text = "مدارک کامل نیست", callback_data = "rr:" + loanId + ":1" } },
                    new[] { new { text = "فعلاً صندوق نمی‌تواند", callback_data = "rr:" + loanId + ":2" } },
                    new[] { new { text = "بعداً دوباره درخواست کند", callback_data = "rr:" + loanId + ":3" } }
                }
            };
        }

        private static string ReadGatewayUrl(object data)
        {
            if (data == null)
                return null;
            var json = JObject.FromObject(data);
            return json["GatewayUrl"]?.ToString() ?? json["gatewayUrl"]?.ToString();
        }

        private static bool TryReadMoney(string text, out long toman)
        {
            toman = 0;
            var digits = Regex.Replace(NormalizeDigits(text ?? ""), @"[^\d]", "");
            if (digits.Length < 4 || digits.Length > 12)
                return false;
            return long.TryParse(digits, out toman) && toman > 0;
        }

        private static bool IsMenuCommand(string norm)
        {
            switch (norm)
            {
                case "وضعیت من":
                case "وضعیت":
                case "مانده من":
                case "مانده":
                case "مانده و قسطها":
                case "مانده و قسط ها":
                case "پرداخت قسط":
                case "قسط":
                case "درخواست وام":
                case "وام":
                case "پرداخت کمک":
                case "کمک":
                case "صف وام":
                case "صف":
                case "راهنما":
                    return true;
                default:
                    return norm.StartsWith("/start");
            }
        }

        private static string Normalize(string text)
        {
            var value = NormalizeDigits(text ?? "").Trim();
            value = value.Replace("ي", "ی").Replace("ك", "ک").Replace("\u200c", "").Replace("‌", "");
            return value;
        }

        private static string NormalizeDigits(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            var chars = text.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= '۰' && chars[i] <= '۹')
                    chars[i] = (char)('0' + (chars[i] - '۰'));
                else if (chars[i] >= '٠' && chars[i] <= '٩')
                    chars[i] = (char)('0' + (chars[i] - '٠'));
            }
            return new string(chars);
        }
    }
}

using Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using Services.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;
        public AdminController(IServiceManager serviceManager) => _serviceManager = serviceManager;

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard([FromQuery] int managerUserId, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(managerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            return Ok(await _serviceManager.AdminService.GetDashboardAsync(cancellationToken));
        }

        [HttpGet("reports/management")]
        public async Task<IActionResult> ManagementReport([FromQuery] int managerUserId, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(managerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            return Ok(await _serviceManager.AdminService.GetManagementReportAsync(cancellationToken));
        }

        [HttpGet("reports/installments")]
        public async Task<IActionResult> InstallmentCollectionReport([FromQuery] int managerUserId, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(managerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            return Ok(await _serviceManager.AdminService.GetInstallmentCollectionReportAsync(cancellationToken));
        }

        [HttpGet("notification-logs")]
        public async Task<IActionResult> NotificationLogs([FromQuery] int managerUserId, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(managerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            return Ok(await _serviceManager.AdminService.GetNotificationLogsAsync(cancellationToken));
        }

        [HttpGet("settings")]
        public async Task<IActionResult> Settings([FromQuery] int managerUserId, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(managerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            return Ok(await _serviceManager.AdminService.GetSettingsAsync(cancellationToken));
        }

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSetting([FromBody] UpdateSettingDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.AdminService.UpdateSettingAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("fund-transactions")]
        public async Task<IActionResult> FundTransactions([FromQuery] int managerUserId, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(managerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            return Ok(await _serviceManager.AdminService.GetFundTransactionsAsync(cancellationToken));
        }

        [HttpPost("fund-deposit")]
        public async Task<IActionResult> FundDeposit([FromBody] FundDepositDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.AdminService.DepositFundAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("send-monthly-reminders")]
        public async Task<IActionResult> SendReminders([FromBody] AdminActionDto dto, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            var result = await _serviceManager.NotificationSchedulerService.SendMonthlyRemindersAsync(cancellationToken);
            var message = result.Logs.Count == 0
                ? "موردی برای یادآوری یافت نشد (اقساط عقب\u200cافتاده یا سررسید ۳ روز دیگر)"
                : $"ارسال شد: {result.SentCount} | ناموفق: {result.FailedCount}" +
                  (result.SkippedCount > 0 ? $" | بدون بله: {result.SkippedCount}" : "");
            return Ok(ApiResultDto.Ok(message, result));
        }

        [HttpPost("process-loan-queue")]
        public async Task<IActionResult> ProcessQueue([FromBody] AdminActionDto dto, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            await _serviceManager.NotificationSchedulerService.ProcessLoanQueueAsync(cancellationToken);
            return Ok(ApiResultDto.Ok("صف وام پردازش شد"));
        }
    }
}

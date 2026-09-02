using Contracts.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Abstractions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/loan")]
    public class LoanController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;
        public LoanController(IServiceManager serviceManager) => _serviceManager = serviceManager;

        [HttpPost("request")]
        public async Task<IActionResult> RequestLoan([FromBody] LoanRequestDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.LoanService.RequestLoanAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("disburse")]
        public async Task<IActionResult> Disburse([FromBody] DisburseLoanDto dto, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            var result = await _serviceManager.LoanService.DisburseLoanAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("reject")]
        public async Task<IActionResult> Reject([FromBody] RejectLoanDto dto, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            var result = await _serviceManager.LoanService.RejectLoanAsync(dto.LoanId, dto.Reason, dto.ManagerUserId, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("queue")]
        public async Task<IActionResult> Queue([FromQuery] int managerUserId, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(managerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            return Ok(await _serviceManager.LoanService.GetQueueAsync(cancellationToken));
        }

        [HttpGet("all")]
        public async Task<IActionResult> All([FromQuery] int managerUserId, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(managerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            return Ok(await _serviceManager.LoanService.GetAllAsync(cancellationToken));
        }

        [HttpGet("waiting-count")]
        public async Task<IActionResult> WaitingCount(CancellationToken cancellationToken) =>
            Ok(new { count = await _serviceManager.LoanService.GetWaitingCountAsync(cancellationToken) });

        [HttpGet("disbursed-count")]
        public async Task<IActionResult> DisbursedCount(CancellationToken cancellationToken) =>
            Ok(new { count = await _serviceManager.LoanService.GetDisbursedCountAsync(cancellationToken) });

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> ByUser(int userId, CancellationToken cancellationToken) =>
            Ok(await _serviceManager.LoanService.GetByUserIdAsync(userId, cancellationToken));

        [HttpGet("{loanId:int}")]
        public async Task<IActionResult> Get(int loanId, CancellationToken cancellationToken)
        {
            var loan = await _serviceManager.LoanService.GetByIdAsync(loanId, cancellationToken);
            return loan == null ? NotFound() : Ok(loan);
        }
    }

    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;
        public PaymentController(IServiceManager serviceManager) => _serviceManager = serviceManager;

        [HttpPost("zarinpal/request")]
        public async Task<IActionResult> ZarinPalRequest([FromBody] ZarinPalRequestDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.PaymentService.StartZarinPalPaymentAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("zarinpal/callback")]
        public async Task<IActionResult> ZarinPalCallback([FromQuery] string Authority, [FromQuery] string Status, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.PaymentService.VerifyZarinPalPaymentAsync(Authority, Status, cancellationToken);
            if (result.Success)
                return Redirect($"/payment-result.html?ok=1&msg={Uri.EscapeDataString(result.Message)}");
            return Redirect($"/payment-result.html?ok=0&msg={Uri.EscapeDataString(result.Message)}");
        }

        [HttpPost("manual")]
        public async Task<IActionResult> Manual([FromBody] ManualPaymentDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.PaymentService.SubmitManualPaymentAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> ByUser(int userId, CancellationToken cancellationToken) =>
            Ok(await _serviceManager.PaymentService.GetByUserIdAsync(userId, cancellationToken));

        [HttpGet]
        public async Task<IActionResult> All([FromQuery] int managerUserId, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(managerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            return Ok(await _serviceManager.PaymentService.GetAllAsync(cancellationToken));
        }
    }

    [ApiController]
    [Route("api/panel")]
    public class PanelController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;
        public PanelController(IServiceManager serviceManager) => _serviceManager = serviceManager;

        [HttpGet("{userId:int}")]
        public async Task<IActionResult> Get(int userId, CancellationToken cancellationToken)
        {
            var panel = await _serviceManager.MemberPanelService.GetPanelAsync(userId, cancellationToken);
            return panel == null ? NotFound() : Ok(panel);
        }
    }

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

            await _serviceManager.NotificationSchedulerService.SendMonthlyRemindersAsync(cancellationToken);
            return Ok(ApiResultDto.Ok("یادآورها ارسال شد"));
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

    [ApiController]
    [Route("api/upload")]
    public class UploadController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;
        public UploadController(IServiceManager serviceManager) => _serviceManager = serviceManager;

        [HttpPost("national-id/{userId:int}")]
        public async Task<IActionResult> UploadNationalId(int userId, IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResultDto.Fail("فایل ارسال نشده است"));

            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "national-ids");
            Directory.CreateDirectory(uploads);
            var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(uploads, fileName);
            await using (var stream = System.IO.File.Create(path))
                await file.CopyToAsync(stream, cancellationToken);

            var relative = $"/uploads/national-ids/{fileName}";
            var result = await _serviceManager.UserService.UploadNationalIdAsync(userId, relative, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}

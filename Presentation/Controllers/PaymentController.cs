using Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using Services.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
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

        [HttpGet("installment-link")]
        public async Task<IActionResult> InstallmentLink([FromQuery] string token, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.PaymentService.GetInstallmentPayLinkAsync(token, cancellationToken);
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

        /// <summary>
        /// بازگشت مرورگر از هاب پرداخت نیکو پس از Confirm زرین‌پال.
        /// </summary>
        [HttpGet("nikoo/return")]
        public async Task<IActionResult> NikooReturn(
            [FromQuery] string token,
            [FromQuery] string status,
            [FromQuery] string amount,
            [FromQuery] string externalRef,
            [FromQuery] string refId,
            [FromQuery] string timestamp,
            [FromQuery] string signature,
            CancellationToken cancellationToken)
        {
            var result = await _serviceManager.PaymentService.CompleteNikooPaymentAsync(
                token, status, amount, externalRef, refId, timestamp, signature, cancellationToken);
            if (result.Success)
                return Redirect($"/payment-result.html?ok=1&msg={Uri.EscapeDataString(result.Message)}");
            return Redirect($"/payment-result.html?ok=0&msg={Uri.EscapeDataString(result.Message)}");
        }

        /// <summary>
        /// نوتیفیکیشن سرور به سرور از هاب نیکو (اگر کاربر صفحه را ببندد).
        /// </summary>
        [HttpPost("nikoo/notify")]
        public async Task<IActionResult> NikooNotify([FromBody] NikooNotifyDto dto, CancellationToken cancellationToken)
        {
            if (dto == null)
                return BadRequest(ApiResultDto.Fail("بدنه خالی است"));

            var result = await _serviceManager.PaymentService.CompleteNikooPaymentAsync(
                dto.Token,
                dto.Status,
                dto.Amount?.ToString(),
                dto.ExternalRef,
                dto.RefId?.ToString(),
                dto.Timestamp,
                dto.Signature,
                cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
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
}

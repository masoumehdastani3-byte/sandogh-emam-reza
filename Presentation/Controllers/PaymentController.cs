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
}

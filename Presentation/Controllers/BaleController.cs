using Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using Services.Abstractions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/bale")]
    public class BaleController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public BaleController(IServiceManager serviceManager) => _serviceManager = serviceManager;

        /// <summary>وب‌هوک بله — آدرس عمومی HTTPS را در setWebhook ثبت کنید</summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();
            var result = await _serviceManager.BaleMenuService.ProcessIncomingUpdateJsonAsync(json, cancellationToken);
            return Ok(result);
        }

        /// <summary>دریافت دستی آپدیت‌ها و ذخیره chat_id (مناسب Localhost)</summary>
        [HttpPost("poll")]
        public async Task<IActionResult> Poll(CancellationToken cancellationToken)
        {
            var result = await _serviceManager.BaleMenuService.PollAsync(cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("set-webhook")]
        public async Task<IActionResult> SetWebhook([FromQuery] string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
                url = $"{Request.Scheme}://{Request.Host}/api/bale/webhook";
            var result = await _serviceManager.BaleService.SetWebhookAsync(url, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("bot-link/{userId:int}")]
        public IActionResult BotLink(int userId)
        {
            var link = _serviceManager.BaleService.GetBotStartLink(userId);
            return Ok(ApiResultDto.Ok("لینک ربات", new { userId, botLink = link }));
        }
    }
}

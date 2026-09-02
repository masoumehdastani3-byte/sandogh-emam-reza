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

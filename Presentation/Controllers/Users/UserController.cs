using Contracts.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Abstractions;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Presentation.Controllers.Users
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public UserController(IServiceManager serviceManager) => _serviceManager = serviceManager;

        [HttpGet]
        public async Task<IActionResult> Get(string userName, int? userId, string fullName, string cardNumber, string mobile, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.UserService.GetFilterAsync(userName, userId, fullName, cardNumber, mobile, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.UserService.GetByUserId(id, cancellationToken);
            if (result == null) return NotFound();
            result.Password = null;
            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterMemberDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.UserService.RegisterAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.UserService.LoginAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        [HttpPost("login/request-otp")]
        public async Task<IActionResult> RequestLoginOtp([FromBody] RequestLoginOtpDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.UserService.RequestLoginOtpAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("login/verify-otp")]
        public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.UserService.VerifyLoginOtpAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        [HttpPost("login/token")]
        public async Task<IActionResult> VerifyLoginToken([FromBody] VerifyLoginTokenDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.UserService.VerifyLoginTokenAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        /// <summary>لینک ورود از بله. نشست را می‌سازد و کاربر را داخل پنل می‌برد، نه فرم کد ملی.</summary>
        [HttpGet("/enter/{token}")]
        public async Task<IActionResult> EnterFromBale(string token, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.UserService.VerifyLoginTokenAsync(
                new VerifyLoginTokenDto { LoginToken = token }, cancellationToken);
            var user = result.Data as UserDto;
            if (!result.Success || user == null || user.UserId <= 0)
            {
                var message = string.IsNullOrWhiteSpace(result.Message) ? "لینک ورود نامعتبر است" : result.Message;
                return Redirect("/login.html?linkError=" + Uri.EscapeDataString(message));
            }

            var roleName = user.RoleName ?? user.UserRole?.RoleName ?? "";
            var next = LoggedInUrl(user, roleName);
            var script =
                "try{" +
                "localStorage.setItem('gl_userId'," + JsonSerializer.Serialize(user.UserId.ToString()) + ");" +
                "localStorage.setItem('gl_fullName'," + JsonSerializer.Serialize(user.FullName ?? "") + ");" +
                "localStorage.setItem('gl_roleId'," + JsonSerializer.Serialize(user.RoleId.ToString()) + ");" +
                "localStorage.setItem('gl_roleName'," + JsonSerializer.Serialize(roleName) + ");" +
                "localStorage.setItem('gl_userStatus'," + JsonSerializer.Serialize(user.UserStatus.ToString()) + ");" +
                "localStorage.setItem('gl_nationalCode'," + JsonSerializer.Serialize(user.NationalCode ?? "") + ");" +
                "}catch(e){}" +
                "location.replace(" + JsonSerializer.Serialize(next) + ");";

            var html = "<!DOCTYPE html><html lang=\"fa\" dir=\"rtl\"><head><meta charset=\"utf-8\">" +
                       "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
                       "<title>ورود</title></head><body><p>در حال ورود به سایت...</p><script>" +
                       script + "</script></body></html>";
            return Content(html, "text/html; charset=utf-8");
        }

        private static string LoggedInUrl(UserDto user, string roleName)
        {
            var nationalCode = user.NationalCode ?? "";
            var multi = nationalCode == "0058715940";
            var manager = user.UserStatus == 5
                || roleName == "مدیر صندوق"
                || string.Equals(roleName, "admin", StringComparison.OrdinalIgnoreCase)
                || user.RoleId == 1;
            var page = multi ? "login.html" : (manager ? "admin.html" : "panel.html");
            var query = new QueryString()
                .Add("userId", user.UserId.ToString())
                .Add("fullName", user.FullName ?? "")
                .Add("roleId", user.RoleId.ToString())
                .Add("roleName", roleName ?? "")
                .Add("userStatus", user.UserStatus.ToString())
                .Add("nationalCode", nationalCode);
            if (multi)
                query = query.Add("choose", "1");
            return "/" + page + query.ToUriComponent();
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.UserService.UpdateProfileAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] Domain.Entities.User user, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _serviceManager.UserService.Insert(user, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.UserService.Delete(id, cancellationToken);
            return Ok(result);
        }
    }
}

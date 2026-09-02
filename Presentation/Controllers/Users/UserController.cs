using Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using Services.Abstractions;
using System;
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

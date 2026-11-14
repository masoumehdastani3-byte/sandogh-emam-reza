using Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using Services.Abstractions;
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

        [HttpPost("import-past")]
        public async Task<IActionResult> ImportPast([FromBody] ImportPastLoanDto dto, CancellationToken cancellationToken)
        {
            if (dto == null)
                return BadRequest(ApiResultDto.Fail("درخواست نامعتبر است"));
            if (!await _serviceManager.AdminService.IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            var result = await _serviceManager.LoanService.ImportPastLoanAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("move")]
        public async Task<IActionResult> Move([FromBody] MoveQueueDto dto, CancellationToken cancellationToken)
        {
            if (dto == null)
                return BadRequest(ApiResultDto.Fail("درخواست نامعتبر است"));
            if (!await _serviceManager.AdminService.IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            var result = await _serviceManager.LoanService.MoveInQueueAsync(dto.LoanId, dto.Direction, dto.ManagerUserId, cancellationToken);
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
        public async Task<IActionResult> WaitingCount([FromQuery] int managerUserId, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(managerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            return Ok(new { count = await _serviceManager.LoanService.GetWaitingCountAsync(cancellationToken) });
        }

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
}

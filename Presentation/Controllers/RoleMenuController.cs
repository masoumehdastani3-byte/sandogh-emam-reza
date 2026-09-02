using Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using Services.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RoleMenuController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;
        public RoleMenuController(IServiceManager serviceManager) => _serviceManager = serviceManager;

        [HttpGet]
        public async Task<IActionResult> GetRoles([FromQuery] int managerUserId, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(managerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            return Ok(await _serviceManager.RoleMenuService.GetRolesWithMenusAsync(cancellationToken));
        }

        [HttpGet("menus")]
        public async Task<IActionResult> GetMenus([FromQuery] int managerUserId, CancellationToken cancellationToken)
        {
            if (!await _serviceManager.AdminService.IsManagerAsync(managerUserId, cancellationToken))
                return Unauthorized(ApiResultDto.Fail("فقط مدیر صندوق مجاز است"));

            return Ok(await _serviceManager.RoleMenuService.GetMenusAsync(cancellationToken));
        }

        [HttpGet("my-menus/{userId:int}")]
        public async Task<IActionResult> GetMyMenus(int userId, CancellationToken cancellationToken) =>
            Ok(await _serviceManager.RoleMenuService.GetMenusForUserAsync(userId, cancellationToken));

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] SaveRoleDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.RoleMenuService.CreateRoleAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{roleId:int}")]
        public async Task<IActionResult> UpdateRole(int roleId, [FromBody] UpdateRoleDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.RoleMenuService.UpdateRoleAsync(roleId, dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{roleId:int}")]
        public async Task<IActionResult> DeleteRole(int roleId, [FromQuery] int managerUserId, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.RoleMenuService.DeleteRoleAsync(roleId, managerUserId, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("access")]
        public async Task<IActionResult> SaveAccess([FromBody] SaveRoleMenuAccessDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.RoleMenuService.SaveRoleMenuAccessAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("menus")]
        public async Task<IActionResult> CreateMenu([FromBody] SaveMenuDto dto, CancellationToken cancellationToken)
        {
            var result = await _serviceManager.RoleMenuService.CreateMenuAsync(dto, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}

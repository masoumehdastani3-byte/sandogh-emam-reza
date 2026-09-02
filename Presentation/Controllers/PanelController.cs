using Microsoft.AspNetCore.Mvc;
using Services.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
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
}

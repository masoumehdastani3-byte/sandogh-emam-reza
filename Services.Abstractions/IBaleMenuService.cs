using Contracts.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Abstractions
{
    public interface IBaleMenuService
    {
        Task<ApiResultDto> ProcessIncomingUpdateJsonAsync(string json, CancellationToken cancellationToken = default);
        Task<ApiResultDto> PollAsync(CancellationToken cancellationToken = default);
    }
}

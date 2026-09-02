using Domain.Repositories;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Helpers
{
    public static class AppUrlHelper
    {
        public const string AppBaseUrlSettingName = "AppBaseUrl";

        public static async Task<string> GetBaseUrlAsync(
            IRepositoryManager repositoryManager,
            IConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            var fromDb = await repositoryManager.SettingRepository
                .GetValueAsync(AppBaseUrlSettingName, null, cancellationToken);

            var url = !string.IsNullOrWhiteSpace(fromDb)
                ? fromDb
                : (configuration?["App:BaseUrl"] ?? "http://localhost:5000");

            return url.Trim().TrimEnd('/');
        }
    }
}

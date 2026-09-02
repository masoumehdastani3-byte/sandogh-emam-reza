using Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface ISettingRepository
    {
        Task<IEnumerable<Setting>> GetBySettingProcAsync(string Lang, string UserName, string Category);
        void Insert(Setting setting);
        void Update(Setting setting);
        Task<Setting> GetByNameAsync(string settingName, CancellationToken cancellationToken = default);
        Task<IEnumerable<Setting>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<string> GetValueAsync(string settingName, string defaultValue = null, CancellationToken cancellationToken = default);
    }
}

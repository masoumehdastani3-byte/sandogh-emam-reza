using Domain.Entities;
using Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Persistence.Repositories
{
    public class SettingRepository : ISettingRepository
    {
        private readonly RepositoryDbContext _dbContext;
        private readonly ADOConnection<Setting> _aDOConnection;

        public SettingRepository(RepositoryDbContext dbContext, ADOConnection<Setting> aDOConnection)
        {
            _dbContext = dbContext;
            _aDOConnection = aDOConnection;
        }

        public async Task<IEnumerable<Setting>> GetBySettingProcAsync(string Lang, string UserName, string Category)
        {
            SqlParameter[] parameters =
            {
                new SqlParameter("@Lang", SqlDbType.VarChar, 10) { Value = Lang },
                new SqlParameter("@UserName", SqlDbType.VarChar, 20) { Value = UserName },
                new SqlParameter("@Category", SqlDbType.VarChar, 50) { Value = Category },
            };
            var list = await _aDOConnection.ExecutionCommand("sp_Settings_Select", parameters, new Setting());
            return list;
        }

        public void Insert(Setting setting) => _dbContext.Settings.Add(setting);

        public void Update(Setting setting) => _dbContext.Settings.Update(setting);

        public async Task<Setting> GetByNameAsync(string settingName, CancellationToken cancellationToken = default) =>
            await _dbContext.Settings.FirstOrDefaultAsync(a => a.SettingName == settingName, cancellationToken);

        public async Task<IEnumerable<Setting>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _dbContext.Settings.ToListAsync(cancellationToken);

        public async Task<string> GetValueAsync(string settingName, string defaultValue = null, CancellationToken cancellationToken = default)
        {
            var setting = await GetByNameAsync(settingName, cancellationToken);
            return setting?.SettingValue ?? defaultValue;
        }
    }
}

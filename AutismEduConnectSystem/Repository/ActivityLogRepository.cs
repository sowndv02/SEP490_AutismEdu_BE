using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Repository
{
    public class ActivityLogRepository : Repository<ActivityLog>, IActivityLogRepository
    {
        private readonly ApplicationDbContext _context;
        public ActivityLogRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ActivityLog> UpdateAsync(ActivityLog model)
        {
            try
            {
                _context.ActivityLogs.Update(model);
                await _context.SaveChangesAsync();
                return model;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}

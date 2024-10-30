using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class ScheduleRepository : Repository<Schedule>, IScheduleRepository
    {
        private readonly ApplicationDbContext _context;

        public ScheduleRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Schedule> UpdateAsync(Schedule model)
        {
            try
            {
                _context.Schedules.Update(model);
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

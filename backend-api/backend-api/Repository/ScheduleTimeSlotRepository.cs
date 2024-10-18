using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class ScheduleTimeSlotRepository : Repository<ScheduleTimeSlot>, IScheduleTimeSlotRepository
    {
        private readonly ApplicationDbContext _context;

        public ScheduleTimeSlotRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}

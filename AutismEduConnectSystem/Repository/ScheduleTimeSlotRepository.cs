using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Repository
{
    public class ScheduleTimeSlotRepository : Repository<ScheduleTimeSlot>, IScheduleTimeSlotRepository
    {
        private readonly ApplicationDbContext _context;

        public ScheduleTimeSlotRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ScheduleTimeSlot> UpdateAsync(ScheduleTimeSlot model)
        {
            try
            {
                _context.ScheduleTimeSlots.Update(model);
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

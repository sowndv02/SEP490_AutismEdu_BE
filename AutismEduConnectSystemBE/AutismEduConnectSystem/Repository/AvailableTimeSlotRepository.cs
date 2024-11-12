using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Repository
{
    public class AvailableTimeSlotRepository : Repository<AvailableTimeSlot>, IAvailableTimeSlotRepository
    {
        private readonly ApplicationDbContext _context;

        public AvailableTimeSlotRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<AvailableTimeSlot> UpdateAsync(AvailableTimeSlot model)
        {
            try
            {
                _context.AvailableTimeSlots.Update(model);
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

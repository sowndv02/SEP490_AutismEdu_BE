using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
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

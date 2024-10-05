using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using System.Linq.Expressions;

namespace backend_api.Repository
{
    public class AvailableTimeRepository : Repository<AvailableTime>, IAvailableTimeRepository
    {
        private readonly ApplicationDbContext _context;

        public AvailableTimeRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<AvailableTime> UpdateAsync(AvailableTime model)
        {
            try
            {
                _context.AvailableTimes.Update(model);
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

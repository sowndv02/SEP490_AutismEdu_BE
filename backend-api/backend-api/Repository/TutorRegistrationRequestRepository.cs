using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class TutorRegistrationRequestRepository : Repository<TutorRegistrationRequest>, ITutorRegistrationRequestRepository
    {
        private readonly ApplicationDbContext _context;
        public TutorRegistrationRequestRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<TutorRegistrationRequest> UpdateAsync(TutorRegistrationRequest model)
        {
            try
            {
                _context.TutorRegistrationRequests.Update(model);
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

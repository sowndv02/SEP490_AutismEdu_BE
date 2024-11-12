using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Repository
{
    public class TutorProfileUpdateRequestRepository : Repository<TutorProfileUpdateRequest>, ITutorProfileUpdateRequestRepository
    {
        private readonly ApplicationDbContext _context;
        public TutorProfileUpdateRequestRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<TutorProfileUpdateRequest> UpdateAsync(TutorProfileUpdateRequest model)
        {
            try
            {
                _context.TutorProfileUpdateRequests.Update(model);
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

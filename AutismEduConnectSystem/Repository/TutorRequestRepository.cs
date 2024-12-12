using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Repository
{
    public class TutorRequestRepository : Repository<TutorRequest>, ITutorRequestRepository
    {
        private readonly ApplicationDbContext _context;
        public TutorRequestRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<TutorRequest> UpdateAsync(TutorRequest model)
        {
            try
            {
                _context.TutorRequests.Update(model);
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

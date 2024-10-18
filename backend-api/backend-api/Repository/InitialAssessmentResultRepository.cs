using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class InitialAssessmentResultRepository : Repository<InitialAssessmentResult>, IInitialAssessmentResultRepository
    {
        private readonly ApplicationDbContext _context;

        public InitialAssessmentResultRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}

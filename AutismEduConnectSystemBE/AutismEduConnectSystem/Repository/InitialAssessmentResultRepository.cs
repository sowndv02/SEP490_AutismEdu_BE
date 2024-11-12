using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Repository
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

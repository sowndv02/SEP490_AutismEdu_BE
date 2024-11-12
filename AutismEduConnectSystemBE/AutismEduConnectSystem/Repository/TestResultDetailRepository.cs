using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Repository
{
    public class TestResultDetailRepository : Repository<TestResultDetail>, ITestResultDetailRepository
    {
        private readonly ApplicationDbContext _context;

        public TestResultDetailRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}

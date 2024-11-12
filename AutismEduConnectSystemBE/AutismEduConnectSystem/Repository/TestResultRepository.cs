using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Repository
{
    public class TestResultRepository : Repository<TestResult>, ITestResultRepository
    {
        private readonly ApplicationDbContext _context;

        public TestResultRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}

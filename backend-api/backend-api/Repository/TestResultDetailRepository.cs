using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
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

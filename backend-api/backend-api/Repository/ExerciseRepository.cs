using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;
using System.Linq.Expressions;

namespace backend_api.Repository
{
    public class ExerciseRepository : Repository<Exercise>, IExerciseRepository
    {
        private readonly ApplicationDbContext _context;
        public ExerciseRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<Exercise> UpdateAsync(Exercise model)
        {
            throw new NotImplementedException();
        }
    }
}

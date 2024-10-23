using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace backend_api.Repository
{
    public class ExerciseTypeRepository : Repository<ExerciseType>, IExerciseTypeRepository
    {
        private readonly ApplicationDbContext _context;
        public ExerciseTypeRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ExerciseType> GetExerciseNameByID(int exerciseTypeId)
        {
            return await _context.ExerciseType.FirstOrDefaultAsync(e => e.Id == exerciseTypeId);
        }

        public Task<ExerciseType> UpdateAsync(ExerciseType model)
        {
            throw new NotImplementedException();
        }
    }
}

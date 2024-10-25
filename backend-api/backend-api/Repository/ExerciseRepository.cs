using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
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

        public async Task<(int TotalCount, List<Exercise>)> GetByExerciseTypeAsync(int exerciseTypeId)
        {
            // Get all exercises matching the provided ExerciseTypeId
            var exercises = await _context.Exercise
                                     .Where(e => e.ExerciseTypeId == exerciseTypeId)
                                     .ToListAsync();

            // Get the total count of the exercises that match the ExerciseTypeId
            var totalCount = exercises.Count;

            // Return the total count and the list of exercises
            return (totalCount, exercises);
        }

        public Task<Exercise> UpdateAsync(Exercise model)
        {
            throw new NotImplementedException();
        }
    }
}

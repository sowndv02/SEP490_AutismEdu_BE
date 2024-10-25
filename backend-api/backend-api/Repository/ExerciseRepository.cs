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
            var exercises = await _context.Exercise
                                     .Where(e => e.ExerciseTypeId == exerciseTypeId)
                                     .ToListAsync();

            var totalCount = exercises.Count;

            return (totalCount, exercises);
        }

        public async Task<Exercise> UpdateAsync(Exercise model)
        {
            try
            {
                _context.Exercise.Update(model);
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

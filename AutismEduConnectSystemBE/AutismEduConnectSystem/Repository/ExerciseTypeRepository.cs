using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AutismEduConnectSystem.Repository
{
    public class ExerciseTypeRepository : Repository<ExerciseType>, IExerciseTypeRepository
    {
        private readonly ApplicationDbContext _context;
        public ExerciseTypeRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        
        public async Task<ExerciseType> UpdateAsync(ExerciseType model)
        {
            try
            {
                _context.ExerciseTypes.Update(model);
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

using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AutismEduConnectSystem.Repository
{
    public class SyllabusExerciseRepository : Repository<SyllabusExercise>, ISyllabusExerciseRepository
    {
        private readonly ApplicationDbContext _context;
        public SyllabusExerciseRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<SyllabusExercise> UpdateAsync(SyllabusExercise model)
        {
            try
            {
                _context.SyllabusExercises.Update(model);
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

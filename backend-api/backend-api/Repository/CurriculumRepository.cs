using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class CurriculumRepository : Repository<Curriculum>, ICurriculumRepository
    {
        private readonly ApplicationDbContext _context;
        public CurriculumRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Curriculum> UpdateAsync(Curriculum model)
        {
            try
            {
                _context.Curriculums.Update(model);
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

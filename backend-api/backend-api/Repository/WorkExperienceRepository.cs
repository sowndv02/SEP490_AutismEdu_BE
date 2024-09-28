using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class WorkExperienceRepository : Repository<WorkExperience>, IWorkExperienceRepository
    {
        private readonly ApplicationDbContext _context;
        public WorkExperienceRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<WorkExperience> UpdateAsync(WorkExperience model)
        {
            try
            {
                _context.WorkExperiences.Update(model);
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

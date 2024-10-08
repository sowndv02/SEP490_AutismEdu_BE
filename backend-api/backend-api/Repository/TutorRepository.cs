using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class TutorRepository : Repository<Tutor>, ITutorRepository
    {
        private readonly ApplicationDbContext _context;
        public TutorRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Tutor> UpdateAsync(Tutor tutor)
        {
            try
            {
                _context.Tutors.Update(tutor);
                await _context.SaveChangesAsync();
                return tutor;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}

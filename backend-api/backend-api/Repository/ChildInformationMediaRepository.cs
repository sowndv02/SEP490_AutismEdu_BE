using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class ChildInformationMediaRepository : Repository<ChildInformationMedia>, IChildInformationMediaRepository
    {
        private readonly ApplicationDbContext _context;

        public ChildInformationMediaRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ChildInformationMedia> UpdateAsync(ChildInformationMedia model)
        {
            try
            {
                _context.ChildInformationMedias.Update(model);
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

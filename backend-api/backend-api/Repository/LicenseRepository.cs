using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class LicenseRepository : Repository<Licence>, ILicenseRepository
    {
        private readonly ApplicationDbContext _context;
        public LicenseRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Licence> UpdateAsync(Licence model)
        {
            try
            {
                _context.Licences.Update(model);
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

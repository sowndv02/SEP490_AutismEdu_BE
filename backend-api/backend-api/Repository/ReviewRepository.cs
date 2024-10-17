using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class ReviewRepository : Repository<Review>, IReviewRepository
    {
        private readonly ApplicationDbContext _context;
        public ReviewRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Review> UpdateAsync(Review model)
        {
            try
            {
                _context.Reviews.Update(model);
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

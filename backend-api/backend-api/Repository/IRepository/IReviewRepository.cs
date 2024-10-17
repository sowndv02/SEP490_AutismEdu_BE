using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IReviewRepository : IRepository<Review>
    {
        Task<Review> UpdateAsync(Review model);

    }
}

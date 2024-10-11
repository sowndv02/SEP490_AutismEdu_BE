using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IReviewRepository : IRepository<Review>
    {
        Task<Review> UpdateAsync(Review model);
        Task AddReviewAsync(Review review);
        Task<ApplicationUser> GetReviewerByIdAsync(string reviewerId);
        Task<Tutor> GetRevieweeByIdAsync(string revieweeId);
        Task<Review> GetByIdAsync(int id);

    }
}

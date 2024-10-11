using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Repository
{
    public class ReviewRepository : Repository<Review>, IReviewRepository
    {
        private readonly ApplicationDbContext _context;

        public ReviewRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddReviewAsync(Review review)
        {
            await _context.Reviews.AddAsync(review);
        }

        public async Task<ApplicationUser> GetReviewerByIdAsync(string reviewerId)
        {
            return await _context.ApplicationUsers.FindAsync(reviewerId);
        }

        public async Task<Tutor> GetRevieweeByIdAsync(string revieweeId)
        {
            return await _context.Tutors.FindAsync(revieweeId);
        }

        public async Task<Review> UpdateAsync(Review model)
        {
            _context.Reviews.Update(model); 
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<Review> GetByIdAsync(int id)
        {
            return await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
        }
    }
}
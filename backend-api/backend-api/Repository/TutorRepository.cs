using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace backend_api.Repository
{
    public class TutorRepository : Repository<Tutor>, ITutorRepository
    {
        private readonly ApplicationDbContext _context;
        public TutorRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(int TotalCount, List<Tutor> list)> GetAllTutorAsync(Expression<Func<Tutor, bool>>? filterName = null,
            Expression<Func<Tutor, bool>>? filterAddress = null, int? filterScore = null, Expression<Func<Tutor, bool>>? filterAge = null,
            string? includeProperties = null, int pageSize = 0, int pageNumber = 1, Expression<Func<Tutor, object>>? orderBy = null, bool isDesc = true)
        {
            IQueryable<Tutor> query = dbset.AsNoTracking();

            if (filterName != null)
                query = query.Where(filterName);
            if (filterAddress != null)
                query = query.Include(x => x.User).Where(filterAddress);
            if (filterAge != null)
                query = query.Where(filterAge);

            IQueryable<Tutor> storageQuery = query;
            if (filterScore != null)
            {
                query = GetTutorsWithReviews(query, (int)filterScore);

                if (!query.Any() && filterScore == 5)
                {
                    query = storageQuery.Include(x => x.Reviews).Where(x => x.Reviews.Any());
                }
            }
            int totalCount = await query.CountAsync();

            if (pageSize > 0)
                query = query.Skip(pageSize * (pageNumber - 1)).Take(pageSize);
            if (includeProperties != null)
            {
                var includeProps = includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var includeProp in includeProps)
                {
                    query = query.Include(includeProp);
                }
            }
            if (orderBy != null)
                if (isDesc)
                    query = query.OrderByDescending(orderBy);
                else
                    query = query.OrderBy(orderBy);

            var tutorList = await query.ToListAsync();
            return (totalCount, tutorList);
        }
        public IQueryable<Tutor> GetTutorsWithReviews(IQueryable<Tutor> query, int filterScore)
        {
            var reviews = _context.Reviews
                .AsNoTracking()
                .GroupBy(r => r.TutorId)
                .Select(g => new
                {
                    TutorId = g.Key,
                    AvgScore = g.Average(r => r.RateScore),
                    TotalReview = g.Key.Count()
                })
                .Where(x => x.AvgScore >= filterScore && x.AvgScore < filterScore + 1);

            var filteredQuery = query
                .Where(t => reviews.Any(r => r.TutorId == t.UserId));
            foreach (var item in filteredQuery)
            {
                var review = reviews.FirstOrDefault(x => x.TutorId == item.UserId);
                if (review != null)
                {
                    item.TotalReview = review.TotalReview;
                    item.ReviewScore = review.AvgScore == 0 ? 5 : review.AvgScore;
                }
            }
            return filteredQuery;
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

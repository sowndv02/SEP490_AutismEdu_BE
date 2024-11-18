using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AutismEduConnectSystem.Repository
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
            if (includeProperties != null)
            {
                var includeProps = includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var includeProp in includeProps)
                {
                    query = query.Include(includeProp);
                }
            }
            int defaultFilterScore = 5;
            if (filterScore != null)
            {
                query = await GetTutorsWithReviews(query, (int)filterScore);
                if (!query.Any() && filterScore == defaultFilterScore)
                {
                    query = storageQuery.Include(x => x.Reviews).Where(x => !x.Reviews.Any());
                }
                for (int i = 4; i >= 1; i--)
                {
                    if (!query.Any())
                    {
                        query = storageQuery;
                        query = await GetTutorsWithReviews(query, i);
                    }
                    else break;
                }
            }
            int totalCount = query.Count();

            if (pageSize > 0)
                query = query.Skip(pageSize * (pageNumber - 1)).Take(pageSize);
            
            if (orderBy != null)
                if (isDesc)
                    query = query.OrderByDescending(orderBy);
                else
                    query = query.OrderBy(orderBy);

            var tutorList =  query.ToList();
            return (totalCount, tutorList);
        }
        public async Task<IQueryable<Tutor>> GetTutorsWithReviews(IQueryable<Tutor> query, int filterScore)
        {
            // Get reviews from the database and perform the filtering
            var reviews = await _context.Reviews
                .AsNoTracking()
                .GroupBy(r => r.TutorId)
                .Select(g => new
                {
                    TutorId = g.Key,
                    AvgScore = g.Average(r => r.RateScore),
                    TotalReview = g.Count() // Use g.Count() to get the total review count
                })
                .Where(x => x.AvgScore >= filterScore && x.AvgScore < filterScore + 1)
                .ToListAsync();  // Execute and bring data into memory

            // Now filter the tutors based on the reviews in memory
            var tutorIdsWithReviews = reviews.Select(r => r.TutorId).ToList();

            // Filter tutors based on the tutor IDs in the reviews
            var filteredQuery = await query
                .Where(t => tutorIdsWithReviews.Contains(t.TutorId))  // Filter on TutorId
                .ToListAsync();  // Execute the query and bring tutors into memory

            // Populate the review data for the tutors
            foreach (var item in filteredQuery)
            {
                var review = reviews.FirstOrDefault(x => x.TutorId == item.TutorId);
                if (review != null)
                {
                    item.TotalReview = review.TotalReview;
                    item.ReviewScore = review.AvgScore == 0 ? 5 : review.AvgScore;
                }
            }

            // Return the modified list as IQueryable
            return filteredQuery.AsQueryable();
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

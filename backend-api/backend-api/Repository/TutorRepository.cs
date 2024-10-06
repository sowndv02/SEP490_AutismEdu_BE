using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq;
using System.Security.Claims;

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

        public async Task<(int TotalCount, List<Tutor> list)> GetAllTutorAsync(Expression<Func<Tutor, bool>>? filter = null, Expression<Func<Tutor, bool>>? filterAddress = null, int? reviewScore = 5, 
            int? ageFrom = 0, int? ageTo = 15, string? includeProperties = null,
            int pageSize = 10, int pageNumber = 1)
        {
            IQueryable<Tutor> query = _context.Tutors;
            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }

            int count = await query.CountAsync();
            if (filter != null)
                query = query.Where(filter);

            if (filterAddress != null)
                query = query.Where(filterAddress);
            if(ageFrom != null && ageTo != null)
                query.Where(x => x.StartAge >= ageFrom && x.EndAge <= ageTo);
            if (reviewScore.HasValue)
                query = query.Where(tutor => tutor.Reviews.Average(x => x.RateScore) >= reviewScore - 1 &&
                                              tutor.Reviews.Average(x => x.RateScore) < reviewScore);

            var tutors = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return (count, tutors);
        }
    }
}

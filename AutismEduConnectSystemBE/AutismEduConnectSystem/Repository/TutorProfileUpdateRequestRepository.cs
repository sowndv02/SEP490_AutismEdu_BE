using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AutismEduConnectSystem.Repository
{
    public class TutorProfileUpdateRequestRepository : Repository<TutorProfileUpdateRequest>, ITutorProfileUpdateRequestRepository
    {
        private readonly ApplicationDbContext _context;
        public TutorProfileUpdateRequestRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<TutorProfileUpdateRequest> UpdateAsync(TutorProfileUpdateRequest model)
        {
            try
            {
                _context.TutorProfileUpdateRequests.Update(model);
                await _context.SaveChangesAsync();
                return model;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<(int TotalCount, List<TutorProfileUpdateRequest> list)> GetAllTutorUpdateRequestAsync(Expression<Func<TutorProfileUpdateRequest, bool>>? filterName = null,
           Expression<Func<TutorProfileUpdateRequest, bool>>? filterOther = null,
           string? includeProperties = null, int pageSize = 0, int pageNumber = 1, Expression<Func<TutorProfileUpdateRequest, object>>? orderBy = null, bool isDesc = true)
        {
            IQueryable<TutorProfileUpdateRequest> query = dbset.AsNoTracking();
            query = query.Include(u => u.Tutor).ThenInclude(x => x.User);
            if (filterName != null)
            {
                query = query.Where(filterName);
            }
            if (filterOther != null)
            {
                query = query.Where(filterOther);
            }
            if (orderBy != null)
            {
                if (isDesc)
                    query = query.OrderByDescending(orderBy);
                else
                    query = query.OrderBy(orderBy);
            }
            int totalCount = await query.CountAsync();
            if (pageSize > 0)
            {
                query = query.Skip(pageSize * (pageNumber - 1)).Take(pageSize);
            }
            return (totalCount, await query.ToListAsync());
        }
    }
}

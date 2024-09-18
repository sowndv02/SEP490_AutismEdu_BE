using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace backend_api.Repository
{
    public class ClaimRepository : Repository<ApplicationClaim>, IClaimRepository
    {
        private readonly ApplicationDbContext _context;
        public ClaimRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ApplicationClaim> UpdateAsync(ApplicationClaim claim)
        {
            try
            {
                _context.ApplicationClaims.Update(claim);
                await _context.SaveChangesAsync();
                return claim;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public int GetTotalClaim(List<UserClaim> userClaims = null)
        {
            try
            {
                if (userClaims == null || userClaims.Count == 0)
                    return _context.ApplicationClaims.Count();
                var result = _context.ApplicationClaims.ToList();
                result = result.Where(claim => !userClaims.Any(uc => uc.ClaimType == claim.ClaimType && uc.ClaimValue == claim.ClaimValue)).ToList();
                return result.Count;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<(int TotalCount, List<ApplicationClaim> list)> GetAllAsync(Expression<Func<ApplicationClaim, bool>>? filter = null, string? includeProperties = null,
            int pageSize = 10, int pageNumber = 1, List<UserClaim> userClaims = null)
        {
            IQueryable<ApplicationClaim> query = _context.ApplicationClaims;
            if (filter != null)
                query = query.Where(filter);

            if (includeProperties != null)
            {
                var includeProps = includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var includeProp in includeProps)
                {
                    query = query.Include(includeProp);
                }
            }
            var result = await query.ToListAsync();
            if (userClaims != null && userClaims.Count > 0)
            {
                result = result.Where(claim => !userClaims.Any(uc => uc.ClaimType == claim.ClaimType && uc.ClaimValue == claim.ClaimValue)).ToList();
            }
            return (result.Count, result.Skip(pageSize * (pageNumber - 1)).Take(pageSize).ToList());
        }
    }
}

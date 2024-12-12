using AutismEduConnectSystem.Models;
using System.Linq.Expressions;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IClaimRepository : IRepository<ApplicationClaim>
    {

        Task<(int TotalCount, List<ApplicationClaim> list)> GetAllAsync(Expression<Func<ApplicationClaim, bool>>? filter = null,
            string? includeProperties = null, int pageSize = 10, int pageNumber = 1, List<UserClaim> userClaims = null);
        Task<ApplicationClaim> UpdateAsync(ApplicationClaim claim);
        int GetTotalClaim(List<UserClaim> userClaims = null);
    }
}

using backend_api.Data;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    }
}

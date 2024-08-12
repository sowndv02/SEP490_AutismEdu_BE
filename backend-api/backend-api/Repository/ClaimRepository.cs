using backend_api.Data;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Repository
{
    public class ClaimRepository : IClaimRepository
    {
        private readonly ApplicationDbContext _context;
        public ClaimRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApplicationClaim> CreateAsync(ApplicationClaim claim)
        {
            claim.CreatedDate = DateTime.UtcNow;
            claim.UpdatedDate = DateTime.UtcNow;
            var isEqual = _context.ApplicationClaims.FirstOrDefault
                (x => x.ClaimType.ToUpper().Equals(claim.ClaimType.ToUpper()) &&
               x.ClaimValue.ToUpper().Equals(claim.ClaimValue.ToUpper()));

            if (isEqual != null)
            {
                throw new Exception("Duplicated values");
            }
            _context.ApplicationClaims.Add(claim);
            await _context.SaveChangesAsync();

            return claim;
        }

        public async Task<List<ApplicationClaim>> GetAllAsync()
        {
            return await _context.ApplicationClaims.ToListAsync();
        }

        public async Task<ApplicationClaim> GetAsync(string claimId)
        {
            return await _context.ApplicationClaims.FindAsync(int.Parse(claimId));
        }

        public Task<ApplicationClaim> GetAsync(int claimId)
        {
            return _context.ApplicationClaims.FirstOrDefaultAsync(c => c.Id == claimId);
        }

        public Task<ApplicationClaim> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RemoveAsync(ApplicationClaim a)
        {
            var claim = await _context.ApplicationClaims.FindAsync(a.Id);
            if (claim == null)
            {
                return false;
            }

            _context.ApplicationClaims.Remove(claim);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<ApplicationClaim> UpdateAsync(ApplicationClaim claim)
        {
            var existingClaim = await _context.ApplicationClaims.FindAsync(claim.Id);
            if (existingClaim == null)
            {
                return null;
            }

            existingClaim.ClaimType = claim.ClaimType;
            existingClaim.ClaimValue = claim.ClaimValue;
            existingClaim.UpdatedDate = DateTime.UtcNow;

            _context.ApplicationClaims.Update(existingClaim);
            await _context.SaveChangesAsync();

            return existingClaim;
        }
    }
}

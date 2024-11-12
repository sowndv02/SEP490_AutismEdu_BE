using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AutismEduConnectSystem.Repository
{
    public class ChildInformationRepository : Repository<ChildInformation>, IChildInformationRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ChildInformationRepository(UserManager<ApplicationUser> userManager, ApplicationDbContext context) : base(context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<List<ChildInformation>> GetParentChildInformationAsync(string parentId)
        {
            try
            {
                if (parentId != null)
                {
                    var childInfoList = await _context.ChildInformations.Where(c => c.ParentId == parentId).ToListAsync();
                    return childInfoList;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<ChildInformation> UpdateAsync(ChildInformation model)
        {
            try
            {
                _context.ChildInformations.Update(model);
                await _context.SaveChangesAsync();
                return model;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}

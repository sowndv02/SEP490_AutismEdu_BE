using backend_api.Data;
using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Repository
{
    public class RoleRepository : IRoleRepository
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        public RoleRepository(RoleManager<IdentityRole> roleManager, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
            _roleManager = roleManager;
        }
        public async Task CreateAsync(IdentityRole objAdd)
        {
            try
            {
                if (_roleManager.RoleExistsAsync(objAdd.Name).GetAwaiter().GetResult())
                {
                    return;
                }
                IdentityRole role = new IdentityRole()
                {
                    Name = objAdd.Name,
                };
                await _roleManager.CreateAsync(role);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<IdentityRole>> GetAllAsync()
        {
            try
            {
                return await _roleManager.Roles.ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<IdentityRole> GetByNameAsync(string name)
        {
            try
            {
                if (!string.IsNullOrEmpty(name))
                {
                    var role = await _roleManager.FindByNameAsync(name);
                    return role;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<IdentityRole> GetByIdAsync(string roleId)
        {
            try
            {
                if (!string.IsNullOrEmpty(roleId))
                {
                    var role = await _roleManager.FindByIdAsync(roleId);
                    return role;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> RemoveAsync(IdentityRole role)
        {
            try
            {
                var objFromDb = _context.Roles.FirstOrDefault(u => u.Id == role.Id);
                if (objFromDb != null)
                {

                    var userRolesForThisRole = _context.UserRoles.Where(u => u.RoleId == role.Id).Count();
                    if (userRolesForThisRole > 0)
                    {
                        return false;
                    }

                    var result = await _roleManager.DeleteAsync(objFromDb);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}

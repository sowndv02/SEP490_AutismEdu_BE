﻿using Microsoft.AspNetCore.Identity;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IRoleRepository
    {
        Task CreateAsync(IdentityRole role);
        Task<List<IdentityRole>> GetAllAsync();
        Task<IdentityRole> GetByNameAsync(string name);
        Task<IdentityRole> GetByIdAsync(string roleId);
        Task<bool> RemoveAsync(IdentityRole role);
    }
}

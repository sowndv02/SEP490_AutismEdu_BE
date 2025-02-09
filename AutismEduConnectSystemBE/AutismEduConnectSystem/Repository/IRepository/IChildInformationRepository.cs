﻿using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IChildInformationRepository : IRepository<ChildInformation>
    {
        Task<ChildInformation> UpdateAsync(ChildInformation model);
        Task<List<ChildInformation>> GetParentChildInformationAsync(string parentId);
    }
}

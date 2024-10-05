using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IChildInformationRepository : IRepository<ChildInformation>
    {
        Task<ChildInformation> UpdateAsync(ChildInformation model);
        Task<List<ChildInformation>> GetParentChildInformationAsync(string parentId);
    }
}

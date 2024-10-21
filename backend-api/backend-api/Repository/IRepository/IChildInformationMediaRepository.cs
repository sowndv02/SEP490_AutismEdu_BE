using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IChildInformationMediaRepository : IRepository<ChildInformationMedia>
    {
        Task<ChildInformationMedia> UpdateAsync(ChildInformationMedia model);
    }
}

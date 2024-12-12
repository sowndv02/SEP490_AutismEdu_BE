using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface ISyllabusRepository : IRepository<Syllabus>
    {
        Task<Syllabus> UpdateAsync(Syllabus model);
    }
}

using AutismEduConnectSystem.Models;
using System.Linq.Expressions;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface IStudentProfileRepository : IRepository<StudentProfile>
    {
        Task<StudentProfile> UpdateAsync(StudentProfile model);
    }
}

using AutismEduConnectSystem.Models;

namespace AutismEduConnectSystem.Repository.IRepository
{
    public interface ISyllabusExerciseRepository : IRepository<SyllabusExercise>
    {
        Task<SyllabusExercise> UpdateAsync(SyllabusExercise model);
    }
}
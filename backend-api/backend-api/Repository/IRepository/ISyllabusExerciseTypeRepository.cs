using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface ISyllabusExerciseRepository : IRepository<SyllabusExercise>
    {
        Task<SyllabusExercise> UpdateAsync(SyllabusExercise model);
    }
}
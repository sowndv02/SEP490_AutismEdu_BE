using backend_api.Models;

namespace backend_api.Repository.IRepository
{
    public interface IExerciseTypeRepository : IRepository<ExerciseType>
    {
        Task<ExerciseType> UpdateAsync(ExerciseType model);
        Task<ExerciseType> GetExerciseNameByID(int exerciseTypeId);
    }
}

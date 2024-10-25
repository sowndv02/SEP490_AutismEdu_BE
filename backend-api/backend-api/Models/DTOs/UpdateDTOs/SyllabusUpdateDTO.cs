using backend_api.Models.DTOs.CreateDTOs;

namespace backend_api.Models.DTOs.UpdateDTOs
{
    public class SyllabusUpdateDTO
    {
        public int Id { get; set; }
        public int AgeFrom { get; set; }
        public int AgeEnd { get; set; }
        public List<SyllabusExerciseCreateDTO> SyllabusExercises { get; set; }
    }
}

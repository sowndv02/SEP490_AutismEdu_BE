using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static backend_api.SD;

namespace backend_api.Models.DTOs
{
    public class ExerciseTypeDTO
    {
        public int Id { get; set; }
        public string ExerciseTypeName { get; set; }
        public int AgeFrom { get; set; }
        public int AgeTo { get; set; }
        public Status RequestStatus { get; set; }
    }
}

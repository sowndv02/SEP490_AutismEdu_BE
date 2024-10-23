using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models.DTOs
{
    public class ExerciseTypeDTO
    {
        public int Id { get; set; }
        public string ExerciseTypeName { get; set; }
    }
}

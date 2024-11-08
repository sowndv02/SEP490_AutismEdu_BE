using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs.CreateDTOs
{
    public class TestResultCreateDTO
    {
        [Required(ErrorMessage = SD.ID_REQUIRED)]
        public int TestId { get; set; }
        [Required(ErrorMessage = SD.TOTAL_POINT_REQUIRED)]
        public double TotalPoint { get; set; }
        [Required(ErrorMessage = SD.TEST_RESULT_REQUIRED)]
        public List<TestResultDetailCreateDTO> TestResults { get; set; }
    }
}

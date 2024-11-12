using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class TutorProfileUpdateRequestCreateDTO
    {
        [Required(ErrorMessage = SD.PRICE_REQUIRED)]
        public decimal PriceFrom { get; set; }
        public decimal PriceEnd { get; set; }
        public float SessionHours { get; set; }
        [Required(ErrorMessage = SD.ADDRESS_REQUIRED)]
        public string Address { get; set; }
        [Required(ErrorMessage = SD.DESCRIPTION_REQUIRED)]
        public string AboutMe { get; set; }
        [Required(ErrorMessage = SD.PHONE_NUMBER_REQUIRED)]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage = SD.AGE_REQUIRED)]
        public int StartAge { get; set; }
        [Required(ErrorMessage = SD.AGE_REQUIRED)]
        public int EndAge { get; set; }
    }
}

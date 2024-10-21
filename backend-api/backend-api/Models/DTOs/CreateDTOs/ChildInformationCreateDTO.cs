namespace backend_api.Models.DTOs.CreateDTOs
{
    public class ChildInformationCreateDTO
    {
        public string? Name { get; set; }
        public bool? isMale { get; set; }
        public DateTime? BirthDate { get; set; }
        public List<IFormFile> Medias { get; set; }
    }
}

namespace backend_api.Models.DTOs
{
    public class ConfirmEmailDTO
    {
        public string Code { get; set; }
        public string Security { get; set; }
        public string UserId { get; set; }
    }
}

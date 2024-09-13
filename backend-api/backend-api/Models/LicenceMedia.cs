using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class LicenceMedia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int LicenceId { get; set; }
        public string? LicencePath { get; set; }
        [ForeignKey(nameof(LicenceId))]
        public Licence Licence { get; set; }
    }
}

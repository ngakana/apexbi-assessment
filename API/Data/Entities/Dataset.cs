using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Data.Entities;

public class Dataset(string fileHash) : IAuditableEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public DateTime UploadDate { get; set; }
    [Required]
    public string FileHash { get; init; } = fileHash;

    public virtual ICollection<SimCard> SIMCards { get; set; } = [];
}

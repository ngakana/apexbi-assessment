using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Data.Entities;

public class SimCard
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int? OrganizationId { get; set; }
    public long PhoneNumberInt { get; set; }
    public long PhoneNumberLocal { get; set; }
    public DateTime? AddedDate { get; set; }
    public bool? Locked { get; set; }
    public DateTime? LockDate { get; set; }
    public bool CanLock { get; set; }

    [MaxLength(32)]
    public required string PhoneNumberString { get; set; }

    [MaxLength(32)]
    public string? IMEI { get; set; }

    [MaxLength(64)]
    public string? SIMNumber { get; set; }

    [MaxLength(32)]
    public string? Status { get; set; }

    [ForeignKey("Dataset")]
    public int DatasetId { get; set; }
}

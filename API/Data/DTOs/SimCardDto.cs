using System.Text.Json.Serialization;

namespace API.Data.DTOs;

public class SimDataWrapper
{
    [JsonPropertyName("SIMs")]
    public List<SimCardDto> SimCards { get; set; } = [];
}

public record struct SimCardDto
{
    [JsonPropertyName("TenantId")]
    public int? OrganizationId { get; set; }

    [JsonPropertyName("MSISDN")]
    public required string PhoneNumberString { get; set; }

    [JsonPropertyName("MSISDN_Int")]
    public long PhoneNumberInt { get; set; }

    [JsonPropertyName("MSISDN_Num")]
    public long PhoneNumberLocal { get; set; }

    public string? IMEI { get; set; }
    public string? SIMNumber { get; set; }
    public DateTime? AddedDate { get; set; }

    [JsonPropertyName("SIMStatus")]
    public string? Status { get; set; }

    public bool? Locked { get; set; }
    public DateTime? LockDate { get; set; }
    public bool CanLock {  get; set; }
}

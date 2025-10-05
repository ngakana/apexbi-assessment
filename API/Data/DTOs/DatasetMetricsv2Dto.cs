namespace API.Data.DTOs;

public record DatasetMetricsv2Dto
{
    public int Id { get; set; }
    public int RowCount { get; set; }
    public List<string> ModalAreaCodes { get; set; } = [];
    public int PhoneNumberWith123Count { get; set; }
}

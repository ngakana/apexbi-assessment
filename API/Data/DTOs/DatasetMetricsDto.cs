namespace API.Data.DTOs;

public record DatasetMetricsDto
{
    public int Id { get; set; }
    public int RowCount { get; set; }
    public List<string> ModalAreaCodes { get; set; } = [];
    public int PhoneNumberWith123Count { get; set; }
}

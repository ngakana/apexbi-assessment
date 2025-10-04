namespace API.Data.DTOs;

public record DatasetMetricsDto
{
    public int Id { get; set; }
    public int Count { get; set; }
    public List<string> ModalAreaCode { get; set; } = [];
    public int Contain123Count { get; set; }
}

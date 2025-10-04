using Microsoft.AspNetCore.Mvc;

namespace API.Data.DTOs;

public class DatasetUploadDto
{
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = default!;
}

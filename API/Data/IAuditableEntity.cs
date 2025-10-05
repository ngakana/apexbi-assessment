namespace API.Data;

public interface IAuditableEntity
{
    DateTime UploadDate { get; set; }
}

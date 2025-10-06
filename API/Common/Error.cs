namespace API.Common;

public record Error(ErrorType Type, string Description);

public enum ErrorType
{
    InvalidFileFormat,
    InvalidSchema,
    FileIsEmpty,
    DuplicateInsert,
    InvalidOperaton,
    Exception
}

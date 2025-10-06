using API.Data.DTOs;
using API.Repositories.Abstract;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;

namespace API.Common;

public static class IFormFileExtensions
{
    public static Result<IFormFile> ValidateFileType(this IFormFile file, string fileExt)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(fileExt);

            if (!file.FileName.EndsWith(fileExt)) return Result<IFormFile>.Failure(new Error(
                ErrorType.InvalidFileFormat,
                $"File type ({file.FileName.Split(".").Last()}) is not supported. Please upload a JSON file."));

            return Result<IFormFile>.Success(file);
        }
        catch (ArgumentNullException ex)
        {
            return Result<IFormFile>.Failure(new Error(
                ErrorType.InvalidOperaton,
                $"Cannot perform {nameof(ValidateFileType)}() in {nameof(IFormFileExtensions)} on null object. Missing parameter {ex.ParamName}"));
        }
    }

    public static Result<IFormFile> VerifyIsNotEmpty(this IFormFile file)
    {
        if (file is null) return Result<IFormFile>.Failure(new Error(
            ErrorType.InvalidOperaton,
            $"Cannot perform {nameof(VerifyIsNotEmpty)}() in {nameof(IFormFileExtensions)} on null object."));
        if (file.Length == 0) return Result<IFormFile>.Failure(new Error(
            ErrorType.FileIsEmpty,
            "File is empty! No data to load."));
        return Result<IFormFile>.Success(file);
    }

    public static string? ComputeContentHash(this IFormFile file)
    {
        try
        {
            using var sha = SHA256.Create();
            using var stream = file.OpenReadStream();
            var hashBytes = sha.ComputeHash(stream);
            var hashString = Convert.ToHexString(hashBytes);
            return hashString;
        }
        catch
        {
            return null;
        }
    }

    public static Result<IFormFile> VerifyIsNotDuplicate(this IFormFile file, IUnitOfWork uow)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(uow);
            var fileHash = file.ComputeContentHash();
            if (fileHash == null) throw new Exception();
            var exists = uow.DatasetRepository
                .GetAsQueryable()
                .AsNoTracking()
                .Where(d => d.FileHash == fileHash)
                .Any();
            if (exists) return Result<IFormFile>.Failure(new Error(
                ErrorType.DuplicateInsert,
                "Duplicate dataset! A dataset with identical data has already been uploaded."));
            return Result<IFormFile>.Success(file);
        }
        catch (ArgumentNullException ex)
        {
            return Result<IFormFile>.Failure(new Error(
                ErrorType.InvalidOperaton,
                $"Cannot perform {nameof(VerifyIsNotDuplicate)}() in {nameof(IFormFileExtensions)} on null object or with null argument. Missing parameter {ex.ParamName}"));
        }
        catch
        {
            return Result<IFormFile>.Failure(new Error(ErrorType.Exception, "Failed! Could not read file."));
        }
    }

    public static async Task<Result<T>> DeserializeAsync<T>(this IFormFile file)
    {
        try
        {
            if (file is null) return Result<T>.Failure(new Error(
                ErrorType.InvalidOperaton,
                $"Cannot perform {nameof(DeserializeAsync)}() in {nameof(IFormFileExtensions)} on null object."));
            
            using var reader = new StreamReader(file.OpenReadStream());
            var jsonString = await reader.ReadToEndAsync();
            var data = JsonSerializer.Deserialize<T>(jsonString);
            if (data == null) return Result<T>.Failure(new Error(
                ErrorType.FileIsEmpty,
                "File is empty! no data to load."));
            return Result<T>.Success(data);
        }
        catch (JsonException)
        {
            return Result<T>.Failure(new Error(
                ErrorType.InvalidSchema,
                "Invalid JSON schema. Could not deserialize JSON data."));
        }
        catch
        {
            return Result<T>.Failure(new Error(
                ErrorType.Exception,
                "Failed! Could read file."));
        }
    }
}

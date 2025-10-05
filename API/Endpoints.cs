using API.Data.DTOs;
using API.Data.Entities;
using API.Repositories.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;

namespace API;

public static class Endpoints
{
    public static void RegisterMyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/SIM", async (
            [FromForm] DatasetUploadDto payload,
            IUnitOfWork uow,
            CancellationToken ct) =>
        {
            if (payload is null)
                return Results.BadRequest("Missing dataset file. Please upload JSON file.");
            var file = payload.File;
            if (file is null ||
                file.Length == 0 ||
               !file.FileName.EndsWith("json", StringComparison.InvariantCultureIgnoreCase))
                return TypedResults.Problem(
                    statusCode: 405,
                    title: "Unsupported File Format",
                    detail: "Only JSON files are supported.");

            SimDataWrapper? simDataWrapper;
            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var jsonString = await reader.ReadToEndAsync();
                simDataWrapper = JsonSerializer.Deserialize<SimDataWrapper>(jsonString);
            }
            catch
            {
                return TypedResults.Problem(
                    statusCode: 405,
                    title: "Invalid Input",
                    detail: "JSON file schema is invalid. File schema does not match expected schema.");
            }

            if (simDataWrapper is null || !simDataWrapper.SimCards.Any())
                return Results.BadRequest("Empty data set.");

            using var sha = SHA256.Create();
            using var stream = file.OpenReadStream();
            var hashBytes = sha.ComputeHash(stream);
            var hashString = Convert.ToHexString(hashBytes);

            var exists = await uow.DatasetRepository
            .GetAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.FileHash == hashString, ct);
            if (exists != null)
                return Results.Conflict("Dataset with the same data already uploaded.");
            var dataset = new Dataset(hashString);
            await uow.DatasetRepository.AddAsync(dataset, ct);
            await uow.SaveChangesAsync();

            foreach (var item in simDataWrapper.SimCards)
            {
                var simcard = new SimCard
                {
                    DatasetId = dataset.Id,
                    OrganizationId = item.OrganizationId,
                    PhoneNumberString = item.PhoneNumberString,
                    PhoneNumberInt = item.PhoneNumberInt,
                    PhoneNumberLocal = item.PhoneNumberLocal,
                    IMEI = item.IMEI,
                    SIMNumber = item.SIMNumber,
                    AddedDate = item.AddedDate.HasValue ?
                                item.AddedDate.Value.ToUniversalTime() :
                                null,
                    Status = item.Status,
                    Locked = item.Locked,
                    LockDate = item.LockDate,
                    CanLock = item.CanLock
                };
                await uow.SimCardRepository.AddAsync(simcard, ct);
            }

            await uow.SaveChangesAsync();
            return Results.Ok();
        })
        .Accepts<DatasetUploadDto>("multipart/form-data")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status405MethodNotAllowed)
        .DisableAntiforgery();

        app.MapGet("/api/SIM", async (IUnitOfWork uow, CancellationToken ct) =>
        {
            List<DatasetDto> datasets = [];
            try
            {
                datasets = await uow.DatasetRepository
                    .GetAsQueryable()
                    .AsNoTracking()
                    .Select(x => new DatasetDto(x.Id, x.UploadDate))
                    .ToListAsync(ct);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem(
                    statusCode: 405,
                    title: "Exception Occured",
                    detail: ex.Message);
            }
            return Results.Ok(datasets);
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status405MethodNotAllowed);

        app.MapGet("/api/SIM/{id}", async (int id, IUnitOfWork uow) =>  
        {
            try
            {
                var dataset = await uow.DatasetRepository.GetByIdAsync(id);
                if (dataset is null)
                    return TypedResults.BadRequest($"Dataset with id {id} does not exist.");

                var query = uow.SimCardRepository
                    .GetAsQueryable()
                    .Where(s => s.DatasetId == id);
                var count = await query.CountAsync();
                var with123Count = await query.CountAsync(s => s.PhoneNumberString.Contains("123"));
                var modalAreaCodesQuery = query
                    .Select(s => s.PhoneNumberString.Substring(0, 3))
                    .GroupBy(code => code)
                    .OrderByDescending(g => g.Count());
                var maxGroupCount = await modalAreaCodesQuery
                    .Select(g => g.Count())
                    .FirstOrDefaultAsync();
                var modalAreaCodes = await modalAreaCodesQuery
                    .Where(g => g.Count() == maxGroupCount)
                    .Select(x => x.Key)
                    .ToListAsync();
                
                if (modalAreaCodes.Count > 1)
                {
                    return Results.Ok(new DatasetMetricsv2Dto
                    {
                        Id = id,
                        RowCount = count,
                        PhoneNumberWith123Count = with123Count,
                        ModalAreaCodes = modalAreaCodes
                    });
                }

                var metrics = new DatasetMetricsDto
                {
                    Id = id,
                    RowCount = count,
                    PhoneNumberWith123Count = with123Count,
                    ModalAreaCode = modalAreaCodes.First()
                };
                return Results.Ok(metrics);
            }
            catch (Exception ex)
            {
                return TypedResults.Problem(
                    statusCode: 405,
                    title: "Something went wrong!",
                    detail: ex.Message);
            }
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status405MethodNotAllowed);
    }
}

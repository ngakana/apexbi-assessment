using API.Data;
using API.Data.DTOs;
using API.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace API;

public static class Endpoints
{
    public static void RegisterMyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/SIM", async ([FromForm] DatasetUploadDto payload, AppDbContext db) =>
        {
            var file = payload.File;
            if (file is null ||
                file.Length == 0 ||
               !file.FileName.EndsWith("json", StringComparison.InvariantCultureIgnoreCase))
                return Results.StatusCode(405);

            SimDataWrapper? simDataWrapper;
            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var jsonString = await reader.ReadToEndAsync();
                simDataWrapper = JsonSerializer.Deserialize<SimDataWrapper>(jsonString);
            }
            catch
            {
                return Results.StatusCode(405);
            }

            if (simDataWrapper is null || !simDataWrapper.SimCards.Any())
                return Results.BadRequest("Empty data set.");

            var dataset = new Dataset
            {
                UploadDate = DateTime.UtcNow
            };
            db.Datasets.Add(dataset);
            var affected = await db.SaveChangesAsync();
            Console.WriteLine($"Rows written: {affected}");

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
                db.SimCards.Add(simcard);
            }
            var affected1 = await db.SaveChangesAsync();
            Console.WriteLine($"Rows written: {affected1}");

            return Results.Ok();
        })
        .Accepts<DatasetUploadDto>("multipart/form-data")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status405MethodNotAllowed)
        .DisableAntiforgery();

        app.MapGet("/api/SIM", async (AppDbContext db) =>
        {
            List<DatasetDto> datasets = [];
            try
            {
                datasets = await db.Datasets
                    .AsNoTracking()
                    .Select(x => new DatasetDto(x.Id, x.UploadDate))
                    .ToListAsync();
            }
            catch
            {
                return Results.StatusCode(405);
            }
            return Results.Ok(datasets);
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status405MethodNotAllowed);
    }
}

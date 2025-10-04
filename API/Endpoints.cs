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

        app.MapGet("/api/SIM/{id}", async (int id, AppDbContext db) =>
        {
            try
            {
                var dataset = await db.Datasets
                    .AsNoTracking()
                    .FirstAsync(d => d.Id == id);
                if (dataset is null) return Results.NotFound();

                var query = db.SimCards.Where(s => s.DatasetId == id);
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
                var metrics = new DatasetMetricsDto
                {
                    Id = id,
                    RowCount = count,
                    PhoneNumberWith123Count = with123Count,
                    ModalAreaCodes = modalAreaCodes
                };
                return Results.Ok(metrics);
            }
            catch
            {
                return Results.StatusCode(405);
            }
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status405MethodNotAllowed);
    }
}

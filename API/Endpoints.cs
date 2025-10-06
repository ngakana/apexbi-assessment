using API.Common;
using API.Data.DTOs;
using API.Data.Entities;
using API.Repositories.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API;

public static class Endpoints
{
    public static void RegisterMyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/SIM", async ([FromForm] DatasetUploadDto payload, IUnitOfWork uow, CancellationToken ct) =>
        {
            if (payload is null) return Results.BadRequest("Missing dataset file. Please upload JSON file.");
            var uploadedFile = payload.File;
            var result = await uploadedFile.ValidateFileType(".json")
                .Bind(file => file.VerifyIsNotEmpty())
                .Bind(file => file.VerifyIsNotDuplicate(uow))
                .BindAsync(async file => await file.DeserializeAsync<SimDataWrapper>())
                .BindAsync(data => Task.FromResult(data.VerifyIsNotEmpty()))
                .TapAsync(async simdata =>
                {
                    var hashString = uploadedFile.ComputeContentHash();
                    var dataset = new Dataset(hashString!);
                    await uow.DatasetRepository.AddAsync(dataset, ct);
                    await uow.SaveChangesAsync();

                    foreach (var item in simdata.SimCards)
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
                })
                .MatchAsync(onSuccess: value => Task.FromResult<IResult>(Results.Ok("File upload successful")), onFailure: error => Task.FromResult<IResult>(error.Type switch
                {
                    ErrorType.InvalidFileFormat => TypedResults.Problem(statusCode: 405, title: nameof(ErrorType.InvalidFileFormat), detail: error.Description),
                    ErrorType.FileIsEmpty => TypedResults.Problem(statusCode: 405, title: nameof(ErrorType.FileIsEmpty), detail: error.Description),
                    ErrorType.DuplicateInsert => TypedResults.Problem(statusCode: 409, title: nameof(ErrorType.DuplicateInsert), detail: error.Description),
                    ErrorType.InvalidSchema => TypedResults.Problem(statusCode: 405, title: nameof(ErrorType.InvalidSchema), detail: error.Description),
                    ErrorType.InvalidOperaton => TypedResults.Problem(statusCode: 500, title: nameof(ErrorType.InvalidOperaton), detail: error.Description),
                    ErrorType.Exception => TypedResults.Problem(statusCode: 500, title: nameof(ErrorType.Exception), detail: error.Description),
                    _ => TypedResults.Problem(statusCode: 500, title: "InternalServerError", detail: error.Description)
                }));
            return result;
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

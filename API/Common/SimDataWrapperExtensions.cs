using API.Data.DTOs;

namespace API.Common;

public static class SimDataWrapperExtensions
{
    public static Result<SimDataWrapper> VerifyIsNotEmpty(this SimDataWrapper data)
    {
        if (data is null) return Result<SimDataWrapper>.Failure(new Error(
            ErrorType.Exception,
            $"Passed null argument to {nameof(VerifyIsNotEmpty)} method in {nameof(SimDataWrapperExtensions)}"));

        if (data.SimCards.Count == 0) Result<SimDataWrapper>.Failure(new Error(
            ErrorType.FileIsEmpty,
            "Dataset is empty."));

        return Result<SimDataWrapper>.Success(data);
    }
}

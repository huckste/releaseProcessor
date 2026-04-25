using ErrorOr;
using ReleaseProcessor.UI;

namespace ReleaseProcessor.Errors;

public static class ErrorOrExtensions
{
    public static ErrorOr<T> LogOnError<T>(this ErrorOr<T> result)
    {
        if (result.IsError)
        {
            DisplayInfo.Error(result.Errors);
            return result.Errors;
        }

        return result.Value;
    }

    public static ErrorOr<T> CollectTo<T>(this ErrorOr<T> result, List<Error> sink)
    {
        if (result.IsError)
            sink.AddRange(result.Errors);

        return result;
    }

    public static ErrorOr<Success> Discard<T>(this ErrorOr<T> r) =>
        r.IsError ? r.Errors : Result.Success;
}

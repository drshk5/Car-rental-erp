namespace CarRentalERP.Shared.Contracts;

public sealed record ApiResponse<T>(bool Success, string Message, T? Data, IReadOnlyCollection<string> Errors)
{
    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new(true, message, data, Array.Empty<string>());

    public static ApiResponse<T> Fail(string message, params string[] errors) =>
        new(false, message, default, errors);
}

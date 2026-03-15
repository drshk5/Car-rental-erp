namespace CarRentalERP.Shared.Contracts;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Data,
    int Total,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
}

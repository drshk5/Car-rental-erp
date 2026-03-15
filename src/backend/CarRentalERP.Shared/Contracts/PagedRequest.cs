namespace CarRentalERP.Shared.Contracts;

public abstract record PagedRequest(int Page = 1, int PageSize = 20);

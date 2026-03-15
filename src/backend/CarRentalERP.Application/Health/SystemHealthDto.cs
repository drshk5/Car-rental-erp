namespace CarRentalERP.Application.Health;

public sealed record SystemHealthDto(string Service, string Status, DateTime TimestampUtc, string Environment);

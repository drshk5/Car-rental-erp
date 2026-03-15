namespace CarRentalERP.Application.Health;

public sealed class SystemHealthService
{
    public SystemHealthDto GetSummary(string environmentName) =>
        new("CarRentalERP.Api", "Healthy", DateTime.UtcNow, environmentName);
}

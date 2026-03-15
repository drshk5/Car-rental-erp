using CarRentalERP.Domain.Common;
using CarRentalERP.Domain.Enums;

namespace CarRentalERP.Domain.Entities;

public sealed class MaintenanceRecord : BaseEntity
{
    public Guid VehicleId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;
    public string Notes { get; set; } = string.Empty;
}

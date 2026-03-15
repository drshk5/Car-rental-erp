using CarRentalERP.Domain.Common;
using CarRentalERP.Domain.Enums;

namespace CarRentalERP.Domain.Entities;

public sealed class Vehicle : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid OwnerId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Vin { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal DailyRate { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal KmRate { get; set; }
    public VehicleStatus Status { get; set; } = VehicleStatus.Available;
}

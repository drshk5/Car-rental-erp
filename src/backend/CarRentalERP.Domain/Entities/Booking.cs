using CarRentalERP.Domain.Common;
using CarRentalERP.Domain.Enums;

namespace CarRentalERP.Domain.Entities;

public sealed class Booking : BaseEntity
{
    public string BookingNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid PickupBranchId { get; set; }
    public Guid ReturnBranchId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public PricingPlan PricingPlan { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal QuotedTotal { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Draft;
}

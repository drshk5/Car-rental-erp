using CarRentalERP.Domain.Common;
using CarRentalERP.Domain.Constants;

namespace CarRentalERP.Domain.Entities;

public sealed class RentalTransaction : BaseEntity
{
    public Guid BookingId { get; set; }
    public DateTime? CheckOutAtUtc { get; set; }
    public DateTime? CheckInAtUtc { get; set; }
    public int OdometerOut { get; set; }
    public int? OdometerIn { get; set; }
    public string FuelOut { get; set; } = string.Empty;
    public string? FuelIn { get; set; }
    public decimal ExtraCharges { get; set; }
    public string DamageNotes { get; set; } = string.Empty;
    public decimal FinalAmount { get; set; }
    public string Status { get; set; } = RentalStatuses.Active;
}

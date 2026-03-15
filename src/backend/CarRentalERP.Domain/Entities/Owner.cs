using CarRentalERP.Domain.Common;

namespace CarRentalERP.Domain.Entities;

public sealed class Owner : BaseEntity
{
    public string DisplayName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal RevenueSharePercentage { get; set; }
    public bool IsActive { get; set; } = true;
}

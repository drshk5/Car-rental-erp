using CarRentalERP.Domain.Common;

namespace CarRentalERP.Domain.Entities;

public sealed class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

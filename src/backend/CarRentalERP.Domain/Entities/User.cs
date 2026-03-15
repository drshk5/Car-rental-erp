using CarRentalERP.Domain.Common;

namespace CarRentalERP.Domain.Entities;

public sealed class User : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid BranchId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

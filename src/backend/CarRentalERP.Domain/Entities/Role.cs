using CarRentalERP.Domain.Common;
using CarRentalERP.Domain.Enums;

namespace CarRentalERP.Domain.Entities;

public sealed class Role : BaseEntity
{
    public UserRoleType RoleType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PermissionsJson { get; set; } = "[]";
}

using CarRentalERP.Domain.Common;
using CarRentalERP.Domain.Enums;

namespace CarRentalERP.Domain.Entities;

public sealed class Customer : BaseEntity
{
    public string CustomerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AlternatePhone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public DateOnly? LicenseExpiry { get; set; }
    public string IdentityDocumentType { get; set; } = string.Empty;
    public string IdentityDocumentNumber { get; set; } = string.Empty;
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string RiskNotes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
}

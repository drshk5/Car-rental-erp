using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Customers;

public sealed record CustomerDto(
    Guid Id,
    string CustomerCode,
    string FullName,
    string Phone,
    string AlternatePhone,
    string Email,
    string Address,
    string City,
    string State,
    string PostalCode,
    DateOnly? DateOfBirth,
    string Nationality,
    string LicenseNumber,
    DateOnly? LicenseExpiry,
    string IdentityDocumentType,
    string IdentityDocumentNumber,
    string EmergencyContactName,
    string EmergencyContactPhone,
    string Notes,
    string RiskNotes,
    bool IsActive,
    VerificationStatus VerificationStatus,
    int TotalBookings,
    int CompletedRentals,
    decimal LifetimeValue,
    decimal OutstandingBalance,
    DateTime? LastBookingAtUtc,
    bool HasActiveRental,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CustomerBookingSnapshotDto(
    Guid BookingId,
    string BookingNumber,
    string VehicleLabel,
    string PickupBranchName,
    string ReturnBranchName,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    BookingStatus Status,
    decimal QuotedTotal,
    decimal TotalPaid,
    decimal OutstandingBalance);

public sealed record CustomerRentalSnapshotDto(
    Guid RentalId,
    string BookingNumber,
    string VehicleLabel,
    DateTime? CheckOutAtUtc,
    DateTime? CheckInAtUtc,
    int OdometerOut,
    int? OdometerIn,
    string FuelOut,
    string? FuelIn,
    decimal FinalAmount,
    string Status,
    string DamageNotes);

public sealed record CustomerDetailDto(
    CustomerDto Profile,
    IReadOnlyCollection<CustomerBookingSnapshotDto> RecentBookings,
    CustomerRentalSnapshotDto? ActiveRental);

public sealed record CustomerListRequest(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    VerificationStatus? VerificationStatus = null,
    bool? IsActive = null,
    bool? HasActiveRental = null,
    bool? HasOutstandingBalance = null) : PagedRequest(Page, PageSize);

public sealed record CreateCustomerRequest(
    string FullName,
    string Phone,
    string AlternatePhone,
    string Email,
    string Address,
    string City,
    string State,
    string PostalCode,
    DateOnly? DateOfBirth,
    string Nationality,
    string LicenseNumber,
    DateOnly? LicenseExpiry,
    string IdentityDocumentType,
    string IdentityDocumentNumber,
    string EmergencyContactName,
    string EmergencyContactPhone,
    string Notes,
    string RiskNotes);

public sealed record UpdateCustomerRequest(
    string FullName,
    string Phone,
    string AlternatePhone,
    string Email,
    string Address,
    string City,
    string State,
    string PostalCode,
    DateOnly? DateOfBirth,
    string Nationality,
    string LicenseNumber,
    DateOnly? LicenseExpiry,
    string IdentityDocumentType,
    string IdentityDocumentNumber,
    string EmergencyContactName,
    string EmergencyContactPhone,
    string Notes,
    string RiskNotes);

public sealed record SetCustomerVerificationRequest(VerificationStatus VerificationStatus);

public sealed record SetCustomerStatusRequest(bool IsActive);

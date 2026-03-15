using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Bookings;

public sealed record BookingDto(
    Guid Id,
    string BookingNumber,
    Guid CustomerId,
    string CustomerName,
    Guid VehicleId,
    string VehicleLabel,
    Guid PickupBranchId,
    string PickupBranchName,
    Guid ReturnBranchId,
    string ReturnBranchName,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    PricingPlan PricingPlan,
    decimal BaseAmount,
    decimal DiscountAmount,
    decimal DepositAmount,
    decimal QuotedTotal,
    decimal TotalPaid,
    decimal OutstandingBalance,
    bool HasActiveRental,
    bool IsOverdue,
    BookingStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record BookingDetailDto(
    BookingDto Booking,
    BookingFinancialsDto Financials,
    BookingOperationsDto Operations);

public sealed record BookingFinancialsDto(
    decimal BaseAmount,
    decimal DiscountAmount,
    decimal DepositAmount,
    decimal QuotedTotal,
    decimal TotalPaid,
    decimal OutstandingBalance);

public sealed record BookingOperationsDto(
    bool HasActiveRental,
    bool IsOverdue,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string BookingWindowLabel);

public sealed record BookingListRequest(
    int Page = 1,
    int PageSize = 20,
    BookingStatus? Status = null,
    Guid? BranchId = null,
    Guid? CustomerId = null,
    Guid? VehicleId = null,
    DateTime? StartFromUtc = null,
    DateTime? StartToUtc = null,
    bool? HasOutstandingBalance = null,
    bool? HasActiveRental = null,
    string? Search = null) : PagedRequest(Page, PageSize);

public sealed record CreateBookingRequest(
    Guid CustomerId,
    Guid VehicleId,
    Guid PickupBranchId,
    Guid ReturnBranchId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    PricingPlan PricingPlan,
    decimal DiscountAmount,
    decimal DepositAmount);

public sealed record BookingQuoteRequest(
    Guid VehicleId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    PricingPlan PricingPlan,
    decimal DiscountAmount,
    decimal DepositAmount);

public sealed record BookingQuoteDto(
    Guid VehicleId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    PricingPlan PricingPlan,
    decimal BaseAmount,
    decimal DiscountAmount,
    decimal DepositAmount,
    decimal QuotedTotal);

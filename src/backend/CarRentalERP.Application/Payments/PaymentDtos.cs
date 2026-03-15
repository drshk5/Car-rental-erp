using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Payments;

public sealed record PaymentDto(
    Guid Id,
    Guid BookingId,
    string BookingNumber,
    Guid CustomerId,
    string CustomerName,
    Guid VehicleId,
    string VehicleLabel,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string ReferenceNumber,
    PaymentStatus PaymentStatus,
    DateTime PaidAtUtc,
    string Notes,
    decimal BookingTotal,
    decimal NetPaidAmount,
    decimal OutstandingBalance,
    bool IsRefunded,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record PaymentDetailDto(
    PaymentDto Payment,
    PaymentBookingSnapshotDto Booking,
    PaymentSummaryDto Summary);

public sealed record PaymentBookingSnapshotDto(
    Guid BookingId,
    string BookingNumber,
    Guid CustomerId,
    string CustomerName,
    Guid VehicleId,
    string VehicleLabel,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    BookingStatus BookingStatus);

public sealed record PaymentListRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? BookingId = null,
    Guid? CustomerId = null,
    PaymentStatus? PaymentStatus = null,
    PaymentMethod? PaymentMethod = null,
    DateTime? PaidFromUtc = null,
    DateTime? PaidToUtc = null,
    string? Search = null) : PagedRequest(Page, PageSize);

public sealed record RecordPaymentRequest(
    Guid BookingId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string ReferenceNumber,
    DateTime? PaidAtUtc,
    string Notes);

public sealed record PaymentSummaryDto(
    Guid BookingId,
    string BookingNumber,
    decimal BookingTotal,
    decimal TotalPaid,
    decimal TotalRefunded,
    decimal NetPaid,
    decimal OutstandingBalance,
    string BalanceStatus);

using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Entities;
using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Payments;

public sealed class PaymentService
{
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<RentalTransaction> _rentalRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Vehicle> _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(
        IRepository<Payment> paymentRepository,
        IRepository<Booking> bookingRepository,
        IRepository<RentalTransaction> rentalRepository,
        IRepository<Customer> customerRepository,
        IRepository<Vehicle> vehicleRepository,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _rentalRepository = rentalRepository;
        _customerRepository = customerRepository;
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<PaymentDto>> GetPagedAsync(PaymentListRequest request, CancellationToken cancellationToken = default)
    {
        ValidateListRequest(request);

        var data = await LoadContextAsync(cancellationToken);
        var filtered = data.Payments
            .Where(x => request.BookingId is null || x.BookingId == request.BookingId)
            .Where(x => request.CustomerId is null || data.BookingsById[x.BookingId].CustomerId == request.CustomerId)
            .Where(x => request.PaymentStatus is null || x.PaymentStatus == request.PaymentStatus)
            .Where(x => request.PaymentMethod is null || x.PaymentMethod == request.PaymentMethod)
            .Where(x => request.PaidFromUtc is null || x.PaidAtUtc >= request.PaidFromUtc.Value)
            .Where(x => request.PaidToUtc is null || x.PaidAtUtc <= request.PaidToUtc.Value)
            .Where(x => MatchesSearch(x, request.Search, data))
            .OrderByDescending(x => x.PaidAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToArray();

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var result = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => Map(x, data))
            .ToArray();

        return new PagedResult<PaymentDto>(result, filtered.Length, page, pageSize);
    }

    public async Task<IReadOnlyCollection<PaymentDto>> GetByBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        EnsureBookingExists(bookingId, data.BookingsById);

        return data.Payments
            .Where(x => x.BookingId == bookingId)
            .OrderByDescending(x => x.PaidAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => Map(x, data))
            .ToArray();
    }

    public async Task<PaymentDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var payment = data.Payments.FirstOrDefault(x => x.Id == id);
        if (payment is null)
        {
            return null;
        }

        var booking = data.BookingsById[payment.BookingId];
        var customer = data.CustomersById[booking.CustomerId];
        var vehicle = data.VehiclesById[booking.VehicleId];

        return new PaymentDetailDto(
            Map(payment, data),
            new PaymentBookingSnapshotDto(
                booking.Id,
                booking.BookingNumber,
                customer.Id,
                customer.FullName,
                vehicle.Id,
                $"{vehicle.PlateNumber} - {vehicle.Brand} {vehicle.Model}",
                booking.StartAtUtc,
                booking.EndAtUtc,
                booking.Status),
            BuildSummary(booking, data));
    }

    public async Task<PaymentDto> RecordAsync(RecordPaymentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRecordRequest(request);

        var data = await LoadContextAsync(cancellationToken);
        var booking = data.BookingsById.GetValueOrDefault(request.BookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        EnsureBookingEligibleForPayment(booking);

        var paymentTotal = CalculateBookingTotal(booking, data.LatestRentalByBookingId);
        var paidSummary = SummarizePaymentsForBooking(booking.Id, data.PaymentsByBookingId);
        if (paidSummary.NetPaid + request.Amount > paymentTotal)
        {
            throw new InvalidOperationException("Payment exceeds outstanding balance.");
        }

        EnsureReferenceRules(request, data.Payments);

        var paidAtUtc = request.PaidAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        if (paidAtUtc > DateTime.UtcNow.AddMinutes(5))
        {
            throw new InvalidOperationException("Payment date cannot be in the future.");
        }

        var payment = new Payment
        {
            BookingId = request.BookingId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = request.ReferenceNumber.Trim(),
            PaidAtUtc = paidAtUtc,
            Notes = request.Notes.Trim(),
            PaymentStatus = PaymentStatus.Paid
        };

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(payment.Id, cancellationToken)
            ?? throw new InvalidOperationException("Payment could not be reloaded.")).Payment;
    }

    public async Task<PaymentDto?> RefundAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var payment = data.Payments.FirstOrDefault(x => x.Id == id);
        if (payment is null)
        {
            return null;
        }

        if (payment.PaymentStatus == PaymentStatus.Refunded)
        {
            return null;
        }

        var booking = data.BookingsById.GetValueOrDefault(payment.BookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        if (booking.Status == BookingStatus.Active)
        {
            throw new InvalidOperationException("Active rental payments cannot be refunded until check-in is completed.");
        }

        payment.PaymentStatus = PaymentStatus.Refunded;
        payment.UpdatedAtUtc = DateTime.UtcNow;

        await _paymentRepository.UpdateAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(payment.Id, cancellationToken)
            ?? throw new InvalidOperationException("Payment could not be reloaded.")).Payment;
    }

    public async Task<PaymentSummaryDto> GetSummaryAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var data = await LoadContextAsync(cancellationToken);
        var booking = data.BookingsById.GetValueOrDefault(bookingId)
            ?? throw new InvalidOperationException("Booking not found.");
        return BuildSummary(booking, data);
    }

    private async Task<PaymentDataContext> LoadContextAsync(CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.ListAsync(cancellationToken);
        var bookings = (await _bookingRepository.ListAsync(cancellationToken)).ToDictionary(x => x.Id);
        var rentals = await _rentalRepository.ListAsync(cancellationToken);
        var customers = (await _customerRepository.ListAsync(cancellationToken)).ToDictionary(x => x.Id);
        var vehicles = (await _vehicleRepository.ListAsync(cancellationToken)).ToDictionary(x => x.Id);

        var latestRentalByBookingId = rentals
            .GroupBy(x => x.BookingId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(r => r.CheckInAtUtc ?? r.CheckOutAtUtc ?? r.CreatedAtUtc).First());

        var paymentsByBookingId = payments
            .GroupBy(x => x.BookingId)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<Payment>)x.OrderByDescending(p => p.PaidAtUtc).ToArray());

        return new PaymentDataContext(
            payments,
            bookings,
            latestRentalByBookingId,
            customers,
            vehicles,
            paymentsByBookingId);
    }

    private static PaymentDto Map(Payment payment, PaymentDataContext data)
    {
        var booking = data.BookingsById.GetValueOrDefault(payment.BookingId)
            ?? throw new InvalidOperationException("Payment booking mapping is missing.");
        var customer = data.CustomersById.GetValueOrDefault(booking.CustomerId)
            ?? throw new InvalidOperationException("Payment customer mapping is missing.");
        var vehicle = data.VehiclesById.GetValueOrDefault(booking.VehicleId)
            ?? throw new InvalidOperationException("Payment vehicle mapping is missing.");

        var summary = SummarizePaymentsForBooking(booking.Id, data.PaymentsByBookingId);
        var bookingTotal = CalculateBookingTotal(booking, data.LatestRentalByBookingId);
        var outstanding = Math.Max(0, bookingTotal - summary.NetPaid);

        return new PaymentDto(
            payment.Id,
            payment.BookingId,
            booking.BookingNumber,
            customer.Id,
            customer.FullName,
            vehicle.Id,
            $"{vehicle.PlateNumber} - {vehicle.Brand} {vehicle.Model}",
            payment.Amount,
            payment.PaymentMethod,
            payment.ReferenceNumber,
            payment.PaymentStatus,
            payment.PaidAtUtc,
            payment.Notes,
            bookingTotal,
            summary.NetPaid,
            outstanding,
            payment.PaymentStatus == PaymentStatus.Refunded,
            payment.CreatedAtUtc,
            payment.UpdatedAtUtc);
    }

    private static PaymentSummaryDto BuildSummary(Booking booking, PaymentDataContext data)
    {
        var bookingTotal = CalculateBookingTotal(booking, data.LatestRentalByBookingId);
        var summary = SummarizePaymentsForBooking(booking.Id, data.PaymentsByBookingId);
        var outstanding = Math.Max(0, bookingTotal - summary.NetPaid);

        var balanceStatus = summary.NetPaid <= 0
            ? "Unpaid"
            : outstanding <= 0
                ? "Paid"
                : "Partial";

        return new PaymentSummaryDto(
            booking.Id,
            booking.BookingNumber,
            bookingTotal,
            summary.TotalPaid,
            summary.TotalRefunded,
            summary.NetPaid,
            outstanding,
            balanceStatus);
    }

    private static PaymentRollup SummarizePaymentsForBooking(
        Guid bookingId,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Payment>> paymentsByBookingId)
    {
        var payments = paymentsByBookingId.GetValueOrDefault(bookingId) ?? [];
        var totalPaid = payments.Where(x => x.PaymentStatus == PaymentStatus.Paid).Sum(x => x.Amount);
        var totalRefunded = payments.Where(x => x.PaymentStatus == PaymentStatus.Refunded).Sum(x => x.Amount);
        return new PaymentRollup(totalPaid, totalRefunded, totalPaid - totalRefunded);
    }

    private static decimal CalculateBookingTotal(
        Booking booking,
        IReadOnlyDictionary<Guid, RentalTransaction> latestRentalByBookingId)
    {
        var rental = latestRentalByBookingId.GetValueOrDefault(booking.Id);
        return rental is not null && rental.FinalAmount > 0 ? rental.FinalAmount : booking.QuotedTotal;
    }

    private static bool MatchesSearch(Payment payment, string? search, PaymentDataContext data)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        var term = search.Trim();
        var booking = data.BookingsById[payment.BookingId];
        var customer = data.CustomersById[booking.CustomerId];
        var vehicle = data.VehiclesById[booking.VehicleId];

        return booking.BookingNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               customer.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               customer.Phone.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               vehicle.PlateNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               payment.ReferenceNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               payment.Notes.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateListRequest(PaymentListRequest request)
    {
        if (request.PaidFromUtc.HasValue && request.PaidToUtc.HasValue && request.PaidFromUtc > request.PaidToUtc)
        {
            throw new InvalidOperationException("Paid from date cannot be after paid to date.");
        }
    }

    private static void ValidateRecordRequest(RecordPaymentRequest request)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        }

        if (request.Amount > 1_000_000m)
        {
            throw new InvalidOperationException("Payment amount exceeds allowed transaction limit.");
        }

        if (request.PaymentMethod is PaymentMethod.Upi or PaymentMethod.Card or PaymentMethod.BankTransfer &&
            string.IsNullOrWhiteSpace(request.ReferenceNumber))
        {
            throw new InvalidOperationException("Reference number is required for non-cash payments.");
        }
    }

    private static void EnsureReferenceRules(RecordPaymentRequest request, IReadOnlyCollection<Payment> payments)
    {
        var reference = request.ReferenceNumber.Trim();
        if (string.IsNullOrWhiteSpace(reference))
        {
            return;
        }

        if (payments.Any(x =>
                x.ReferenceNumber.Equals(reference, StringComparison.OrdinalIgnoreCase) &&
                x.PaymentStatus == PaymentStatus.Paid))
        {
            throw new InvalidOperationException("Reference number already exists for another paid payment.");
        }
    }

    private static void EnsureBookingEligibleForPayment(Booking booking)
    {
        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled bookings cannot accept payments.");
        }
    }

    private static void EnsureBookingExists(Guid bookingId, IReadOnlyDictionary<Guid, Booking> bookingsById)
    {
        if (!bookingsById.ContainsKey(bookingId))
        {
            throw new InvalidOperationException("Booking not found.");
        }
    }

    private sealed record PaymentDataContext(
        IReadOnlyCollection<Payment> Payments,
        IReadOnlyDictionary<Guid, Booking> BookingsById,
        IReadOnlyDictionary<Guid, RentalTransaction> LatestRentalByBookingId,
        IReadOnlyDictionary<Guid, Customer> CustomersById,
        IReadOnlyDictionary<Guid, Vehicle> VehiclesById,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Payment>> PaymentsByBookingId);

    private sealed record PaymentRollup(decimal TotalPaid, decimal TotalRefunded, decimal NetPaid);
}

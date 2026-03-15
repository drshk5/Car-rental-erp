using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Constants;
using CarRentalERP.Domain.Entities;
using CarRentalERP.Domain.Enums;

namespace CarRentalERP.Application.Dashboard;

public sealed class DashboardService
{
    private readonly IRepository<Vehicle> _vehicleRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<RentalTransaction> _rentalRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IRepository<MaintenanceRecord> _maintenanceRepository;
    private readonly IRepository<Owner> _ownerRepository;

    public DashboardService(
        IRepository<Vehicle> vehicleRepository,
        IRepository<Booking> bookingRepository,
        IRepository<RentalTransaction> rentalRepository,
        IRepository<Payment> paymentRepository,
        IRepository<MaintenanceRecord> maintenanceRepository,
        IRepository<Owner> ownerRepository)
    {
        _vehicleRepository = vehicleRepository;
        _bookingRepository = bookingRepository;
        _rentalRepository = rentalRepository;
        _paymentRepository = paymentRepository;
        _maintenanceRepository = maintenanceRepository;
        _ownerRepository = ownerRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var vehicles = await _vehicleRepository.ListAsync(cancellationToken);
        var bookings = await _bookingRepository.ListAsync(cancellationToken);
        var rentals = await _rentalRepository.ListAsync(cancellationToken);
        var payments = await _paymentRepository.ListAsync(cancellationToken);
        var maintenance = await _maintenanceRepository.ListAsync(cancellationToken);
        var owners = await _ownerRepository.ListAsync(cancellationToken);

        var activeRentalBookings = rentals
            .Where(x => x.Status == RentalStatuses.Active)
            .Select(x => x.BookingId)
            .ToHashSet();

        var unpaidBookings = 0;
        foreach (var booking in bookings.Where(x => x.Status != BookingStatus.Cancelled))
        {
            var bookingPaid = payments
                .Where(x => x.BookingId == booking.Id && x.PaymentStatus == PaymentStatus.Paid)
                .Sum(x => x.Amount);

            var rental = rentals
                .Where(x => x.BookingId == booking.Id)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefault();

            var totalDue = rental is not null && rental.FinalAmount > 0
                ? rental.FinalAmount
                : booking.QuotedTotal;

            if (bookingPaid < totalDue)
            {
                unpaidBookings++;
            }
        }

        var ownerRevenue = owners
            .OrderBy(x => x.DisplayName)
            .Select(owner =>
            {
                var ownerVehicleIds = vehicles.Where(v => v.OwnerId == owner.Id).Select(v => v.Id).ToHashSet();
                var ownerBookingIds = bookings.Where(b => ownerVehicleIds.Contains(b.VehicleId)).Select(b => b.Id).ToHashSet();
                var grossRevenue = payments
                    .Where(p => p.PaymentStatus == PaymentStatus.Paid && ownerBookingIds.Contains(p.BookingId))
                    .Sum(p => p.Amount);
                var partnerShare = grossRevenue * (owner.RevenueSharePercentage / 100m);

                return new OwnerRevenueSummaryDto(
                    owner.Id,
                    owner.DisplayName,
                    grossRevenue,
                    partnerShare,
                    grossRevenue - partnerShare,
                    ownerVehicleIds.Count);
            })
            .ToArray();

        return new DashboardSummaryDto(
            AvailableVehicles: vehicles.Count(x => x.Status == VehicleStatus.Available),
            ActiveRentals: rentals.Count(x => x.Status == RentalStatuses.Active),
            TodayPickups: bookings.Count(x => x.StartAtUtc.Date == today && x.Status is BookingStatus.Confirmed or BookingStatus.Active),
            TodayReturns: bookings.Count(x => x.EndAtUtc.Date == today && x.Status is BookingStatus.Active or BookingStatus.Completed),
            OverdueRentals: bookings.Count(x => activeRentalBookings.Contains(x.Id) && x.EndAtUtc < now),
            UnpaidBookings: unpaidBookings,
            RevenueToday: payments.Where(x => x.PaymentStatus == PaymentStatus.Paid && x.PaidAtUtc.Date == today).Sum(x => x.Amount),
            RevenueThisMonth: payments.Where(x => x.PaymentStatus == PaymentStatus.Paid && x.PaidAtUtc >= monthStart).Sum(x => x.Amount),
            VehiclesInMaintenance: maintenance.Count(x => x.Status != MaintenanceStatus.Completed),
            OwnerRevenue: ownerRevenue);
    }
}

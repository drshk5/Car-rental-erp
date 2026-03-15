using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Constants;
using CarRentalERP.Domain.Entities;
using CarRentalERP.Domain.Enums;
using CarRentalERP.Shared.Contracts;

namespace CarRentalERP.Application.Customers;

public sealed class CustomerService
{
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<Vehicle> _vehicleRepository;
    private readonly IRepository<Branch> _branchRepository;
    private readonly IRepository<RentalTransaction> _rentalRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(
        IRepository<Customer> customerRepository,
        IRepository<Booking> bookingRepository,
        IRepository<Vehicle> vehicleRepository,
        IRepository<Branch> branchRepository,
        IRepository<RentalTransaction> rentalRepository,
        IRepository<Payment> paymentRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _bookingRepository = bookingRepository;
        _vehicleRepository = vehicleRepository;
        _branchRepository = branchRepository;
        _rentalRepository = rentalRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<CustomerDto>> GetPagedAsync(CustomerListRequest request, CancellationToken cancellationToken = default)
    {
        var data = await LoadDataAsync(cancellationToken);

        var filtered = data.Customers
            .Where(x => request.VerificationStatus is null || x.VerificationStatus == request.VerificationStatus)
            .Where(x => request.IsActive is null || x.IsActive == request.IsActive)
            .Where(x => request.HasActiveRental is null || data.Summaries[x.Id].HasActiveRental == request.HasActiveRental)
            .Where(x => request.HasOutstandingBalance is null || (data.Summaries[x.Id].OutstandingBalance > 0) == request.HasOutstandingBalance)
            .Where(x => MatchesSearch(x, request.Search))
            .OrderByDescending(x => data.Summaries[x.Id].HasActiveRental)
            .ThenByDescending(x => data.Summaries[x.Id].LastBookingAtUtc)
            .ThenBy(x => x.FullName)
            .ToArray();

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

        var result = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => Map(x, data.Summaries[x.Id]))
            .ToArray();

        return new PagedResult<CustomerDto>(result, filtered.Length, page, pageSize);
    }

    public async Task<CustomerDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var data = await LoadDataAsync(cancellationToken);
        var customer = data.Customers.FirstOrDefault(x => x.Id == id);
        if (customer is null)
        {
            return null;
        }

        return MapDetail(customer, data);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var customers = await _customerRepository.ListAsync(cancellationToken);
        EnsureUniqueness(customers, request.Phone, request.Email, request.LicenseNumber, request.IdentityDocumentNumber, null);

        var customer = new Customer
        {
            CustomerCode = GenerateCustomerCode(customers.Count + 1),
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            AlternatePhone = request.AlternatePhone.Trim(),
            Email = request.Email.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim(),
            PostalCode = request.PostalCode.Trim(),
            DateOfBirth = request.DateOfBirth,
            Nationality = request.Nationality.Trim(),
            LicenseNumber = request.LicenseNumber.Trim().ToUpperInvariant(),
            LicenseExpiry = request.LicenseExpiry,
            IdentityDocumentType = request.IdentityDocumentType.Trim(),
            IdentityDocumentNumber = request.IdentityDocumentNumber.Trim().ToUpperInvariant(),
            EmergencyContactName = request.EmergencyContactName.Trim(),
            EmergencyContactPhone = request.EmergencyContactPhone.Trim(),
            Notes = request.Notes.Trim(),
            RiskNotes = request.RiskNotes.Trim(),
            IsActive = true,
            VerificationStatus = VerificationStatus.Pending
        };

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(customer, CustomerSummary.Empty);
    }

    public async Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var customers = await _customerRepository.ListAsync(cancellationToken);
        EnsureUniqueness(customers, request.Phone, request.Email, request.LicenseNumber, request.IdentityDocumentNumber, id);

        customer.FullName = request.FullName.Trim();
        customer.Phone = request.Phone.Trim();
        customer.AlternatePhone = request.AlternatePhone.Trim();
        customer.Email = request.Email.Trim();
        customer.Address = request.Address.Trim();
        customer.City = request.City.Trim();
        customer.State = request.State.Trim();
        customer.PostalCode = request.PostalCode.Trim();
        customer.DateOfBirth = request.DateOfBirth;
        customer.Nationality = request.Nationality.Trim();
        customer.LicenseNumber = request.LicenseNumber.Trim().ToUpperInvariant();
        customer.LicenseExpiry = request.LicenseExpiry;
        customer.IdentityDocumentType = request.IdentityDocumentType.Trim();
        customer.IdentityDocumentNumber = request.IdentityDocumentNumber.Trim().ToUpperInvariant();
        customer.EmergencyContactName = request.EmergencyContactName.Trim();
        customer.EmergencyContactPhone = request.EmergencyContactPhone.Trim();
        customer.Notes = request.Notes.Trim();
        customer.RiskNotes = request.RiskNotes.Trim();
        customer.UpdatedAtUtc = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = await GetByIdAsync(customer.Id, cancellationToken)
            ?? throw new InvalidOperationException("Customer detail could not be reloaded.");

        return detail.Profile;
    }

    public async Task<CustomerDto?> SetVerificationAsync(Guid id, SetCustomerVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        customer.VerificationStatus = request.VerificationStatus;
        customer.UpdatedAtUtc = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = await GetByIdAsync(customer.Id, cancellationToken)
            ?? throw new InvalidOperationException("Customer detail could not be reloaded.");

        return detail.Profile;
    }

    public async Task<CustomerDto?> SetStatusAsync(Guid id, SetCustomerStatusRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        customer.IsActive = request.IsActive;
        customer.UpdatedAtUtc = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = await GetByIdAsync(customer.Id, cancellationToken)
            ?? throw new InvalidOperationException("Customer detail could not be reloaded.");

        return detail.Profile;
    }

    private async Task<CustomerDataContext> LoadDataAsync(CancellationToken cancellationToken)
    {
        var customers = await _customerRepository.ListAsync(cancellationToken);
        var bookings = await _bookingRepository.ListAsync(cancellationToken);
        var vehicles = (await _vehicleRepository.ListAsync(cancellationToken)).ToDictionary(x => x.Id);
        var branches = (await _branchRepository.ListAsync(cancellationToken)).ToDictionary(x => x.Id);
        var rentals = await _rentalRepository.ListAsync(cancellationToken);
        var payments = await _paymentRepository.ListAsync(cancellationToken);

        var rentalByBookingId = rentals
            .GroupBy(x => x.BookingId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(r => r.CheckOutAtUtc ?? r.CreatedAtUtc).First());

        var paymentsByBookingId = payments
            .GroupBy(x => x.BookingId)
            .ToDictionary(x => x.Key, x => x.ToArray());

        var bookingsByCustomerId = bookings
            .GroupBy(x => x.CustomerId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(b => b.StartAtUtc).ToArray());

        var summaries = customers.ToDictionary(
            customer => customer.Id,
            customer =>
            {
                bookingsByCustomerId.TryGetValue(customer.Id, out var customerBookings);
                customerBookings ??= [];

                var completedRentals = customerBookings
                    .Select(x => rentalByBookingId.GetValueOrDefault(x.Id))
                    .Count(x => x?.Status == RentalStatuses.Completed);

                var totalQuoted = customerBookings.Sum(x => x.QuotedTotal);
                var totalPaid = customerBookings.Sum(x => paymentsByBookingId.GetValueOrDefault(x.Id)?.Sum(p => p.Amount) ?? 0m);
                var outstandingBalance = Math.Max(0, totalQuoted - totalPaid);

                return new CustomerSummary(
                    customerBookings.Length,
                    completedRentals,
                    totalPaid,
                    outstandingBalance,
                    customerBookings.MaxBy(x => x.StartAtUtc)?.StartAtUtc,
                    customerBookings.Any(x => rentalByBookingId.GetValueOrDefault(x.Id)?.Status == RentalStatuses.Active));
            });

        return new CustomerDataContext(customers, bookingsByCustomerId, rentalByBookingId, paymentsByBookingId, vehicles, branches, summaries);
    }

    private CustomerDetailDto MapDetail(Customer customer, CustomerDataContext data)
    {
        data.BookingsByCustomerId.TryGetValue(customer.Id, out var bookings);
        bookings ??= [];

        var recentBookings = bookings
            .Take(5)
            .Select(booking =>
            {
                var vehicle = data.Vehicles.GetValueOrDefault(booking.VehicleId);
                var pickupBranch = data.Branches.GetValueOrDefault(booking.PickupBranchId);
                var returnBranch = data.Branches.GetValueOrDefault(booking.ReturnBranchId);
                var totalPaid = data.PaymentsByBookingId.GetValueOrDefault(booking.Id)?.Sum(x => x.Amount) ?? 0m;

                return new CustomerBookingSnapshotDto(
                    booking.Id,
                    booking.BookingNumber,
                    vehicle is null ? "Vehicle unavailable" : $"{vehicle.PlateNumber} - {vehicle.Brand} {vehicle.Model}",
                    pickupBranch?.Name ?? "Pickup branch unavailable",
                    returnBranch?.Name ?? "Return branch unavailable",
                    booking.StartAtUtc,
                    booking.EndAtUtc,
                    booking.Status,
                    booking.QuotedTotal,
                    totalPaid,
                    Math.Max(0, booking.QuotedTotal - totalPaid));
            })
            .ToArray();

        var activeBooking = bookings.FirstOrDefault(x => data.RentalByBookingId.GetValueOrDefault(x.Id)?.Status == RentalStatuses.Active);
        CustomerRentalSnapshotDto? activeRental = null;

        if (activeBooking is not null)
        {
            var rental = data.RentalByBookingId[activeBooking.Id];
            var vehicle = data.Vehicles.GetValueOrDefault(activeBooking.VehicleId);

            activeRental = new CustomerRentalSnapshotDto(
                rental.Id,
                activeBooking.BookingNumber,
                vehicle is null ? "Vehicle unavailable" : $"{vehicle.PlateNumber} - {vehicle.Brand} {vehicle.Model}",
                rental.CheckOutAtUtc,
                rental.CheckInAtUtc,
                rental.OdometerOut,
                rental.OdometerIn,
                rental.FuelOut,
                rental.FuelIn,
                rental.FinalAmount,
                rental.Status,
                rental.DamageNotes);
        }

        return new CustomerDetailDto(Map(customer, data.Summaries[customer.Id]), recentBookings, activeRental);
    }

    private static CustomerDto Map(Customer customer, CustomerSummary summary) =>
        new(
            customer.Id,
            customer.CustomerCode,
            customer.FullName,
            customer.Phone,
            customer.AlternatePhone,
            customer.Email,
            customer.Address,
            customer.City,
            customer.State,
            customer.PostalCode,
            customer.DateOfBirth,
            customer.Nationality,
            customer.LicenseNumber,
            customer.LicenseExpiry,
            customer.IdentityDocumentType,
            customer.IdentityDocumentNumber,
            customer.EmergencyContactName,
            customer.EmergencyContactPhone,
            customer.Notes,
            customer.RiskNotes,
            customer.IsActive,
            customer.VerificationStatus,
            summary.TotalBookings,
            summary.CompletedRentals,
            summary.LifetimeValue,
            summary.OutstandingBalance,
            summary.LastBookingAtUtc,
            summary.HasActiveRental,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc);

    private static bool MatchesSearch(Customer customer, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return new[]
        {
            customer.CustomerCode,
            customer.FullName,
            customer.Phone,
            customer.AlternatePhone,
            customer.Email,
            customer.City,
            customer.State,
            customer.LicenseNumber,
            customer.IdentityDocumentNumber,
            customer.EmergencyContactName
        }.Any(value => value.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidateRequest(CreateCustomerRequest request)
    {
        ValidateCommon(
            request.FullName,
            request.Phone,
            request.AlternatePhone,
            request.Email,
            request.Address,
            request.City,
            request.State,
            request.PostalCode,
            request.DateOfBirth,
            request.Nationality,
            request.LicenseNumber,
            request.LicenseExpiry,
            request.IdentityDocumentType,
            request.IdentityDocumentNumber,
            request.EmergencyContactName,
            request.EmergencyContactPhone);
    }

    private static void ValidateRequest(UpdateCustomerRequest request)
    {
        ValidateCommon(
            request.FullName,
            request.Phone,
            request.AlternatePhone,
            request.Email,
            request.Address,
            request.City,
            request.State,
            request.PostalCode,
            request.DateOfBirth,
            request.Nationality,
            request.LicenseNumber,
            request.LicenseExpiry,
            request.IdentityDocumentType,
            request.IdentityDocumentNumber,
            request.EmergencyContactName,
            request.EmergencyContactPhone);
    }

    private static void ValidateCommon(
        string fullName,
        string phone,
        string alternatePhone,
        string email,
        string address,
        string city,
        string state,
        string postalCode,
        DateOnly? dateOfBirth,
        string nationality,
        string licenseNumber,
        DateOnly? licenseExpiry,
        string identityDocumentType,
        string identityDocumentNumber,
        string emergencyContactName,
        string emergencyContactPhone)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 3)
        {
            throw new InvalidOperationException("Full name must be at least 3 characters.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new InvalidOperationException("Primary phone is required.");
        }

        if (!string.IsNullOrWhiteSpace(alternatePhone) &&
            phone.Trim().Equals(alternatePhone.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Alternate phone must be different from the primary phone.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new InvalidOperationException("A valid email address is required.");
        }

        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(postalCode))
        {
            throw new InvalidOperationException("Address, city, state, and postal code are required.");
        }

        if (dateOfBirth is null)
        {
            throw new InvalidOperationException("Date of birth is required.");
        }

        var adultCutoff = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-18));
        if (dateOfBirth > adultCutoff)
        {
            throw new InvalidOperationException("Customer must be at least 18 years old.");
        }

        if (string.IsNullOrWhiteSpace(nationality))
        {
            throw new InvalidOperationException("Nationality is required.");
        }

        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            throw new InvalidOperationException("License number is required.");
        }

        if (licenseExpiry is null || licenseExpiry <= DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            throw new InvalidOperationException("License expiry must be in the future.");
        }

        if (string.IsNullOrWhiteSpace(identityDocumentType) || string.IsNullOrWhiteSpace(identityDocumentNumber))
        {
            throw new InvalidOperationException("Identity document type and number are required.");
        }

        if (string.IsNullOrWhiteSpace(emergencyContactName) || string.IsNullOrWhiteSpace(emergencyContactPhone))
        {
            throw new InvalidOperationException("Emergency contact name and phone are required.");
        }
    }

    private static void EnsureUniqueness(
        IReadOnlyCollection<Customer> customers,
        string phone,
        string email,
        string licenseNumber,
        string identityDocumentNumber,
        Guid? currentId)
    {
        var normalizedPhone = phone.Trim();
        var normalizedEmail = email.Trim();
        var normalizedLicense = licenseNumber.Trim().ToUpperInvariant();
        var normalizedIdentityDocument = identityDocumentNumber.Trim().ToUpperInvariant();

        if (customers.Any(x => x.Id != currentId && x.Phone.Equals(normalizedPhone, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Phone already exists.");
        }

        if (customers.Any(x => x.Id != currentId && x.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        if (customers.Any(x => x.Id != currentId && x.LicenseNumber.Equals(normalizedLicense, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("License number already exists.");
        }

        if (customers.Any(x => x.Id != currentId && x.IdentityDocumentNumber.Equals(normalizedIdentityDocument, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Identity document number already exists.");
        }
    }

    private static string GenerateCustomerCode(int sequence) =>
        $"CUS-{DateTime.UtcNow:yyyyMMdd}-{sequence:0000}";

    private sealed record CustomerSummary(
        int TotalBookings,
        int CompletedRentals,
        decimal LifetimeValue,
        decimal OutstandingBalance,
        DateTime? LastBookingAtUtc,
        bool HasActiveRental)
    {
        public static CustomerSummary Empty { get; } = new(0, 0, 0m, 0m, null, false);
    }

    private sealed record CustomerDataContext(
        IReadOnlyCollection<Customer> Customers,
        IReadOnlyDictionary<Guid, Booking[]> BookingsByCustomerId,
        IReadOnlyDictionary<Guid, RentalTransaction> RentalByBookingId,
        IReadOnlyDictionary<Guid, Payment[]> PaymentsByBookingId,
        IReadOnlyDictionary<Guid, Vehicle> Vehicles,
        IReadOnlyDictionary<Guid, Branch> Branches,
        IReadOnlyDictionary<Guid, CustomerSummary> Summaries);
}

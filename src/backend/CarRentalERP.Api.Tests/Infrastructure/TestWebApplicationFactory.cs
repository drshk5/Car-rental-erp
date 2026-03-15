using System.Text.Json;
using CarRentalERP.Application.Auth;
using CarRentalERP.Domain.Entities;
using CarRentalERP.Domain.Enums;
using CarRentalERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarRentalERP.Api.Tests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private string? _databasePath;
    private string? _originalConnectionString;
    private string? _originalSigningKey;
    private string? _originalDemoSeedEnabled;

    public TestWebApplicationFactory()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"car-rental-erp-tests-{Guid.NewGuid():N}.db");

        _originalConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        _originalSigningKey = Environment.GetEnvironmentVariable("Authentication__SigningKey");
        _originalDemoSeedEnabled = Environment.GetEnvironmentVariable("Seeding__DemoDataEnabled");

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $"Data Source={_databasePath}");
        Environment.SetEnvironmentVariable("Authentication__SigningKey", "test-signing-key-with-at-least-32-chars");
        Environment.SetEnvironmentVariable("Seeding__DemoDataEnabled", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public async Task<(string Email, string Password)> EnsureAuthUserAsync(
        string email = "test.admin@carrental.local",
        string password = "Change-me-123!")
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (existingUser is not null)
        {
            return (email, password);
        }

        var branch = new Branch
        {
            Name = $"Test Branch {Guid.NewGuid():N}".Substring(0, 20),
            City = "Test City",
            Address = "Test Address",
            Phone = "+1-555-0100"
        };

        var role = new Role
        {
            RoleType = UserRoleType.Admin,
            Name = $"Test Admin {Guid.NewGuid():N}".Substring(0, 20),
            PermissionsJson = JsonSerializer.Serialize(new[] { "*" })
        };

        var user = new User
        {
            BranchId = branch.Id,
            RoleId = role.Id,
            FullName = "Integration Test User",
            Email = email,
            PasswordHash = PasswordSecurity.HashPassword(password),
            IsActive = true
        };

        dbContext.Branches.Add(branch);
        dbContext.Roles.Add(role);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return (email, password);
    }

    public async Task<BookingScenario> CreateBookingScenarioAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var branch = new Branch
        {
            Name = $"Booking Branch {Guid.NewGuid():N}".Substring(0, 20),
            City = "Test City",
            Address = "Booking Address",
            Phone = "+1-555-0200",
            IsActive = true
        };

        var owner = new Owner
        {
            DisplayName = $"Owner {Guid.NewGuid():N}".Substring(0, 16),
            ContactName = "Owner Contact",
            Email = $"owner-{Guid.NewGuid():N}@test.local",
            Phone = "+1-555-0201",
            RevenueSharePercentage = 50,
            IsActive = true
        };

        var vehicle = new Vehicle
        {
            BranchId = branch.Id,
            OwnerId = owner.Id,
            PlateNumber = $"TEST{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            Vin = $"VIN-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Brand = "Toyota",
            Model = "Corolla",
            Year = DateTime.UtcNow.Year,
            DailyRate = 100,
            HourlyRate = 10,
            KmRate = 2,
            Status = VehicleStatus.Available
        };

        var customer = new Customer
        {
            CustomerCode = $"CUS-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            FullName = "Booking Test Customer",
            Phone = $"+1-555-{Random.Shared.Next(1000, 9999)}",
            AlternatePhone = "+1-555-9999",
            Email = $"customer-{Guid.NewGuid():N}@test.local",
            Address = "Customer Address",
            City = "Test City",
            State = "Test State",
            PostalCode = "12345",
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
            Nationality = "Test",
            LicenseNumber = $"LIC-{Guid.NewGuid():N}"[..14].ToUpperInvariant(),
            LicenseExpiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
            IdentityDocumentType = "Passport",
            IdentityDocumentNumber = $"ID-{Guid.NewGuid():N}"[..14].ToUpperInvariant(),
            EmergencyContactName = "Emergency Contact",
            EmergencyContactPhone = "+1-555-0300",
            Notes = string.Empty,
            RiskNotes = string.Empty,
            IsActive = true,
            VerificationStatus = VerificationStatus.Verified
        };

        dbContext.Branches.Add(branch);
        dbContext.Owners.Add(owner);
        dbContext.Vehicles.Add(vehicle);
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        return new BookingScenario(customer.Id, vehicle.Id, branch.Id, branch.Id);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _originalConnectionString);
        Environment.SetEnvironmentVariable("Authentication__SigningKey", _originalSigningKey);
        Environment.SetEnvironmentVariable("Seeding__DemoDataEnabled", _originalDemoSeedEnabled);

        if (string.IsNullOrWhiteSpace(_databasePath))
        {
            return;
        }

        foreach (var suffix in new[] { string.Empty, "-shm", "-wal" })
        {
            var path = _databasePath + suffix;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    public sealed record BookingScenario(
        Guid CustomerId,
        Guid VehicleId,
        Guid PickupBranchId,
        Guid ReturnBranchId);
}

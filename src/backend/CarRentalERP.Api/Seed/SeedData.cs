using CarRentalERP.Application.Abstractions;
using CarRentalERP.Application.Auth;
using CarRentalERP.Api.Auth;
using CarRentalERP.Domain.Entities;
using CarRentalERP.Domain.Enums;
using System.Text.Json;

namespace CarRentalERP.Api.Seed;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var branchRepository = scope.ServiceProvider.GetRequiredService<IRepository<Branch>>();
        var roleRepository = scope.ServiceProvider.GetRequiredService<IRepository<Role>>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
        var ownerRepository = scope.ServiceProvider.GetRequiredService<IRepository<Owner>>();
        var vehicleRepository = scope.ServiceProvider.GetRequiredService<IRepository<Vehicle>>();
        var customerRepository = scope.ServiceProvider.GetRequiredService<IRepository<Customer>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if ((await userRepository.ListAsync()).Any())
        {
            return;
        }

        var branch = new Branch
        {
            Name = "HQ Branch",
            City = "Ahmedabad",
            Address = "Corporate Road",
            Phone = "+91-9000000000"
        };

        var adminRole = new Role
        {
            RoleType = UserRoleType.Admin,
            Name = "Admin",
            PermissionsJson = JsonSerializer.Serialize(new[] { "*" })
        };

        var managerRole = new Role
        {
            RoleType = UserRoleType.Manager,
            Name = "Manager",
            PermissionsJson = JsonSerializer.Serialize(new[]
            {
                Permissions.AddEditVehicles,
                Permissions.CreateEditBooking,
                Permissions.CancelBooking,
                Permissions.CheckoutCheckin,
                Permissions.RecordPayment,
                Permissions.RefundPayment,
                Permissions.ViewReports,
                Permissions.VerifyCustomer
            })
        };

        var staffRole = new Role
        {
            RoleType = UserRoleType.Staff,
            Name = "Staff",
            PermissionsJson = JsonSerializer.Serialize(new[]
            {
                Permissions.CreateEditBooking,
                Permissions.CheckoutCheckin,
                Permissions.RecordPayment
            })
        };

        var adminUser = new User
        {
            BranchId = branch.Id,
            RoleId = adminRole.Id,
            FullName = "System Admin",
            Email = "admin@carrental.local",
            PasswordHash = PasswordSecurity.HashPassword("change-me")
        };

        var managerUser = new User
        {
            BranchId = branch.Id,
            RoleId = managerRole.Id,
            FullName = "Branch Manager",
            Email = "manager@carrental.local",
            PasswordHash = PasswordSecurity.HashPassword("change-me")
        };

        var staffUser = new User
        {
            BranchId = branch.Id,
            RoleId = staffRole.Id,
            FullName = "Operations Staff",
            Email = "staff@carrental.local",
            PasswordHash = PasswordSecurity.HashPassword("change-me")
        };

        var owner = new Owner
        {
            DisplayName = "Patel Fleet Partners",
            ContactName = "Nirav Patel",
            Email = "owners@carrental.local",
            Phone = "+91-9222222222",
            RevenueSharePercentage = 62.5m
        };

        var demoVehicle = new Vehicle
        {
            BranchId = branch.Id,
            OwnerId = owner.Id,
            PlateNumber = "GJ01AB1234",
            Vin = "VIN-DEMO-0001",
            Brand = "Toyota",
            Model = "Innova Crysta",
            Year = 2024,
            DailyRate = 4500,
            HourlyRate = 500,
            KmRate = 18,
            Status = VehicleStatus.Available
        };

        await branchRepository.AddAsync(branch);
        await roleRepository.AddAsync(adminRole);
        await roleRepository.AddAsync(managerRole);
        await roleRepository.AddAsync(staffRole);
        await userRepository.AddAsync(adminUser);
        await userRepository.AddAsync(managerUser);
        await userRepository.AddAsync(staffUser);
        await ownerRepository.AddAsync(owner);
        await vehicleRepository.AddAsync(demoVehicle);
        await customerRepository.AddAsync(new Customer
        {
            CustomerCode = "CUS-20260307-0001",
            FullName = "Demo Customer",
            Phone = "+91-9111111111",
            AlternatePhone = "+91-9333333333",
            Email = "customer@carrental.local",
            Address = "Satellite Road",
            City = "Ahmedabad",
            State = "Gujarat",
            PostalCode = "380015",
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-30)),
            Nationality = "Indian",
            LicenseNumber = "DL-DEMO-0001",
            LicenseExpiry = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(3)),
            IdentityDocumentType = "Passport",
            IdentityDocumentNumber = "P1234567",
            EmergencyContactName = "Aarav Customer",
            EmergencyContactPhone = "+91-9444444444",
            Notes = "Prefers premium MPV and airport pickup coordination.",
            RiskNotes = string.Empty,
            IsActive = true,
            VerificationStatus = VerificationStatus.Verified
        });

        await unitOfWork.SaveChangesAsync();
    }
}

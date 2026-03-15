using CarRentalERP.Application.Abstractions;
using CarRentalERP.Domain.Entities;
using CarRentalERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarRentalERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IRepository<Branch>, EfRepository<Branch>>();
        services.AddScoped<IRepository<Role>, EfRepository<Role>>();
        services.AddScoped<IRepository<User>, EfRepository<User>>();
        services.AddScoped<IRepository<Owner>, EfRepository<Owner>>();
        services.AddScoped<IRepository<Vehicle>, EfRepository<Vehicle>>();
        services.AddScoped<IRepository<Customer>, EfRepository<Customer>>();
        services.AddScoped<IRepository<Booking>, EfRepository<Booking>>();
        services.AddScoped<IRepository<RentalTransaction>, EfRepository<RentalTransaction>>();
        services.AddScoped<IRepository<Payment>, EfRepository<Payment>>();
        services.AddScoped<IRepository<MaintenanceRecord>, EfRepository<MaintenanceRecord>>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}

using CarRentalERP.Api.Seed;
using CarRentalERP.Api.Auth;
using CarRentalERP.Api.Middleware;
using CarRentalERP.Application.Auth;
using CarRentalERP.Application.Bookings;
using CarRentalERP.Application.Branches;
using CarRentalERP.Application.Customers;
using CarRentalERP.Application.Dashboard;
using CarRentalERP.Application.Health;
using CarRentalERP.Application.Maintenance;
using CarRentalERP.Application.Owners;
using CarRentalERP.Application.Payments;
using CarRentalERP.Application.Rentals;
using CarRentalERP.Application.Roles;
using CarRentalERP.Application.Users;
using CarRentalERP.Application.Vehicles;
using CarRentalERP.Infrastructure;
using CarRentalERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtAuthOptions>(builder.Configuration.GetSection(JwtAuthOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtAuthOptions.SectionName).Get<JwtAuthOptions>() ?? new JwtAuthOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < JwtAuthOptions.MinimumSigningKeyLength)
{
    throw new InvalidOperationException(
        $"Authentication signing key is not configured. Set {JwtAuthOptions.SectionName}__SigningKey with at least {JwtAuthOptions.MinimumSigningKeyLength} characters.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Car Rental ERP API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Provide the JWT bearer token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(jwtOptions.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.ManageUsersAndRoles, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.ManageUsersAndRoles)));
    options.AddPolicy(AuthorizationPolicies.ManageBranches, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.ManageBranches)));
    options.AddPolicy(AuthorizationPolicies.AddEditVehicles, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.AddEditVehicles)));
    options.AddPolicy(AuthorizationPolicies.DeactivateVehicle, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.DeactivateVehicle)));
    options.AddPolicy(AuthorizationPolicies.CreateEditBooking, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.CreateEditBooking)));
    options.AddPolicy(AuthorizationPolicies.CancelBooking, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.CancelBooking)));
    options.AddPolicy(AuthorizationPolicies.CheckoutCheckin, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.CheckoutCheckin)));
    options.AddPolicy(AuthorizationPolicies.RecordPayment, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.RecordPayment)));
    options.AddPolicy(AuthorizationPolicies.RefundPayment, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.RefundPayment)));
    options.AddPolicy(AuthorizationPolicies.ViewReports, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.ViewReports)));
    options.AddPolicy(AuthorizationPolicies.VerifyCustomer, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.VerifyCustomer)));
});

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<BranchService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<SystemHealthService>();
builder.Services.AddScoped<MaintenanceService>();
builder.Services.AddScoped<OwnerService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<RentalService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<VehicleService>();

var app = builder.Build();

await app.Services.ApplyMigrationsAsync();

var seedDemoData = app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Seeding:DemoDataEnabled");
if (seedDemoData)
{
    await SeedData.SeedAsync(app.Services);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!string.IsNullOrWhiteSpace(app.Configuration["ASPNETCORE_HTTPS_PORT"]) ||
    !string.IsNullOrWhiteSpace(app.Configuration["HTTPS_PORT"]))
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;

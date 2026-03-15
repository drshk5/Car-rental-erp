namespace CarRentalERP.Api.Auth;

public static class Permissions
{
    public const string ManageUsersAndRoles = "manage.users.roles";
    public const string ManageBranches = "manage.branches";
    public const string AddEditVehicles = "vehicles.write";
    public const string DeactivateVehicle = "vehicles.deactivate";
    public const string CreateEditBooking = "bookings.write";
    public const string CancelBooking = "bookings.cancel";
    public const string CheckoutCheckin = "rentals.execute";
    public const string RecordPayment = "payments.record";
    public const string RefundPayment = "payments.refund";
    public const string ViewReports = "reports.view";
    public const string VerifyCustomer = "customers.verify";

    public static readonly IReadOnlyCollection<string> All =
    [
        ManageUsersAndRoles,
        ManageBranches,
        AddEditVehicles,
        DeactivateVehicle,
        CreateEditBooking,
        CancelBooking,
        CheckoutCheckin,
        RecordPayment,
        RefundPayment,
        ViewReports,
        VerifyCustomer
    ];
}

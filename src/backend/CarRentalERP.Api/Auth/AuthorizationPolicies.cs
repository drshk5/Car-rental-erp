namespace CarRentalERP.Api.Auth;

public static class AuthorizationPolicies
{
    public const string ManageUsersAndRoles = "policy.manage.users.roles";
    public const string ManageBranches = "policy.manage.branches";
    public const string AddEditVehicles = "policy.vehicles.write";
    public const string DeactivateVehicle = "policy.vehicles.deactivate";
    public const string CreateEditBooking = "policy.bookings.write";
    public const string CancelBooking = "policy.bookings.cancel";
    public const string CheckoutCheckin = "policy.rentals.execute";
    public const string RecordPayment = "policy.payments.record";
    public const string RefundPayment = "policy.payments.refund";
    public const string ViewReports = "policy.reports.view";
    public const string VerifyCustomer = "policy.customers.verify";
}

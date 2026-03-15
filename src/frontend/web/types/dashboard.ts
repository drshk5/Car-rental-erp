export type DashboardOwnerRevenue = {
  ownerId: string;
  ownerName: string;
  grossRevenue: number;
  partnerShareAmount: number;
  companyShareAmount: number;
  vehicleCount: number;
};

export type DashboardSummary = {
  availableVehicles: number;
  activeRentals: number;
  todayPickups: number;
  todayReturns: number;
  overdueRentals: number;
  unpaidBookings: number;
  revenueToday: number;
  revenueThisMonth: number;
  vehiclesInMaintenance: number;
  ownerRevenue: DashboardOwnerRevenue[];
};

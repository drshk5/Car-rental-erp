export type Owner = {
  id: string;
  displayName: string;
  contactName: string;
  email: string;
  phone: string;
  revenueSharePercentage: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

export type OwnerRevenue = {
  ownerId: string;
  ownerName: string;
  vehicleCount: number;
  activeRentalCount: number;
  completedBookingCount: number;
  grossRevenue: number;
  partnerShareAmount: number;
  companyShareAmount: number;
};

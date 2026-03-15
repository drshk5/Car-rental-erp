export type RentalStatus = "Pending" | "Active" | "Completed" | "Cancelled" | "Overdue";

export type Rental = {
  id: string;
  bookingId: string;
  bookingNumber: string;
  customerId: string;
  customerName: string;
  customerPhone?: string;
  customerEmail?: string;
  vehicleId: string;
  vehicleLabel: string;
  vehiclePlate?: string;
  vehicleVin?: string;
  vehicleImage?: string;
  pickupBranchId: string;
  pickupBranchName: string;
  returnBranchId: string;
  returnBranchName: string;
  bookingStartAtUtc?: string;
  bookingEndAtUtc?: string;
  checkOutAtUtc: string | null;
  checkInAtUtc: string | null;
  scheduledPickupAtUtc?: string;
  scheduledReturnAtUtc?: string;
  actualPickupAtUtc?: string | null;
  actualReturnAtUtc?: string | null;
  odometerOut: number;
  odometerIn: number | null;
  distanceTravelled?: number;
  fuelOut: string;
  fuelIn: string | null;
  extraCharges: number;
  damageNotes: string;
  finalAmount: number;
  baseAmount?: number;
  depositAmount?: number;
  amountPaid?: number;
  totalPaid?: number;
  outstandingBalance?: number;
  isOverdue?: boolean;
  status: RentalStatus;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

export type RentalFilter = {
  status?: RentalStatus;
  customerId?: string;
  vehicleId?: string;
  branchId?: string;
  dateFrom?: string;
  dateTo?: string;
  search?: string;
};

export type RentalCheckoutData = {
  bookingId: string;
  odometerOut: number;
  fuelOut: string;
  notes?: string;
};

export type RentalCheckinData = {
  rentalId: string;
  odometerIn: number;
  fuelIn: string;
  damageNotes?: string;
  extraCharges?: number;
};

export type RentalListItem = {
  id: string;
  bookingNumber: string;
  customerName: string;
  customerPhone: string;
  vehicleLabel: string;
  vehiclePlate: string;
  scheduledPickupAtUtc: string;
  scheduledReturnAtUtc: string;
  checkOutAtUtc: string | null;
  checkInAtUtc: string | null;
  finalAmount: number;
  status: RentalStatus;
};

export type RentalStats = {
  totalRentals: number;
  activeRentals: number;
  completedRentals: number;
  overdueRentals: number;
  todayCheckouts: number;
  todayCheckins: number;
  outstandingBalance: number;
  revenueToday: number;
  revenueThisWeek: number;
  revenueThisMonth: number;
  averageRentalDurationDays: number;
};

export type PaginatedRentals = {
  data: RentalListItem[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
  summary?: {
    totalRentals: number;
    activeRentals: number;
    completedRentals: number;
    overdueRentals: number;
    outstandingBalance: number;
  };
};

export type DashboardRentalSummary = {
  activeRentals: number;
  overdueRentals: number;
  todayPickups: number;
  todayReturns: number;
  upcomingReturns: Rental[];
  recentCheckouts: Rental[];
};

export type RentalDetail = {
  rental: Rental;
  financials: {
    quotedTotal: number;
    extraCharges: number;
    finalAmount: number;
    totalPaid: number;
    outstandingBalance: number;
  };
  timeline: {
    bookingStartAtUtc: string;
    bookingEndAtUtc: string;
    checkOutAtUtc: string | null;
    checkInAtUtc: string | null;
    isOverdue: boolean;
  };
};

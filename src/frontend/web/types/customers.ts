export type Customer = {
  id: string;
  customerCode: string;
  fullName: string;
  phone: string;
  alternatePhone: string;
  email: string;
  address: string;
  city: string;
  state: string;
  postalCode: string;
  dateOfBirth: string | null;
  nationality: string;
  licenseNumber: string;
  licenseExpiry: string | null;
  identityDocumentType: string;
  identityDocumentNumber: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  notes: string;
  riskNotes: string;
  isActive: boolean;
  verificationStatus: string | number;
  totalBookings: number;
  completedRentals: number;
  lifetimeValue: number;
  outstandingBalance: number;
  lastBookingAtUtc: string | null;
  hasActiveRental: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

export type CustomerBookingSnapshot = {
  bookingId: string;
  bookingNumber: string;
  vehicleLabel: string;
  pickupBranchName: string;
  returnBranchName: string;
  startAtUtc: string;
  endAtUtc: string;
  status: string | number;
  quotedTotal: number;
  totalPaid: number;
  outstandingBalance: number;
};

export type CustomerRentalSnapshot = {
  rentalId: string;
  bookingNumber: string;
  vehicleLabel: string;
  checkOutAtUtc: string | null;
  checkInAtUtc: string | null;
  odometerOut: number;
  odometerIn: number | null;
  fuelOut: string;
  fuelIn: string | null;
  finalAmount: number;
  status: string;
  damageNotes: string;
};

export type CustomerDetail = {
  profile: Customer;
  recentBookings: CustomerBookingSnapshot[];
  activeRental: CustomerRentalSnapshot | null;
};

export type CustomerFilters = {
  search?: string;
  verificationStatus?: string;
  isActive?: boolean;
  hasActiveRental?: boolean;
  hasOutstandingBalance?: boolean;
};

export type CustomerFormPayload = {
  fullName: string;
  phone: string;
  alternatePhone: string;
  email: string;
  address: string;
  city: string;
  state: string;
  postalCode: string;
  dateOfBirth: string;
  nationality: string;
  licenseNumber: string;
  licenseExpiry: string;
  identityDocumentType: string;
  identityDocumentNumber: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  notes: string;
  riskNotes: string;
};

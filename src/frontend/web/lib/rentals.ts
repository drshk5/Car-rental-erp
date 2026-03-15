import { fetchApi } from "@/lib/api-client";
import type {
  Rental,
  RentalListItem,
  RentalStats,
  PaginatedRentals,
  RentalFilter,
  RentalCheckoutData,
  RentalCheckinData,
  DashboardRentalSummary,
  RentalDetail,
} from "@/types/rentals";

type RentalApiDto = {
  id: string;
  bookingId: string;
  bookingNumber: string;
  customerId: string;
  customerName: string;
  customerPhone: string;
  customerEmail: string;
  vehicleId: string;
  vehicleLabel: string;
  vehiclePlate: string;
  vehicleVin: string;
  pickupBranchId: string;
  pickupBranchName: string;
  returnBranchId: string;
  returnBranchName: string;
  bookingStartAtUtc: string;
  bookingEndAtUtc: string;
  checkOutAtUtc: string | null;
  checkInAtUtc: string | null;
  odometerOut: number;
  odometerIn: number | null;
  distanceTravelled: number;
  fuelOut: string;
  fuelIn: string | null;
  extraCharges: number;
  damageNotes: string;
  finalAmount: number;
  totalPaid: number;
  outstandingBalance: number;
  isOverdue: boolean;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

type RentalListResponseApi = {
  rentals: {
    items: RentalApiDto[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
  };
  summary: {
    totalRentals: number;
    activeRentals: number;
    completedRentals: number;
    overdueRentals: number;
    outstandingBalance: number;
  };
};

type RentalDetailApi = {
  rental: RentalApiDto;
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

type RentalDashboardSummaryApi = {
  activeRentals: number;
  overdueRentals: number;
  todayPickups: number;
  todayReturns: number;
  upcomingReturns: RentalApiDto[];
  recentCheckouts: RentalApiDto[];
};

function normalizeStatus(rental: Pick<RentalApiDto, "isOverdue" | "status">): Rental["status"] {
  return (rental.isOverdue ? "Overdue" : rental.status) as Rental["status"];
}

function normalizeVehiclePlate(vehicleLabel: string) {
  return vehicleLabel.split(" - ")[0]?.trim() || vehicleLabel;
}

function normalizeRental(rental: RentalApiDto): Rental {
  return {
    ...rental,
    status: normalizeStatus(rental),
    scheduledPickupAtUtc: rental.bookingStartAtUtc,
    scheduledReturnAtUtc: rental.bookingEndAtUtc,
    actualPickupAtUtc: rental.checkOutAtUtc,
    actualReturnAtUtc: rental.checkInAtUtc,
    vehiclePlate: normalizeVehiclePlate(rental.vehicleLabel),
    vehicleVin: rental.vehicleVin,
    customerPhone: rental.customerPhone,
    customerEmail: rental.customerEmail,
    depositAmount: rental.totalPaid,
    amountPaid: rental.totalPaid,
    baseAmount: Math.max(rental.finalAmount - rental.extraCharges, 0),
  };
}

function mapListResponse(response: RentalListResponseApi): PaginatedRentals {
  return {
    data: response.rentals.items.map<RentalListItem>((item) => ({
      id: item.id,
      bookingNumber: item.bookingNumber,
      customerName: item.customerName,
      customerPhone: "",
      vehicleLabel: item.vehicleLabel,
      vehiclePlate: normalizeVehiclePlate(item.vehicleLabel),
      scheduledPickupAtUtc: item.bookingStartAtUtc,
      scheduledReturnAtUtc: item.bookingEndAtUtc,
      checkOutAtUtc: item.checkOutAtUtc,
      checkInAtUtc: item.checkInAtUtc,
      finalAmount: item.finalAmount,
      status: normalizeStatus(item),
    })),
    total: response.rentals.totalCount,
    page: response.rentals.page,
    pageSize: response.rentals.pageSize,
    totalPages: response.rentals.totalPages,
    summary: response.summary,
  };
}

function mapQueueResponse(response: RentalListResponseApi) {
  return {
    items: response.rentals.items.map(normalizeRental),
    summary: response.summary,
    total: response.rentals.totalCount,
    page: response.rentals.page,
    pageSize: response.rentals.pageSize,
    totalPages: response.rentals.totalPages,
  };
}

export function getActiveRentals() {
  return fetchApi<RentalApiDto[]>("/rentals/active").then((items) => items.map(normalizeRental));
}

export function getOverdueRentals() {
  return fetchApi<RentalApiDto[]>("/rentals/overdue").then((items) => items.map(normalizeRental));
}

export function getCompletedRentals(page = 1, pageSize = 20) {
  const params = new URLSearchParams({
    status: "Completed",
    page: String(page),
    pageSize: String(pageSize),
  });

  return fetchApi<RentalListResponseApi>(`/rentals?${params.toString()}`).then((response) => ({
    items: response.rentals.items.map(normalizeRental),
    summary: response.summary,
  }));
}

export function getAllRentals(filters?: RentalFilter, page = 1, pageSize = 20) {
  const params = new URLSearchParams();
  if (filters?.status) params.set("status", filters.status);
  if (filters?.customerId) params.set("customerId", filters.customerId);
  if (filters?.vehicleId) params.set("vehicleId", filters.vehicleId);
  if (filters?.branchId) params.set("branchId", filters.branchId);
  if (filters?.dateFrom) params.set("dateFrom", filters.dateFrom);
  if (filters?.dateTo) params.set("dateTo", filters.dateTo);
  if (filters?.search) params.set("search", filters.search);
  params.set("page", String(page));
  params.set("pageSize", String(pageSize));

  return fetchApi<RentalListResponseApi>(`/rentals?${params.toString()}`).then(mapListResponse);
}

export function getRentalQueue(filters?: RentalFilter, page = 1, pageSize = 20) {
  const params = new URLSearchParams();
  if (filters?.status) params.set("status", filters.status);
  if (filters?.customerId) params.set("customerId", filters.customerId);
  if (filters?.vehicleId) params.set("vehicleId", filters.vehicleId);
  if (filters?.branchId) params.set("branchId", filters.branchId);
  if (filters?.dateFrom) params.set("dateFrom", filters.dateFrom);
  if (filters?.dateTo) params.set("dateTo", filters.dateTo);
  if (filters?.search) params.set("search", filters.search);
  params.set("page", String(page));
  params.set("pageSize", String(pageSize));

  return fetchApi<RentalListResponseApi>(`/rentals?${params.toString()}`).then(mapQueueResponse);
}

export function getRentalStats() {
  return fetchApi<RentalStats>("/rentals/stats");
}

export function getDashboardRentalSummary() {
  return fetchApi<RentalDashboardSummaryApi>("/rentals/dashboard-summary").then((summary): DashboardRentalSummary => ({
    ...summary,
    upcomingReturns: summary.upcomingReturns.map(normalizeRental),
    recentCheckouts: summary.recentCheckouts.map(normalizeRental),
  }));
}

export function getRentalById(id: string) {
  return fetchApi<RentalDetailApi>(`/rentals/${id}`).then((detail): RentalDetail => ({
    rental: normalizeRental(detail.rental),
    financials: detail.financials,
    timeline: detail.timeline,
  }));
}

export function checkoutRental(data: RentalCheckoutData) {
  return fetchApi<RentalApiDto>(`/rentals/checkout`, {
    method: "POST",
    body: JSON.stringify({
      bookingId: data.bookingId,
      odometerOut: data.odometerOut,
      fuelOut: data.fuelOut,
      notes: data.notes ?? "",
    }),
  }).then(normalizeRental);
}

export function checkinRental(data: RentalCheckinData) {
  return fetchApi<RentalApiDto>(`/rentals/${data.rentalId}/checkin`, {
    method: "POST",
    body: JSON.stringify({
      odometerIn: data.odometerIn,
      fuelIn: data.fuelIn,
      extraCharges: data.extraCharges ?? 0,
      damageNotes: data.damageNotes ?? "",
    }),
  }).then(normalizeRental);
}

export function updateRentalDamageNotes(rentalId: string, damageNotes: string) {
  return fetchApi<RentalApiDto>(`/rentals/${rentalId}/damage`, {
    method: "PATCH",
    body: JSON.stringify({ damageNotes }),
  }).then(normalizeRental);
}

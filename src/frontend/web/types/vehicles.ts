export type Vehicle = {
  id: string;
  branchId: string;
  branchName: string;
  ownerId: string;
  ownerName: string;
  plateNumber: string;
  vin: string;
  brand: string;
  model: string;
  year: number;
  dailyRate: number;
  hourlyRate: number;
  kmRate: number;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

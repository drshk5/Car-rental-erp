export type MaintenanceRecord = {
  id: string;
  vehicleId: string;
  vehicleLabel: string;
  serviceType: string;
  scheduledAtUtc: string;
  completedAtUtc: string | null;
  vendorName: string;
  cost: number;
  status: string | number;
  notes: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

export type Payment = {
  id: string;
  bookingId: string;
  bookingNumber: string;
  amount: number;
  paymentMethod: string | number;
  referenceNumber: string;
  paymentStatus: string | number;
  paidAtUtc: string;
  notes: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

export type PaymentSummary = {
  bookingId: string;
  bookingNumber: string;
  bookingTotal: number;
  totalPaid: number;
  outstandingBalance: number;
  balanceStatus: string;
};

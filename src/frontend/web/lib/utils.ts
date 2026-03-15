import { type ClassValue, clsx } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount)
}

export function formatDate(date: string | Date | null | undefined): string {
  if (!date) return "N/A"
  return new Intl.DateTimeFormat('en-IN', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  }).format(new Date(date))
}

export function formatDateTime(date: string | Date | null | undefined): string {
  if (!date) return "N/A"
  return new Intl.DateTimeFormat('en-IN', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(date))
}

export function getInitials(name: string): string {
  return name
    .split(' ')
    .map(n => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)
}

export function getVerificationColor(status: string | number): string {
  const statusStr = String(status).toLowerCase()
  if (statusStr === '2' || statusStr === 'verified') return 'success'
  if (statusStr === '1' || statusStr === 'pending') return 'warning'
  if (statusStr === '3' || statusStr === 'rejected') return 'error'
  return 'default'
}

export function getVerificationLabel(value: string | number): string {
  switch (String(value).toLowerCase()) {
    case '1':
    case 'pending':
      return 'Pending'
    case '2':
    case 'verified':
      return 'Verified'
    case '3':
    case 'rejected':
      return 'Rejected'
    default:
      return String(value)
  }
}

export function getBookingStatusLabel(value: string | number): string {
  switch (String(value).toLowerCase()) {
    case '1':
    case 'draft':
      return 'Draft'
    case '2':
    case 'confirmed':
      return 'Confirmed'
    case '3':
    case 'active':
      return 'Active'
    case '4':
    case 'completed':
      return 'Completed'
    case '5':
    case 'cancelled':
      return 'Cancelled'
    default:
      return String(value)
  }
}

export function getBookingStatusColor(status: string | number): string {
  const statusStr = String(status).toLowerCase()
  if (statusStr === '1' || statusStr === 'draft') return 'default'
  if (statusStr === '2' || statusStr === 'confirmed') return 'info'
  if (statusStr === '3' || statusStr === 'active') return 'warning'
  if (statusStr === '4' || statusStr === 'completed') return 'success'
  if (statusStr === '5' || statusStr === 'cancelled') return 'error'
  return 'default'
}

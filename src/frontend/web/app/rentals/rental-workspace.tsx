"use client";

import type { ReactNode } from "react";
import { useEffect, useMemo, useState, useTransition } from "react";
import { fetchApi } from "@/lib/api-client";
import { checkinRental, checkoutRental, getDashboardRentalSummary, getRentalById, getRentalQueue, getRentalStats, updateRentalDamageNotes } from "@/lib/rentals";
import { formatCurrency, formatDate, formatDateTime } from "@/lib/format";
import type { PagedResult } from "@/types/api";
import type { Booking } from "@/types/bookings";
import type { DashboardRentalSummary, Rental, RentalDetail, RentalStats, RentalStatus } from "@/types/rentals";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Sheet, SheetContent, SheetDescription, SheetFooter, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Textarea } from "@/components/ui/textarea";
import { AlertCircle, CarFront, CheckCircle2, ClipboardList, Clock3, PencilLine, RefreshCw, Search, TriangleAlert } from "lucide-react";

type Notice = {
  tone: "success" | "error";
  message: string;
};

const checkoutFuelOptions = ["Full", "3/4", "1/2", "1/4", "Empty"] as const;

function statusVariant(status: RentalStatus) {
  switch (status) {
    case "Active":
      return "success";
    case "Completed":
      return "info";
    case "Overdue":
      return "error";
    case "Cancelled":
      return "secondary";
    default:
      return "warning";
  }
}

function normalizeBookingStatus(status: Booking["status"]) {
  return String(status).toLowerCase();
}

export function RentalWorkspace() {
  const [pending, startTransition] = useTransition();
  const [rentals, setRentals] = useState<Rental[]>([]);
  const [eligibleBookings, setEligibleBookings] = useState<Booking[]>([]);
  const [stats, setStats] = useState<RentalStats | null>(null);
  const [dashboard, setDashboard] = useState<DashboardRentalSummary | null>(null);
  const [selectedId, setSelectedId] = useState("");
  const [selectedDetail, setSelectedDetail] = useState<RentalDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<"all" | RentalStatus>("all");
  const [checkoutOpen, setCheckoutOpen] = useState(false);
  const [checkinOpen, setCheckinOpen] = useState(false);
  const [damageOpen, setDamageOpen] = useState(false);
  const [checkoutForm, setCheckoutForm] = useState({
    bookingId: "",
    odometerOut: "",
    fuelOut: "Full",
    notes: "",
  });
  const [checkinForm, setCheckinForm] = useState({
    odometerIn: "",
    fuelIn: "Full",
    extraCharges: "0",
    damageNotes: "",
  });
  const [damageNotesDraft, setDamageNotesDraft] = useState("");

  async function loadWorkspace(preferredRentalId?: string) {
    setLoading(true);
    setError(null);

    try {
      const [rentalResponse, bookingResponse, statsResponse, dashboardResponse] = await Promise.all([
        getRentalQueue(undefined, 1, 200),
        fetchApi<PagedResult<Booking>>("/bookings?status=Confirmed&hasActiveRental=false&page=1&pageSize=200"),
        getRentalStats(),
        getDashboardRentalSummary(),
      ]);

      const normalizedBookings = bookingResponse.data.filter(
        (booking) => normalizeBookingStatus(booking.status) === "confirmed" && !booking.hasActiveRental,
      );

      const nextSelectedId = preferredRentalId || selectedId || rentalResponse.items[0]?.id || "";

      setRentals(rentalResponse.items);
      setEligibleBookings(normalizedBookings);
      setStats(statsResponse);
      setDashboard(dashboardResponse);
      setSelectedId(nextSelectedId);
      setCheckoutForm((current) => ({
        ...current,
        bookingId: current.bookingId || normalizedBookings[0]?.id || "",
      }));
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Rental workspace could not be loaded.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadWorkspace();
  }, []);

  useEffect(() => {
    if (!selectedId) {
      setSelectedDetail(null);
      return;
    }

    setDetailLoading(true);
    getRentalById(selectedId)
      .then((detail) => setSelectedDetail(detail))
      .catch((detailError) => {
        setSelectedDetail(null);
        setNotice({
          tone: "error",
          message: detailError instanceof Error ? detailError.message : "Rental detail could not be loaded.",
        });
      })
      .finally(() => setDetailLoading(false));
  }, [selectedId]);

  const filteredRentals = useMemo(() => {
    return rentals.filter((rental) => {
      const matchesStatus = statusFilter === "all" || rental.status === statusFilter;
      const term = search.trim().toLowerCase();
      const matchesSearch =
        !term ||
        [
          rental.bookingNumber,
          rental.customerName,
          rental.vehicleLabel,
          rental.vehiclePlate ?? "",
          rental.pickupBranchName,
          rental.returnBranchName,
        ].some((value) => value.toLowerCase().includes(term));

      return matchesStatus && matchesSearch;
    });
  }, [rentals, search, statusFilter]);

  useEffect(() => {
    if (!filteredRentals.length) {
      return;
    }

    if (!filteredRentals.some((rental) => rental.id === selectedId)) {
      setSelectedId(filteredRentals[0].id);
    }
  }, [filteredRentals, selectedId]);

  const selectedRental = selectedDetail?.rental ?? rentals.find((rental) => rental.id === selectedId) ?? null;
  const activeCount = rentals.filter((rental) => rental.status === "Active").length;
  const overdueCount = rentals.filter((rental) => rental.status === "Overdue").length;
  const completedCount = rentals.filter((rental) => rental.status === "Completed").length;
  const outstandingBalance = rentals.reduce((sum, rental) => sum + (rental.outstandingBalance ?? 0), 0);
  const effectiveActiveCount = stats?.activeRentals ?? activeCount;
  const effectiveOverdueCount = stats?.overdueRentals ?? overdueCount;
  const effectiveOutstandingBalance = stats?.outstandingBalance ?? outstandingBalance;

  function openCheckout() {
    setNotice(null);
    setCheckoutForm({
      bookingId: eligibleBookings[0]?.id ?? "",
      odometerOut: "",
      fuelOut: "Full",
      notes: "",
    });
    setCheckoutOpen(true);
  }

  function openCheckin() {
    if (!selectedRental) {
      return;
    }

    setNotice(null);
    setCheckinForm({
      odometerIn: selectedRental.odometerOut ? String(selectedRental.odometerOut) : "",
      fuelIn: selectedRental.fuelOut || "Full",
      extraCharges: String(selectedRental.extraCharges ?? 0),
      damageNotes: selectedRental.damageNotes ?? "",
    });
    setCheckinOpen(true);
  }

  function openDamageEditor() {
    if (!selectedRental) {
      return;
    }

    setNotice(null);
    setDamageNotesDraft(selectedRental.damageNotes ?? "");
    setDamageOpen(true);
  }

  function handleCheckout() {
    if (!checkoutForm.bookingId || !checkoutForm.odometerOut.trim() || !checkoutForm.fuelOut.trim()) {
      setNotice({ tone: "error", message: "Booking, odometer out, and fuel out are required." });
      return;
    }

    startTransition(async () => {
      try {
        const created = await checkoutRental({
          bookingId: checkoutForm.bookingId,
          odometerOut: Number(checkoutForm.odometerOut),
          fuelOut: checkoutForm.fuelOut,
          notes: checkoutForm.notes,
        });

        setCheckoutOpen(false);
        setNotice({ tone: "success", message: `Rental ${created.bookingNumber} checked out successfully.` });
        await loadWorkspace(created.id);
      } catch (checkoutError) {
        setNotice({
          tone: "error",
          message: checkoutError instanceof Error ? checkoutError.message : "Checkout failed.",
        });
      }
    });
  }

  function handleCheckin() {
    if (!selectedRental) {
      setNotice({ tone: "error", message: "Select a rental to check in." });
      return;
    }

    if (!checkinForm.odometerIn.trim() || !checkinForm.fuelIn.trim()) {
      setNotice({ tone: "error", message: "Odometer in and fuel in are required." });
      return;
    }

    startTransition(async () => {
      try {
        const updated = await checkinRental({
          rentalId: selectedRental.id,
          odometerIn: Number(checkinForm.odometerIn),
          fuelIn: checkinForm.fuelIn,
          extraCharges: Number(checkinForm.extraCharges || 0),
          damageNotes: checkinForm.damageNotes,
        });

        setCheckinOpen(false);
        setNotice({ tone: "success", message: `Rental ${updated.bookingNumber} checked in successfully.` });
        await loadWorkspace(updated.id);
      } catch (checkinError) {
        setNotice({
          tone: "error",
          message: checkinError instanceof Error ? checkinError.message : "Check-in failed.",
        });
      }
    });
  }

  function handleDamageUpdate() {
    if (!selectedRental) {
      setNotice({ tone: "error", message: "Select a rental before editing damage notes." });
      return;
    }

    startTransition(async () => {
      try {
        await updateRentalDamageNotes(selectedRental.id, damageNotesDraft);
        setDamageOpen(false);
        setNotice({ tone: "success", message: `Damage notes updated for ${selectedRental.bookingNumber}.` });
        await loadWorkspace(selectedRental.id);
      } catch (updateError) {
        setNotice({
          tone: "error",
          message: updateError instanceof Error ? updateError.message : "Damage note update failed.",
        });
      }
    });
  }

  return (
    <div className="space-y-6">
      {notice ? (
        <div className={`alert-banner ${notice.tone === "success" ? "alert-banner--success" : "alert-banner--error"}`}>
          {notice.tone === "success" ? <CheckCircle2 className="h-4 w-4 shrink-0" /> : <AlertCircle className="h-4 w-4 shrink-0" />}
          <span className="text-sm font-medium">{notice.message}</span>
        </div>
      ) : null}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard title="Rentals" value={String(rentals.length)} note={`${eligibleBookings.length} bookings ready for checkout`} icon={<ClipboardList className="h-5 w-5" />} />
        <MetricCard title="Active" value={String(effectiveActiveCount)} note={`${stats?.todayCheckouts ?? 0} checked out today`} icon={<CarFront className="h-5 w-5" />} tone="success" />
        <MetricCard title="Overdue" value={String(effectiveOverdueCount)} note={`${dashboard?.todayReturns ?? stats?.todayCheckins ?? 0} returned today`} icon={<TriangleAlert className="h-5 w-5" />} tone="error" />
        <MetricCard title="Outstanding" value={formatCurrency(effectiveOutstandingBalance)} note={`Revenue today ${formatCurrency(stats?.revenueToday ?? 0)}`} icon={<Clock3 className="h-5 w-5" />} tone="info" />
      </div>

      <div className="grid gap-4 xl:grid-cols-3">
        <MetricCard
          title="Average Duration"
          value={`${stats?.averageRentalDurationDays ?? 0} days`}
          note={`Revenue this week ${formatCurrency(stats?.revenueThisWeek ?? 0)}`}
          icon={<Clock3 className="h-5 w-5" />}
        />
        <MetricCard
          title="Today Pickups"
          value={String(dashboard?.todayPickups ?? stats?.todayCheckouts ?? 0)}
          note={`${dashboard?.activeRentals ?? effectiveActiveCount} active in fleet`}
          icon={<CarFront className="h-5 w-5" />}
          tone="success"
        />
        <MetricCard
          title="Today Returns"
          value={String(dashboard?.todayReturns ?? stats?.todayCheckins ?? 0)}
          note={`${completedCount} completed rentals`}
          icon={<CheckCircle2 className="h-5 w-5" />}
          tone="info"
        />
      </div>

      <Card>
        <CardContent className="flex flex-col gap-4 pt-6 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex flex-1 flex-col gap-3 md:flex-row">
            <div className="relative flex-1">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input value={search} onChange={(event) => setSearch(event.target.value)} className="pl-9" placeholder="Search by booking, customer, vehicle, or branch" />
            </div>
            <Select value={statusFilter} onValueChange={(value) => setStatusFilter(value as "all" | RentalStatus)}>
              <SelectTrigger className="w-full md:w-[180px]">
                <SelectValue placeholder="Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All statuses</SelectItem>
                <SelectItem value="Active">Active</SelectItem>
                <SelectItem value="Overdue">Overdue</SelectItem>
                <SelectItem value="Completed">Completed</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="flex gap-2">
            <Button variant="outline" className="gap-2" onClick={() => void loadWorkspace(selectedId)} disabled={loading || pending}>
              <RefreshCw className={`h-4 w-4 ${loading ? "animate-spin" : ""}`} />
              Refresh
            </Button>
            <Button onClick={openCheckout} disabled={!eligibleBookings.length || pending} className="gap-2">
              <CarFront className="h-4 w-4" />
              Start rental
            </Button>
          </div>
        </CardContent>
      </Card>

      {error ? (
        <Card>
          <CardContent className="flex min-h-56 items-center justify-center pt-6">
            <div className="space-y-2 text-center">
              <AlertCircle className="text-error-strong mx-auto h-8 w-8" />
              <div className="text-lg font-semibold">Rental workspace failed to load</div>
              <div className="text-sm text-muted-foreground">{error}</div>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-6 xl:grid-cols-[360px_minmax(0,1fr)]">
          <Card className="min-h-[720px]">
            <CardHeader>
              <CardTitle>Rental queue</CardTitle>
              <CardDescription>{filteredRentals.length} records match the current view.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {loading ? (
                Array.from({ length: 6 }).map((_, index) => (
                  <div key={index} className="h-24 animate-pulse rounded-xl border bg-muted/40" />
                ))
              ) : filteredRentals.length === 0 ? (
                <div className="rounded-xl border border-dashed px-4 py-10 text-center text-sm text-muted-foreground">
                  No rentals match the current filters.
                </div>
              ) : (
                filteredRentals.map((rental) => (
                  <button
                    key={rental.id}
                    type="button"
                    onClick={() => setSelectedId(rental.id)}
                    className={`w-full rounded-[var(--radius-xl)] border p-4 text-left transition ${selectedId === rental.id ? "border-primary bg-primary/5 shadow-soft" : "border-border hover:border-primary/40 hover:bg-accent/40"}`}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <div className="font-semibold">{rental.bookingNumber}</div>
                        <div className="mt-1 text-sm text-muted-foreground">{rental.customerName}</div>
                        <div className="text-sm text-muted-foreground">{rental.vehicleLabel}</div>
                      </div>
                      <Badge variant={statusVariant(rental.status)}>{rental.status}</Badge>
                    </div>
                    <div className="mt-3 grid gap-1 text-xs text-muted-foreground">
                      <div>{formatDate(rental.scheduledPickupAtUtc ?? rental.bookingStartAtUtc ?? rental.createdAtUtc)} to {formatDate(rental.scheduledReturnAtUtc ?? rental.bookingEndAtUtc ?? rental.createdAtUtc)}</div>
                      <div>{rental.pickupBranchName} to {rental.returnBranchName}</div>
                    </div>
                  </button>
                ))
              )}
            </CardContent>
          </Card>

          <Card className="min-h-[720px]">
            <CardHeader className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
              <div>
                <CardTitle>{selectedRental ? selectedRental.bookingNumber : "Rental details"}</CardTitle>
                <CardDescription>
                  {selectedRental ? `${selectedRental.customerName} · ${selectedRental.vehicleLabel}` : "Select a rental from the queue to inspect operations and financials."}
                </CardDescription>
              </div>
              <div className="flex gap-2">
                <Button variant="outline" onClick={openDamageEditor} disabled={!selectedRental || pending} className="gap-2">
                  <PencilLine className="h-4 w-4" />
                  Edit damage
                </Button>
                <Button
                  variant="outline"
                  onClick={openCheckin}
                  disabled={!selectedRental || !["Active", "Overdue"].includes(selectedRental.status) || pending}
                >
                  Check in rental
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              {!selectedRental ? (
                <div className="rounded-xl border border-dashed px-4 py-12 text-center text-sm text-muted-foreground">
                  No rental selected.
                </div>
              ) : detailLoading && !selectedDetail ? (
                <div className="space-y-3">
                  {Array.from({ length: 5 }).map((_, index) => (
                    <div key={index} className="h-20 animate-pulse rounded-xl border bg-muted/40" />
                  ))}
                </div>
              ) : (
                <div className="space-y-6">
                  <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
                    <InfoBlock label="Status" value={<Badge variant={statusVariant(selectedRental.status)}>{selectedRental.status}</Badge>} />
                    <InfoBlock label="Pickup window" value={formatDateTime(selectedRental.scheduledPickupAtUtc ?? selectedRental.bookingStartAtUtc ?? selectedRental.createdAtUtc)} />
                    <InfoBlock label="Return window" value={formatDateTime(selectedRental.scheduledReturnAtUtc ?? selectedRental.bookingEndAtUtc ?? selectedRental.createdAtUtc)} />
                    <InfoBlock label="Outstanding" value={formatCurrency(selectedDetail?.financials.outstandingBalance ?? selectedRental.outstandingBalance ?? 0)} />
                  </div>

                  <div className="grid gap-4 lg:grid-cols-2">
                    <SectionCard title="Operations">
                      <DetailRow label="Customer" value={selectedRental.customerName} />
                      <DetailRow label="Customer phone" value={selectedRental.customerPhone || "Not provided"} />
                      <DetailRow label="Customer email" value={selectedRental.customerEmail || "Not provided"} />
                      <DetailRow label="Vehicle" value={selectedRental.vehicleLabel} />
                      <DetailRow label="Pickup branch" value={selectedRental.pickupBranchName} />
                      <DetailRow label="Return branch" value={selectedRental.returnBranchName} />
                      <DetailRow label="Checked out" value={selectedDetail?.timeline.checkOutAtUtc ? formatDateTime(selectedDetail.timeline.checkOutAtUtc) : "Not checked out"} />
                      <DetailRow label="Checked in" value={selectedDetail?.timeline.checkInAtUtc ? formatDateTime(selectedDetail.timeline.checkInAtUtc) : "Still active"} />
                    </SectionCard>

                    <SectionCard title="Vehicle condition">
                      <DetailRow label="Plate" value={selectedRental.vehiclePlate || "Not available"} />
                      <DetailRow label="VIN" value={selectedRental.vehicleVin || "Not available"} />
                      <DetailRow label="Odometer out" value={`${selectedRental.odometerOut.toLocaleString()} km`} />
                      <DetailRow label="Odometer in" value={selectedRental.odometerIn ? `${selectedRental.odometerIn.toLocaleString()} km` : "Pending"} />
                      <DetailRow label="Fuel out" value={selectedRental.fuelOut} />
                      <DetailRow label="Fuel in" value={selectedRental.fuelIn ?? "Pending"} />
                      <DetailRow label="Distance travelled" value={`${selectedRental.distanceTravelled ?? 0} km`} />
                      <DetailRow label="Damage notes" value={selectedRental.damageNotes || "None recorded"} />
                    </SectionCard>
                  </div>

                  <SectionCard title="Financials">
                    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
                      <InfoBlock label="Quoted total" value={formatCurrency(selectedDetail?.financials.quotedTotal ?? selectedRental.baseAmount ?? selectedRental.finalAmount)} />
                      <InfoBlock label="Extra charges" value={formatCurrency(selectedDetail?.financials.extraCharges ?? selectedRental.extraCharges)} />
                      <InfoBlock label="Final amount" value={formatCurrency(selectedDetail?.financials.finalAmount ?? selectedRental.finalAmount)} />
                      <InfoBlock label="Paid" value={formatCurrency(selectedDetail?.financials.totalPaid ?? selectedRental.totalPaid ?? 0)} />
                      <InfoBlock label="Balance" value={formatCurrency(selectedDetail?.financials.outstandingBalance ?? selectedRental.outstandingBalance ?? 0)} />
                    </div>
                  </SectionCard>

                  <div className="grid gap-4 lg:grid-cols-2">
                    <SectionCard title="Upcoming returns">
                      {dashboard?.upcomingReturns.length ? (
                        dashboard.upcomingReturns.map((rental) => (
                          <DetailRow
                            key={rental.id}
                            label={`${rental.bookingNumber} · ${rental.customerName}`}
                            value={`${formatDateTime(rental.scheduledReturnAtUtc ?? rental.bookingEndAtUtc ?? rental.createdAtUtc)} · ${rental.vehicleLabel}`}
                          />
                        ))
                      ) : (
                        <DetailRow label="Schedule" value="No upcoming returns right now." />
                      )}
                    </SectionCard>

                    <SectionCard title="Recent checkouts">
                      {dashboard?.recentCheckouts.length ? (
                        dashboard.recentCheckouts.map((rental) => (
                          <DetailRow
                            key={rental.id}
                            label={`${rental.bookingNumber} · ${rental.customerName}`}
                            value={`${formatDateTime(rental.checkOutAtUtc ?? rental.createdAtUtc)} · ${rental.vehicleLabel}`}
                          />
                        ))
                      ) : (
                        <DetailRow label="Activity" value="No recent checkouts available." />
                      )}
                    </SectionCard>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      <Dialog open={checkoutOpen} onOpenChange={setCheckoutOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Start rental</DialogTitle>
            <DialogDescription>Create a rental by checking out a confirmed booking that does not already have an active rental.</DialogDescription>
          </DialogHeader>
          <div className="grid gap-4 py-2">
            <div className="grid gap-2">
              <Label htmlFor="bookingId">Confirmed booking</Label>
              <Select value={checkoutForm.bookingId} onValueChange={(value) => setCheckoutForm((current) => ({ ...current, bookingId: value }))}>
                <SelectTrigger id="bookingId">
                  <SelectValue placeholder="Select a booking" />
                </SelectTrigger>
                <SelectContent>
                  {eligibleBookings.map((booking) => (
                    <SelectItem key={booking.id} value={booking.id}>
                      {booking.bookingNumber} · {booking.customerName} · {booking.vehicleLabel}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="odometerOut">Odometer out</Label>
              <Input id="odometerOut" inputMode="numeric" value={checkoutForm.odometerOut} onChange={(event) => setCheckoutForm((current) => ({ ...current, odometerOut: event.target.value }))} placeholder="e.g. 25481" />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="fuelOut">Fuel out</Label>
              <Select value={checkoutForm.fuelOut} onValueChange={(value) => setCheckoutForm((current) => ({ ...current, fuelOut: value }))}>
                <SelectTrigger id="fuelOut">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {checkoutFuelOptions.map((option) => (
                    <SelectItem key={option} value={option}>{option}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="checkoutNotes">Notes</Label>
              <Textarea id="checkoutNotes" value={checkoutForm.notes} onChange={(event) => setCheckoutForm((current) => ({ ...current, notes: event.target.value }))} placeholder="Condition notes, accessories issued, visible damage, etc." />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCheckoutOpen(false)}>Cancel</Button>
            <Button onClick={handleCheckout} disabled={pending || !eligibleBookings.length}>Start rental</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Sheet open={checkinOpen} onOpenChange={setCheckinOpen}>
        <SheetContent className="w-[420px] sm:max-w-[460px]">
          <SheetHeader>
            <SheetTitle>Check in rental</SheetTitle>
            <SheetDescription>Close the selected rental, update vehicle condition, and finalize extra charges.</SheetDescription>
          </SheetHeader>
          <div className="grid gap-4 py-6">
            <div className="grid gap-2">
              <Label htmlFor="odometerIn">Odometer in</Label>
              <Input id="odometerIn" inputMode="numeric" value={checkinForm.odometerIn} onChange={(event) => setCheckinForm((current) => ({ ...current, odometerIn: event.target.value }))} />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="fuelIn">Fuel in</Label>
              <Select value={checkinForm.fuelIn} onValueChange={(value) => setCheckinForm((current) => ({ ...current, fuelIn: value }))}>
                <SelectTrigger id="fuelIn">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {checkoutFuelOptions.map((option) => (
                    <SelectItem key={option} value={option}>{option}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="extraCharges">Extra charges</Label>
              <Input id="extraCharges" inputMode="decimal" value={checkinForm.extraCharges} onChange={(event) => setCheckinForm((current) => ({ ...current, extraCharges: event.target.value }))} />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="damageNotes">Damage notes</Label>
              <Textarea id="damageNotes" value={checkinForm.damageNotes} onChange={(event) => setCheckinForm((current) => ({ ...current, damageNotes: event.target.value }))} placeholder="Document new damage, fuel discrepancy, or return comments." />
            </div>
          </div>
          <SheetFooter>
            <Button variant="outline" onClick={() => setCheckinOpen(false)}>Cancel</Button>
            <Button onClick={handleCheckin} disabled={pending || !selectedRental}>Complete check-in</Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>

      <Sheet open={damageOpen} onOpenChange={setDamageOpen}>
        <SheetContent className="w-[420px] sm:max-w-[460px]">
          <SheetHeader>
            <SheetTitle>Edit damage notes</SheetTitle>
            <SheetDescription>Keep vehicle condition and incident notes current throughout the rental lifecycle.</SheetDescription>
          </SheetHeader>
          <div className="grid gap-4 py-6">
            <div className="grid gap-2">
              <Label htmlFor="damageNotesOnly">Damage notes</Label>
              <Textarea
                id="damageNotesOnly"
                value={damageNotesDraft}
                onChange={(event) => setDamageNotesDraft(event.target.value)}
                placeholder="Document scratches, dents, missing accessories, fuel issues, or customer remarks."
              />
            </div>
          </div>
          <SheetFooter>
            <Button variant="outline" onClick={() => setDamageOpen(false)}>Cancel</Button>
            <Button onClick={handleDamageUpdate} disabled={pending || !selectedRental}>Save notes</Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>
    </div>
  );
}

function MetricCard({
  title,
  value,
  note,
  icon,
  tone = "default",
}: {
  title: string;
  value: string;
  note: string;
  icon: ReactNode;
  tone?: "default" | "success" | "error" | "info";
}) {
  const toneClass =
    tone === "success"
      ? "metric-card--success"
      : tone === "error"
        ? "metric-card--error"
        : tone === "info"
          ? "metric-card--info"
          : "metric-card--default";

  const iconToneClass =
    tone === "success"
      ? "metric-icon--success"
      : tone === "error"
        ? "metric-icon--error"
        : tone === "info"
          ? "metric-icon--info"
          : "metric-icon--default";

  return (
    <Card className={`metric-card ${toneClass}`}>
      <CardContent className="pt-6">
        <div className="flex items-start justify-between gap-4">
          <div>
            <div className="text-sm font-medium text-muted-foreground">{title}</div>
            <div className="mt-1 text-3xl font-bold">{value}</div>
            <div className="mt-1 text-xs text-muted-foreground">{note}</div>
          </div>
          <div className={`metric-icon ${iconToneClass}`}>{icon}</div>
        </div>
      </CardContent>
    </Card>
  );
}

function SectionCard({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="rounded-[var(--radius-2xl)] border bg-card/60 p-4">
      <div className="mb-4 text-sm font-semibold uppercase tracking-[0.18em] text-muted-foreground">{title}</div>
      <div className="space-y-3">{children}</div>
    </div>
  );
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="surface-subtle grid gap-1 px-3 py-2">
      <div className="text-xs uppercase tracking-[0.16em] text-muted-foreground">{label}</div>
      <div className="text-sm font-medium">{value}</div>
    </div>
  );
}

function InfoBlock({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="rounded-[var(--radius-lg)] border bg-background/70 px-4 py-3">
      <div className="text-xs uppercase tracking-[0.16em] text-muted-foreground">{label}</div>
      <div className="mt-2 text-sm font-semibold">{value}</div>
    </div>
  );
}

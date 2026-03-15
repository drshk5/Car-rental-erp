"use client";

import { useDeferredValue, useEffect, useMemo, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import {
  createCustomerAction,
  setCustomerStatusAction,
  setCustomerVerificationAction,
  updateCustomerAction,
  type CustomerActionResult,
} from "@/app/customers/actions";
import { formatCurrency, formatDate, formatDateTime } from "@/lib/format";
import { cn, getInitials, getVerificationColor, getVerificationLabel, getBookingStatusLabel, getBookingStatusColor } from "@/lib/utils";
import type { Customer, CustomerDetail, CustomerFormPayload } from "@/types/customers";

// UI Components
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";

// Icons
import {
  Search,
  Plus,
  Filter,
  Users,
  Shield,
  Car,
  DollarSign,
  Phone,
  Mail,
  MapPin,
  Calendar,
  FileText,
  AlertTriangle,
  MoreVertical,
  Edit,
  Trash2,
  Eye,
  RefreshCw,
  CheckCircle,
  XCircle,
  Clock,
  Building,
  User,
  ChevronRight,
  GripVertical,
  X,
  Save,
  UserPlus,
  Activity,
  CreditCard,
  History,
  AlertCircle,
} from "lucide-react";

const verificationOptions = [
  { value: "Pending", label: "Pending", icon: Clock, color: "warning" },
  { value: "Verified", label: "Verified", icon: CheckCircle, color: "success" },
  { value: "Rejected", label: "Rejected", icon: XCircle, color: "error" },
] as const;

const emptyForm: CustomerFormPayload = {
  fullName: "",
  phone: "",
  alternatePhone: "",
  email: "",
  address: "",
  city: "",
  state: "",
  postalCode: "",
  dateOfBirth: "",
  nationality: "Indian",
  licenseNumber: "",
  licenseExpiry: "",
  identityDocumentType: "Passport",
  identityDocumentNumber: "",
  emergencyContactName: "",
  emergencyContactPhone: "",
  notes: "",
  riskNotes: "",
};

function toForm(customer?: Customer): CustomerFormPayload {
  if (!customer) {
    return emptyForm;
  }

  return {
    fullName: customer.fullName,
    phone: customer.phone,
    alternatePhone: customer.alternatePhone,
    email: customer.email,
    address: customer.address,
    city: customer.city,
    state: customer.state,
    postalCode: customer.postalCode,
    dateOfBirth: customer.dateOfBirth ?? "",
    nationality: customer.nationality,
    licenseNumber: customer.licenseNumber,
    licenseExpiry: customer.licenseExpiry ?? "",
    identityDocumentType: customer.identityDocumentType,
    identityDocumentNumber: customer.identityDocumentNumber,
    emergencyContactName: customer.emergencyContactName,
    emergencyContactPhone: customer.emergencyContactPhone,
    notes: customer.notes,
    riskNotes: customer.riskNotes,
  };
}

export function CustomerWorkspace({
  customers,
  details,
}: {
  customers: Customer[];
  details: CustomerDetail[];
}) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const normalizedDetails = useMemo(
    () => details.filter((detail): detail is CustomerDetail => Boolean(detail?.profile?.id)),
    [details],
  );
  const detailById = useMemo(() => new Map(normalizedDetails.map((detail) => [detail.profile.id, detail])), [normalizedDetails]);
  const [selectedId, setSelectedId] = useState(customers[0]?.id ?? "");
  const [editorMode, setEditorMode] = useState<"create" | "edit">("create");
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<"all" | "active" | "archived">("all");
  const [verificationFilter, setVerificationFilter] = useState<"all" | "Pending" | "Verified" | "Rejected">("all");
  const [activeRentalOnly, setActiveRentalOnly] = useState(false);
  const [balanceOnly, setBalanceOnly] = useState(false);
  const [form, setForm] = useState<CustomerFormPayload>(emptyForm);
  const [notice, setNotice] = useState<CustomerActionResult | null>(null);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [isEditSheetOpen, setIsEditSheetOpen] = useState(false);

  const deferredSearch = useDeferredValue(search);
  const filteredCustomers = customers.filter((customer) => {
    const matchesSearch =
      !deferredSearch ||
      [
        customer.customerCode,
        customer.fullName,
        customer.phone,
        customer.email,
        customer.licenseNumber,
        customer.identityDocumentNumber,
      ].some((value) => value.toLowerCase().includes(deferredSearch.toLowerCase()));

    const matchesStatus =
      statusFilter === "all" ||
      (statusFilter === "active" ? customer.isActive : !customer.isActive);

    const matchesVerification =
      verificationFilter === "all" ||
      getVerificationLabel(customer.verificationStatus) === verificationFilter;

    const matchesActiveRental = !activeRentalOnly || customer.hasActiveRental;
    const matchesBalance = !balanceOnly || customer.outstandingBalance > 0;

    return matchesSearch && matchesStatus && matchesVerification && matchesActiveRental && matchesBalance;
  });

  useEffect(() => {
    if (filteredCustomers.length === 0) {
      return;
    }

    if (!filteredCustomers.some((customer) => customer.id === selectedId)) {
      setSelectedId(filteredCustomers[0].id);
    }
  }, [filteredCustomers, selectedId]);

  const selectedDetail = detailById.get(selectedId) ?? null;
  const selectedCustomer = selectedDetail?.profile ?? null;

  const stats = {
    total: customers.length,
    active: customers.filter((customer) => customer.isActive).length,
    verified: customers.filter((customer) => getVerificationLabel(customer.verificationStatus) === "Verified").length,
    exposure: customers.reduce((sum, customer) => sum + customer.outstandingBalance, 0),
  };

  function openCreate() {
    setEditorMode("create");
    setForm(emptyForm);
    setNotice(null);
    setIsCreateDialogOpen(true);
  }

  function openEdit() {
    if (!selectedCustomer) {
      return;
    }

    setEditorMode("edit");
    setForm(toForm(selectedCustomer));
    setNotice(null);
    setIsEditSheetOpen(true);
  }

  function handleSubmit() {
    startTransition(async () => {
      const result =
        editorMode === "create"
          ? await createCustomerAction(form)
          : selectedCustomer
            ? await updateCustomerAction(selectedCustomer.id, form)
            : { ok: false, message: "Select a customer to edit." as const };

      setNotice(result);
      if (result.ok) {
        router.refresh();
        setIsCreateDialogOpen(false);
        setIsEditSheetOpen(false);
      }
    });
  }

  function handleVerificationChange(value: string) {
    if (!selectedCustomer) {
      return;
    }

    startTransition(async () => {
      const result = await setCustomerVerificationAction(selectedCustomer.id, value);
      setNotice(result);
      if (result.ok) {
        router.refresh();
      }
    });
  }

  function handleStatusToggle() {
    if (!selectedCustomer) {
      return;
    }

    startTransition(async () => {
      const result = await setCustomerStatusAction(selectedCustomer.id, !selectedCustomer.isActive);
      setNotice(result);
      if (result.ok) {
        router.refresh();
      }
    });
  }

  return (
    <div className="space-y-6">
      {/* Notification Banner */}
      {notice && (
        <div
          className={cn(
            "alert-banner animate-in slide-in-from-top-2",
            notice.ok ? "alert-banner--success" : "alert-banner--error",
          )}
        >
          {notice.ok ? <CheckCircle className="h-5 w-5 shrink-0" /> : <AlertCircle className="h-5 w-5 shrink-0" />}
          <span className="text-sm font-medium">{notice.message}</span>
        </div>
      )}

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="card-hover metric-card metric-card--default">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Total Customers</p>
                <p className="text-3xl font-bold mt-1">{stats.total}</p>
                <p className="text-xs text-muted-foreground mt-1">{stats.active} active profiles</p>
              </div>
              <div className="metric-icon metric-icon--default">
                <Users className="h-6 w-6" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="card-hover metric-card metric-card--success">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">KYC Verified</p>
                <p className="text-3xl font-bold mt-1">{stats.verified}</p>
                <p className="text-xs text-muted-foreground mt-1">{stats.total - stats.verified} pending review</p>
              </div>
              <div className="metric-icon metric-icon--success">
                <Shield className="h-6 w-6" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="card-hover metric-card metric-card--warning">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Active Rentals</p>
                <p className="text-3xl font-bold mt-1">{customers.filter(c => c.hasActiveRental).length}</p>
                <p className="text-xs text-muted-foreground mt-1">vehicles in use</p>
              </div>
              <div className="metric-icon metric-icon--warning">
                <Car className="h-6 w-6" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="card-hover metric-card metric-card--error">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Outstanding</p>
                <p className="text-3xl font-bold mt-1">{formatCurrency(stats.exposure)}</p>
                <p className="text-xs text-muted-foreground mt-1">receivable exposure</p>
              </div>
              <div className="metric-icon metric-icon--error">
                <DollarSign className="h-6 w-6" />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Toolbar */}
      <Card>
        <CardContent className="p-4">
          <div className="flex flex-col lg:flex-row gap-4">
            {/* Search */}
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search by name, code, phone, license..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-9"
              />
            </div>

            {/* Filters */}
            <div className="flex flex-wrap items-center gap-3">
              <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v as typeof statusFilter)}>
                <SelectTrigger className="w-[150px]">
                  <SelectValue placeholder="Status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Profiles</SelectItem>
                  <SelectItem value="active">Active Only</SelectItem>
                  <SelectItem value="archived">Archived</SelectItem>
                </SelectContent>
              </Select>

              <Select value={verificationFilter} onValueChange={(v) => setVerificationFilter(v as typeof verificationFilter)}>
                <SelectTrigger className="w-[150px]">
                  <SelectValue placeholder="Verification" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Verifications</SelectItem>
                  <SelectItem value="Pending">Pending</SelectItem>
                  <SelectItem value="Verified">Verified</SelectItem>
                  <SelectItem value="Rejected">Rejected</SelectItem>
                </SelectContent>
              </Select>

              <div className="flex items-center gap-2">
                <Checkbox
                  id="activeRental"
                  checked={activeRentalOnly}
                  onCheckedChange={(v) => setActiveRentalOnly(!!v)}
                />
                <Label htmlFor="activeRental" className="text-sm cursor-pointer">Active Rental</Label>
              </div>

              <div className="flex items-center gap-2">
                <Checkbox
                  id="balance"
                  checked={balanceOnly}
                  onCheckedChange={(v) => setBalanceOnly(!!v)}
                />
                <Label htmlFor="balance" className="text-sm cursor-pointer">Outstanding</Label>
              </div>

              <Button onClick={openCreate} className="gap-2">
                <Plus className="h-4 w-4" />
                New Customer
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Main Content */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Customer List */}
        <Card className="lg:col-span-4">
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-base">Customer Roster</CardTitle>
                <CardDescription>{filteredCustomers.length} customers found</CardDescription>
              </div>
              <Filter className="h-4 w-4 text-muted-foreground" />
            </div>
          </CardHeader>
          <CardContent className="p-0">
            <ScrollArea className="h-[600px]">
              <div className="space-y-2 p-4 pt-0">
                {filteredCustomers.length === 0 ? (
                  <div className="flex flex-col items-center justify-center py-12 text-center">
                    <Users className="h-12 w-12 text-muted-foreground/50 mb-3" />
                    <p className="text-sm text-muted-foreground">No customers match your filters</p>
                  </div>
                ) : (
                  filteredCustomers.map((customer) => (
                    <div
                      key={customer.id}
                      onClick={() => setSelectedId(customer.id)}
                      className={cn(
                        "group relative flex items-start gap-3 p-4 rounded-xl cursor-pointer transition-all duration-200 border",
                        customer.id === selectedId
                          ? "bg-primary/5 border-primary/30 shadow-sm"
                          : "hover:bg-muted/50 border-transparent hover:border-border"
                      )}
                    >
                      <div className="hidden group-hover:flex absolute right-3 top-3">
                        <Button size="icon" variant="ghost" className="h-7 w-7">
                          <ChevronRight className="h-4 w-4" />
                        </Button>
                      </div>

                      <Avatar className="h-10 w-10 shrink-0">
                        <AvatarFallback className={cn(
                          "text-sm font-semibold",
                          customer.isActive ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"
                        )}>
                          {getInitials(customer.fullName)}
                        </AvatarFallback>
                      </Avatar>

                      <div className="flex-1 min-w-0 space-y-1">
                        <div className="flex items-center gap-2">
                          <span className="font-semibold text-sm truncate">{customer.fullName}</span>
                          <Badge variant={customer.isActive ? "success" : "secondary"} className="text-[10px] px-1.5 py-0">
                            {customer.isActive ? "Active" : "Archived"}
                          </Badge>
                        </div>
                        <p className="text-xs text-muted-foreground">{customer.customerCode}</p>
                        <div className="flex items-center gap-2 text-xs text-muted-foreground">
                          <Phone className="h-3 w-3" />
                          <span>{customer.phone}</span>
                        </div>
                        <div className="flex items-center gap-2 text-xs text-muted-foreground">
                          <MapPin className="h-3 w-3" />
                          <span>{customer.city}, {customer.state}</span>
                        </div>
                        <div className="flex items-center gap-2 pt-1">
                          <Badge
                            variant={getVerificationColor(customer.verificationStatus) as any}
                            className="text-[10px] px-1.5 py-0"
                          >
                            {getVerificationLabel(customer.verificationStatus)}
                          </Badge>
                          {customer.hasActiveRental && (
                            <Badge variant="warning" className="text-[10px] px-1.5 py-0">
                              On Rent
                            </Badge>
                          )}
                          {customer.outstandingBalance > 0 && (
                            <Badge variant="error" className="text-[10px] px-1.5 py-0">
                              {formatCurrency(customer.outstandingBalance)} due
                            </Badge>
                          )}
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </ScrollArea>
          </CardContent>
        </Card>

        {/* Customer Detail */}
        <Card className="lg:col-span-5">
          {selectedDetail ? (
            <ScrollArea className="h-[600px]">
              <div className="p-6 space-y-6">
                {/* Profile Header */}
                <div className="flex items-start gap-4">
                  <Avatar className="h-16 w-16">
                    <AvatarFallback className="text-xl font-bold bg-primary/10 text-primary">
                      {getInitials(selectedCustomer?.fullName ?? "")}
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <h2 className="text-xl font-bold">{selectedCustomer?.fullName}</h2>
                      <Badge variant={selectedCustomer?.isActive ? "success" : "secondary"}>
                        {selectedCustomer?.isActive ? "Active" : "Archived"}
                      </Badge>
                    </div>
                    <p className="text-sm text-muted-foreground">{selectedCustomer?.customerCode}</p>
                    <div className="flex items-center gap-2 mt-1">
                      <MapPin className="h-3 w-3 text-muted-foreground" />
                      <span className="text-sm text-muted-foreground">{selectedCustomer?.city}, {selectedCustomer?.state}</span>
                    </div>
                  </div>
                  <Button variant="outline" size="sm" onClick={openEdit} className="gap-2">
                    <Edit className="h-4 w-4" />
                    Edit
                  </Button>
                </div>

                {/* Metrics */}
                <div className="grid grid-cols-2 gap-3">
                  <div className="surface-muted p-4 space-y-1">
                    <p className="text-xs text-muted-foreground">Lifetime Value</p>
                    <p className="text-xl font-bold">{formatCurrency(selectedDetail.profile.lifetimeValue)}</p>
                  </div>
                  <div className="surface-muted p-4 space-y-1">
                    <p className="text-xs text-muted-foreground">Total Bookings</p>
                    <p className="text-xl font-bold">{selectedDetail.profile.totalBookings}</p>
                  </div>
                  <div className="surface-muted p-4 space-y-1">
                    <p className="text-xs text-muted-foreground">Completed</p>
                    <p className="text-xl font-bold">{selectedDetail.profile.completedRentals}</p>
                  </div>
                  <div className="surface-muted p-4 space-y-1">
                    <p className="text-xs text-muted-foreground">Outstanding</p>
                    <p className="text-xl font-bold text-warning-strong">{formatCurrency(selectedDetail.profile.outstandingBalance)}</p>
                  </div>
                </div>

                {/* Tabs */}
                <Tabs defaultValue="contact" className="w-full">
                  <TabsList className="grid w-full grid-cols-3">
                    <TabsTrigger value="contact" className="gap-1">
                      <User className="h-3 w-3" /> Contact
                    </TabsTrigger>
                    <TabsTrigger value="rental" className="gap-1">
                      <Car className="h-3 w-3" /> Rental
                    </TabsTrigger>
                    <TabsTrigger value="bookings" className="gap-1">
                      <History className="h-3 w-3" /> History
                    </TabsTrigger>
                  </TabsList>

                  <TabsContent value="contact" className="space-y-4 mt-4">
                    <div className="grid gap-4">
                      <div className="flex items-center gap-3 p-3 rounded-lg bg-muted/30">
                        <Phone className="h-4 w-4 text-muted-foreground" />
                        <div>
                          <p className="text-xs text-muted-foreground">Phone</p>
                          <p className="text-sm font-medium">{selectedDetail.profile.phone}</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-3 p-3 rounded-lg bg-muted/30">
                        <Phone className="h-4 w-4 text-muted-foreground" />
                        <div>
                          <p className="text-xs text-muted-foreground">Alternate Phone</p>
                          <p className="text-sm font-medium">{selectedDetail.profile.alternatePhone || "Not recorded"}</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-3 p-3 rounded-lg bg-muted/30">
                        <Mail className="h-4 w-4 text-muted-foreground" />
                        <div>
                          <p className="text-xs text-muted-foreground">Email</p>
                          <p className="text-sm font-medium">{selectedDetail.profile.email}</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-3 p-3 rounded-lg bg-muted/30">
                        <MapPin className="h-4 w-4 text-muted-foreground" />
                        <div>
                          <p className="text-xs text-muted-foreground">Address</p>
                          <p className="text-sm font-medium">{selectedDetail.profile.address}, {selectedDetail.profile.city}, {selectedDetail.profile.state} {selectedDetail.profile.postalCode}</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-3 p-3 rounded-lg bg-muted/30">
                        <Calendar className="h-4 w-4 text-muted-foreground" />
                        <div>
                          <p className="text-xs text-muted-foreground">Date of Birth</p>
                          <p className="text-sm font-medium">{selectedDetail.profile.dateOfBirth ? formatDate(selectedDetail.profile.dateOfBirth) : "Not recorded"}</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-3 p-3 rounded-lg bg-muted/30">
                        <FileText className="h-4 w-4 text-muted-foreground" />
                        <div>
                          <p className="text-xs text-muted-foreground">ID Document</p>
                          <p className="text-sm font-medium">{selectedDetail.profile.identityDocumentType} - {selectedDetail.profile.identityDocumentNumber}</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-3 p-3 rounded-lg bg-muted/30">
                        <AlertTriangle className="h-4 w-4 text-muted-foreground" />
                        <div>
                          <p className="text-xs text-muted-foreground">Emergency Contact</p>
                          <p className="text-sm font-medium">{selectedDetail.profile.emergencyContactName} - {selectedDetail.profile.emergencyContactPhone}</p>
                        </div>
                      </div>
                    </div>
                  </TabsContent>

                  <TabsContent value="rental" className="space-y-4 mt-4">
                    {selectedDetail.activeRental ? (
                      <div className="space-y-3">
                        <div className="surface-warning p-4">
                          <div className="flex items-center gap-2 mb-3">
                            <Car className="h-5 w-5 text-warning-strong" />
                            <span className="font-semibold">Active Rental</span>
                            <Badge variant="warning">In Progress</Badge>
                          </div>
                          <div className="space-y-2">
                            <div className="flex justify-between">
                              <span className="text-sm text-muted-foreground">Vehicle</span>
                              <span className="text-sm font-medium">{selectedDetail.activeRental.vehicleLabel}</span>
                            </div>
                            <div className="flex justify-between">
                              <span className="text-sm text-muted-foreground">Booking</span>
                              <span className="text-sm font-medium">{selectedDetail.activeRental.bookingNumber}</span>
                            </div>
                            <div className="flex justify-between">
                              <span className="text-sm text-muted-foreground">Checked Out</span>
                              <span className="text-sm font-medium">
                                {selectedDetail.activeRental.checkOutAtUtc ? formatDateTime(selectedDetail.activeRental.checkOutAtUtc) : "N/A"}
                              </span>
                            </div>
                            <div className="flex justify-between">
                              <span className="text-sm text-muted-foreground">Fuel Out</span>
                              <span className="text-sm font-medium">{selectedDetail.activeRental.fuelOut}</span>
                            </div>
                            <div className="flex justify-between">
                              <span className="text-sm text-muted-foreground">Odometer</span>
                              <span className="text-sm font-medium">{selectedDetail.activeRental.odometerOut} km</span>
                            </div>
                            <div className="flex justify-between">
                              <span className="text-sm text-muted-foreground">Current Bill</span>
                              <span className="text-sm font-bold text-warning-strong">{formatCurrency(selectedDetail.activeRental.finalAmount)}</span>
                            </div>
                            {selectedDetail.activeRental.damageNotes && (
                              <div className="pt-2 border-t">
                                <p className="text-xs text-muted-foreground mb-1">Damage Notes</p>
                                <p className="text-sm">{selectedDetail.activeRental.damageNotes}</p>
                              </div>
                            )}
                          </div>
                        </div>
                      </div>
                    ) : (
                      <div className="flex flex-col items-center justify-center py-12 text-center">
                        <Car className="h-12 w-12 text-muted-foreground/50 mb-3" />
                        <p className="text-sm text-muted-foreground">No active rental</p>
                        <p className="text-xs text-muted-foreground">This customer currently has no active rental</p>
                      </div>
                    )}

                    <div className="surface-subtle p-4 space-y-3">
                      <h4 className="font-semibold text-sm">License Information</h4>
                      <div className="flex justify-between">
                        <span className="text-sm text-muted-foreground">License Number</span>
                        <span className="text-sm font-medium">{selectedDetail.profile.licenseNumber}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-sm text-muted-foreground">License Expiry</span>
                        <span className="text-sm font-medium">
                          {selectedDetail.profile.licenseExpiry ? formatDate(selectedDetail.profile.licenseExpiry) : "Missing"}
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-sm text-muted-foreground">Last Booking</span>
                        <span className="text-sm font-medium">
                          {selectedDetail.profile.lastBookingAtUtc ? formatDateTime(selectedDetail.profile.lastBookingAtUtc) : "No bookings yet"}
                        </span>
                      </div>
                    </div>
                  </TabsContent>

                  <TabsContent value="bookings" className="space-y-4 mt-4">
                    {selectedDetail.recentBookings.length === 0 ? (
                      <div className="flex flex-col items-center justify-center py-12 text-center">
                        <History className="h-12 w-12 text-muted-foreground/50 mb-3" />
                        <p className="text-sm text-muted-foreground">No booking history</p>
                        <p className="text-xs text-muted-foreground">This customer has no past bookings</p>
                      </div>
                    ) : (
                      <div className="space-y-3">
                        {selectedDetail.recentBookings.map((booking) => (
                          <div
                            key={booking.bookingId}
                            className="surface-subtle p-4 space-y-2"
                          >
                            <div className="flex items-center justify-between">
                              <span className="font-semibold text-sm">{booking.bookingNumber}</span>
                              <Badge variant={getBookingStatusColor(booking.status) as any}>
                                {getBookingStatusLabel(booking.status)}
                              </Badge>
                            </div>
                            <p className="text-sm">{booking.vehicleLabel}</p>
                            <p className="text-xs text-muted-foreground">
                              {booking.pickupBranchName} → {booking.returnBranchName}
                            </p>
                            <p className="text-xs text-muted-foreground">
                              {formatDateTime(booking.startAtUtc)} - {formatDateTime(booking.endAtUtc)}
                            </p>
                            <div className="flex gap-4 pt-2 text-xs">
                              <div>
                                <span className="text-muted-foreground">Quoted: </span>
                                <span className="font-medium">{formatCurrency(booking.quotedTotal)}</span>
                              </div>
                              <div>
                                <span className="text-muted-foreground">Paid: </span>
                                <span className="font-medium text-success-strong">{formatCurrency(booking.totalPaid)}</span>
                              </div>
                              <div>
                                <span className="text-muted-foreground">Due: </span>
                                <span className="font-medium text-warning-strong">{formatCurrency(booking.outstandingBalance)}</span>
                              </div>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </TabsContent>
                </Tabs>
              </div>
            </ScrollArea>
          ) : (
            <div className="flex flex-col items-center justify-center h-[600px] text-center p-6">
              <Users className="h-16 w-16 text-muted-foreground/30 mb-4" />
              <h3 className="text-lg font-semibold mb-2">No Customer Selected</h3>
              <p className="text-sm text-muted-foreground max-w-xs">
                Select a customer from the roster to view their full profile and booking history.
              </p>
            </div>
          )}
        </Card>

        {/* Quick Actions */}
        <Card className="lg:col-span-3">
          <CardHeader>
            <CardTitle className="text-base">Quick Actions</CardTitle>
            <CardDescription>Manage selected customer</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {selectedCustomer && (
              <>
                <div className="space-y-2">
                  <Label className="text-xs text-muted-foreground">Verification Status</Label>
                  <div className="flex flex-wrap gap-2">
                    {verificationOptions.map((option) => (
                      <Button
                        key={option.value}
                        variant={getVerificationLabel(selectedCustomer.verificationStatus) === option.label ? option.color as any : "outline"}
                        size="sm"
                        onClick={() => handleVerificationChange(option.value)}
                        disabled={pending}
                        className="gap-1"
                      >
                        <option.icon className="h-3 w-3" />
                        {option.label}
                      </Button>
                    ))}
                  </div>
                </div>

                <div className="space-y-2">
                  <Label className="text-xs text-muted-foreground">Profile Status</Label>
                  <Button
                    variant={selectedCustomer.isActive ? "destructive" : "success"}
                    className="w-full gap-2"
                    onClick={handleStatusToggle}
                    disabled={pending}
                  >
                    {selectedCustomer.isActive ? (
                      <>
                        <XCircle className="h-4 w-4" />
                        Archive Profile
                      </>
                    ) : (
                      <>
                        <CheckCircle className="h-4 w-4" />
                        Reactivate Profile
                      </>
                    )}
                  </Button>
                </div>

                <div className="pt-4 border-t space-y-2">
                  <Label className="text-xs text-muted-foreground">Notes</Label>
                  {selectedCustomer.notes && (
                    <div className="surface-muted p-3">
                      <p className="text-xs text-muted-foreground mb-1">Operations Notes</p>
                      <p className="text-sm">{selectedCustomer.notes}</p>
                    </div>
                  )}
                  {selectedCustomer.riskNotes && (
                    <div className="surface-warning p-3">
                      <p className="mb-1 text-xs text-warning-strong">Risk Notes</p>
                      <p className="text-sm">{selectedCustomer.riskNotes}</p>
                    </div>
                  )}
                </div>
              </>
            )}

            {!selectedCustomer && (
              <div className="flex flex-col items-center justify-center py-8 text-center">
                <UserPlus className="h-12 w-12 text-muted-foreground/30 mb-3" />
                <p className="text-sm text-muted-foreground">Select a customer to see actions</p>
              </div>
            )}

            <div className="pt-4 border-t">
              <Button variant="outline" className="w-full gap-2" onClick={openCreate}>
                <UserPlus className="h-4 w-4" />
                Add New Customer
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Create Customer Dialog */}
      <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <UserPlus className="h-5 w-5" />
              Create New Customer
            </DialogTitle>
            <DialogDescription>
              Fill in the customer details to register a new renter in the system.
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="fullName">Full Name *</Label>
                <Input
                  id="fullName"
                  value={form.fullName}
                  onChange={(e) => setForm({ ...form, fullName: e.target.value })}
                  placeholder="Enter full name"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  value={form.email}
                  onChange={(e) => setForm({ ...form, email: e.target.value })}
                  placeholder="email@example.com"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="phone">Phone *</Label>
                <Input
                  id="phone"
                  value={form.phone}
                  onChange={(e) => setForm({ ...form, phone: e.target.value })}
                  placeholder="+91 XXXXX XXXXX"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="alternatePhone">Alternate Phone</Label>
                <Input
                  id="alternatePhone"
                  value={form.alternatePhone}
                  onChange={(e) => setForm({ ...form, alternatePhone: e.target.value })}
                  placeholder="+91 XXXXX XXXXX"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="address">Address</Label>
              <Input
                id="address"
                value={form.address}
                onChange={(e) => setForm({ ...form, address: e.target.value })}
                placeholder="Street address"
              />
            </div>

            <div className="grid grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label htmlFor="city">City</Label>
                <Input
                  id="city"
                  value={form.city}
                  onChange={(e) => setForm({ ...form, city: e.target.value })}
                  placeholder="City"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="state">State</Label>
                <Input
                  id="state"
                  value={form.state}
                  onChange={(e) => setForm({ ...form, state: e.target.value })}
                  placeholder="State"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="postalCode">Postal Code</Label>
                <Input
                  id="postalCode"
                  value={form.postalCode}
                  onChange={(e) => setForm({ ...form, postalCode: e.target.value })}
                  placeholder="XXXXXX"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="dateOfBirth">Date of Birth</Label>
                <Input
                  id="dateOfBirth"
                  type="date"
                  value={form.dateOfBirth}
                  onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="nationality">Nationality</Label>
                <Input
                  id="nationality"
                  value={form.nationality}
                  onChange={(e) => setForm({ ...form, nationality: e.target.value })}
                  placeholder="Indian"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="licenseNumber">License Number</Label>
                <Input
                  id="licenseNumber"
                  value={form.licenseNumber}
                  onChange={(e) => setForm({ ...form, licenseNumber: e.target.value })}
                  placeholder="DL number"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="licenseExpiry">License Expiry</Label>
                <Input
                  id="licenseExpiry"
                  type="date"
                  value={form.licenseExpiry}
                  onChange={(e) => setForm({ ...form, licenseExpiry: e.target.value })}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="identityDocumentType">ID Document Type</Label>
                <Select
                  value={form.identityDocumentType}
                  onValueChange={(v) => setForm({ ...form, identityDocumentType: v })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Passport">Passport</SelectItem>
                    <SelectItem value="Aadhar">Aadhar</SelectItem>
                    <SelectItem value="Driving License">Driving License</SelectItem>
                    <SelectItem value="Voter ID">Voter ID</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="identityDocumentNumber">ID Number</Label>
                <Input
                  id="identityDocumentNumber"
                  value={form.identityDocumentNumber}
                  onChange={(e) => setForm({ ...form, identityDocumentNumber: e.target.value })}
                  placeholder="Document number"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="emergencyContactName">Emergency Contact Name</Label>
                <Input
                  id="emergencyContactName"
                  value={form.emergencyContactName}
                  onChange={(e) => setForm({ ...form, emergencyContactName: e.target.value })}
                  placeholder="Contact name"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="emergencyContactPhone">Emergency Contact Phone</Label>
                <Input
                  id="emergencyContactPhone"
                  value={form.emergencyContactPhone}
                  onChange={(e) => setForm({ ...form, emergencyContactPhone: e.target.value })}
                  placeholder="+91 XXXXX XXXXX"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="notes">Operations Notes</Label>
              <Textarea
                id="notes"
                value={form.notes}
                onChange={(e) => setForm({ ...form, notes: e.target.value })}
                placeholder="Internal notes about this customer..."
                rows={3}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="riskNotes">Risk Notes</Label>
              <Textarea
                id="riskNotes"
                value={form.riskNotes}
                onChange={(e) => setForm({ ...form, riskNotes: e.target.value })}
                placeholder="Any risk-related notes..."
                rows={3}
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsCreateDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleSubmit} disabled={pending} className="gap-2">
              {pending ? (
                <>
                  <RefreshCw className="h-4 w-4 animate-spin" />
                  Creating...
                </>
              ) : (
                <>
                  <Save className="h-4 w-4" />
                  Create Customer
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Customer Sheet */}
      <Sheet open={isEditSheetOpen} onOpenChange={setIsEditSheetOpen}>
        <SheetContent className="w-[500px] sm:max-w-[540px] overflow-y-auto">
          <SheetHeader>
            <SheetTitle className="flex items-center gap-2">
              <Edit className="h-5 w-5" />
              Edit Customer
            </SheetTitle>
            <SheetDescription>
              Update customer information. Changes will be saved immediately.
            </SheetDescription>
          </SheetHeader>

          <div className="grid gap-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="edit-fullName">Full Name *</Label>
                <Input
                  id="edit-fullName"
                  value={form.fullName}
                  onChange={(e) => setForm({ ...form, fullName: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-email">Email</Label>
                <Input
                  id="edit-email"
                  type="email"
                  value={form.email}
                  onChange={(e) => setForm({ ...form, email: e.target.value })}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="edit-phone">Phone *</Label>
                <Input
                  id="edit-phone"
                  value={form.phone}
                  onChange={(e) => setForm({ ...form, phone: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-alternatePhone">Alternate Phone</Label>
                <Input
                  id="edit-alternatePhone"
                  value={form.alternatePhone}
                  onChange={(e) => setForm({ ...form, alternatePhone: e.target.value })}
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="edit-address">Address</Label>
              <Input
                id="edit-address"
                value={form.address}
                onChange={(e) => setForm({ ...form, address: e.target.value })}
              />
            </div>

            <div className="grid grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label htmlFor="edit-city">City</Label>
                <Input
                  id="edit-city"
                  value={form.city}
                  onChange={(e) => setForm({ ...form, city: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-state">State</Label>
                <Input
                  id="edit-state"
                  value={form.state}
                  onChange={(e) => setForm({ ...form, state: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-postalCode">Postal Code</Label>
                <Input
                  id="edit-postalCode"
                  value={form.postalCode}
                  onChange={(e) => setForm({ ...form, postalCode: e.target.value })}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="edit-licenseNumber">License Number</Label>
                <Input
                  id="edit-licenseNumber"
                  value={form.licenseNumber}
                  onChange={(e) => setForm({ ...form, licenseNumber: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-licenseExpiry">License Expiry</Label>
                <Input
                  id="edit-licenseExpiry"
                  type="date"
                  value={form.licenseExpiry}
                  onChange={(e) => setForm({ ...form, licenseExpiry: e.target.value })}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="edit-identityDocumentType">ID Type</Label>
                <Select
                  value={form.identityDocumentType}
                  onValueChange={(v) => setForm({ ...form, identityDocumentType: v })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Passport">Passport</SelectItem>
                    <SelectItem value="Aadhar">Aadhar</SelectItem>
                    <SelectItem value="Driving License">Driving License</SelectItem>
                    <SelectItem value="Voter ID">Voter ID</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-identityDocumentNumber">ID Number</Label>
                <Input
                  id="edit-identityDocumentNumber"
                  value={form.identityDocumentNumber}
                  onChange={(e) => setForm({ ...form, identityDocumentNumber: e.target.value })}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="edit-emergencyContactName">Emergency Contact</Label>
                <Input
                  id="edit-emergencyContactName"
                  value={form.emergencyContactName}
                  onChange={(e) => setForm({ ...form, emergencyContactName: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-emergencyContactPhone">Emergency Phone</Label>
                <Input
                  id="edit-emergencyContactPhone"
                  value={form.emergencyContactPhone}
                  onChange={(e) => setForm({ ...form, emergencyContactPhone: e.target.value })}
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="edit-notes">Operations Notes</Label>
              <Textarea
                id="edit-notes"
                value={form.notes}
                onChange={(e) => setForm({ ...form, notes: e.target.value })}
                rows={3}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="edit-riskNotes">Risk Notes</Label>
              <Textarea
                id="edit-riskNotes"
                value={form.riskNotes}
                onChange={(e) => setForm({ ...form, riskNotes: e.target.value })}
                rows={3}
              />
            </div>
          </div>

          <SheetFooter>
            <Button variant="outline" onClick={() => setIsEditSheetOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleSubmit} disabled={pending} className="gap-2">
              {pending ? (
                <>
                  <RefreshCw className="h-4 w-4 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <Save className="h-4 w-4" />
                  Save Changes
                </>
              )}
            </Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>
    </div>
  );
}

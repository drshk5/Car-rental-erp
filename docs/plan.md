**CAR RENTAL SAAS**

**Complete Developer Documentation**

MVP Build Guide · Architecture · Database · API · UI Screens · Release
Checklist

  ----------------- ----------------- ------------------ -----------------
  **Stack**         **Timeline**      **Architecture**   **Version**

  Next.js 14 ·      30-Day MVP        Modular Monolith   v1.0 MVP
  ASP.NET Core 8 ·                                       
  MSSQL                                                  
  ----------------- ----------------- ------------------ -----------------

Prepared for: Development Team

Document Type: Full Developer Specification

**1. Project Overview**

**Goal**

Deliver an operational web-based admin app that enables a car rental
business to manage their full daily operations: fleet, customers,
bookings, rental execution, and payments --- within a 30-day MVP window.

> *Architecture Decision: Modular Monolith --- 1 frontend app, 1 backend
> API, 1 database. No microservices in month one. Microservices add
> auth-between-services complexity, deployment overhead, and debugging
> pain for a single-client MVP.*

**1.1 Technology Stack**

  --------------- ----------------------- -------------------------------
  **Layer**       **Technology**          **Notes**

  Frontend        **Next.js 14 (App       React 19, Tailwind CSS,
                  Router)**               shadcn/ui, React Hook Form +
                                          Zod, TanStack Query

  Backend         **ASP.NET Core 8 Web    Feature folders (not heavy
                  API**                   clean arch), EF Core 8, LINQ,
                                          FluentValidation

  Database        **MSSQL Server 2019+**  EF Core Code-First migrations,
                                          indexed for availability
                                          queries

  Auth            **JWT Bearer Tokens**   Refresh token rotation,
                                          role-based policies
                                          (Admin/Manager/Staff)

  File Upload     **Azure Blob / local    Customer & vehicle documents,
                  disk**                  license/ID scans

  Deployment      **Single server or      1 API instance, 1 DB --- simple
                  Azure App Service**     for month one
  --------------- ----------------------- -------------------------------

**1.2 MVP Scope**

  ----------------------- ----------------------- -----------------------
  **Area**                **In MVP (Month 1)**    **Phase 2 / Later**

  **Operations**          Vehicle, booking,       GPS, telematics, mobile
                          checkout, check-in,     app
                          payment status          

  **Customers**           Profile, license        Self-service customer
                          capture, verification   portal
                          flag                    

  **Pricing**             Daily/hourly pricing,   Seasonal pricing rules
                          deposits, discounts     engine

  **Reporting**           Dashboard, overdue      Advanced analytics, BI
                          list, CSV export        

  **Notifications**       Optional reminders if   Full SMS/WhatsApp
                          time remains            automation

  **Multi-branch**        Branch master +         Multi-tenant if needed
                          pickup/return location  
  ----------------------- ----------------------- -----------------------

**2. System Architecture**

**2.1 High-Level Flow**

> *Browser (Next.js) → HTTPS/JSON with JWT → ASP.NET Core 8 API → EF
> Core → MSSQL Server*

**2.2 Backend Folder Structure**

Feature-folder layout. No over-engineered repository pattern --- EF Core
DbContext directly in services is acceptable for MVP speed.

> src/
>
> Modules/
>
> Auth/ → AuthController, AuthService, JwtService, DTOs
>
> Users/ → UsersController, UsersService
>
> Branches/ → BranchesController, BranchesService
>
> Vehicles/ → VehiclesController, VehicleService, DocumentService
>
> Customers/ → CustomersController, CustomerService, DocumentService
>
> Bookings/ → BookingsController, BookingService, PricingService
>
> Rentals/ → RentalsController, RentalService
>
> Payments/ → PaymentsController, PaymentService
>
> Maintenance/ → MaintenanceController, MaintenanceService
>
> Dashboard/ → DashboardController, ReportService
>
> Shared/
>
> Contracts/ → Base DTOs, Pagination, ApiResponse\<T\>
>
> Infrastructure/ → AppDbContext, Middleware, Extensions
>
> Persistence/ → Migrations, Seeders
>
> Program.cs

**2.3 Frontend Folder Structure**

> src/
>
> app/
>
> (auth)/login/ → Login page
>
> dashboard/ → KPI dashboard
>
> vehicles/ → Fleet list, detail, new/edit
>
> customers/ → Customer list, detail, new/edit
>
> bookings/ → Booking list, new, detail
>
> rentals/ → Active rentals, overdue, checkout, checkin
>
> payments/ → Payment list, record payment, invoice
>
> maintenance/ → Maintenance log
>
> settings/ → Users, roles, branches
>
> components/
>
> ui/ → shadcn/ui components
>
> forms/ → Reusable form components
>
> tables/ → DataTable with pagination
>
> layout/ → Sidebar, Header, Breadcrumb
>
> lib/
>
> api.ts → Axios instance with JWT interceptor
>
> auth.ts → Auth helpers
>
> utils.ts → Formatters, validators
>
> hooks/ → useFetch\*, useMutation\* TanStack wrappers
>
> types/ → TypeScript interfaces matching API DTOs

**2.4 Cross-Cutting Rules**

-   All list endpoints return: { data: \[\], total, page, pageSize,
    > totalPages }

-   All write endpoints record created_by and updated_by user IDs

-   Use soft-delete (is_active) for master records; status fields for
    > transactional records

-   Server-side validation: date ranges, vehicle availability windows,
    > payment amounts

-   Return standard error shape: { success: false, message, errors: \[\]
    > }

-   Use HTTPS everywhere, even in development via dev cert

**3. Database Schema**

**3.1 Modeling Principles**

> *Keep quoted booking data separate from execution data. Store multiple
> payments per booking. Let maintenance and document expiry directly
> affect availability decisions.*

**3.2 All Tables**

**Security & Organization**

  -------------- --------------- -----------------------------------------
  **Table**      **Key Fields**  **All Columns**

  **roles**      role_id PK,     role_id (PK), name, permissions_json,
                 name            created_at

  **branches**   branch_id PK,   branch_id (PK), name, city, address,
                 name            phone, is_active, created_at

  **users**      user_id PK,     user_id (PK), role_id (FK), branch_id
                 role_id FK      (FK), full_name, email, password_hash,
                                 is_active, created_at, updated_at
  -------------- --------------- -----------------------------------------

**Fleet & Compliance**

  ------------------------- ---------------- --------------------------------------------------
  **Table**                 **Key Fields**   **All Columns**

  **vehicles**              vehicle_id PK,   vehicle_id (PK), branch_id (FK), plate_number
                            plate_number,    (UNIQUE), vin (UNIQUE), brand, model, year,
                            status           fuel_type, transmission, seats, daily_rate,
                                             hourly_rate, status \[Available\|Reserved\|Active
                                             Rental\|Maintenance\|Out of Service\], created_at,
                                             updated_at

  **vehicle_documents**     vehicle_doc_id   vehicle_doc_id (PK), vehicle_id (FK), doc_type
                            PK, vehicle_id   \[Registration\|Insurance\|Pollution\|Fitness\],
                            FK               doc_number, issue_date, expiry_date, file_url,
                                             status \[Valid\|Expiring\|Expired\]

  **maintenance_records**   maintenance_id   maintenance_id (PK), vehicle_id (FK),
                            PK, vehicle_id   service_type, scheduled_at, completed_at,
                            FK               vendor_name, cost, status \[Scheduled\|In
                                             Progress\|Completed\], notes
  ------------------------- ---------------- --------------------------------------------------

**Customer & Booking**

  ------------------------ ----------------- -----------------------------------------------------
  **Table**                **Key Fields**    **All Columns**

  **customers**            customer_id PK,   customer_id (PK), customer_code (UNIQUE), full_name,
                           phone, license_no phone, email, address, license_no, license_expiry,
                                             verification_status \[Pending\|Verified\|Rejected\],
                                             created_at, updated_at

  **customer_documents**   customer_doc_id   customer_doc_id (PK), customer_id (FK), doc_type
                           PK, customer_id   \[License\|Aadhaar\|Passport\|Other\], doc_number,
                           FK                file_url, verified_at

  **bookings**             booking_id PK,    booking_id (PK), booking_no (UNIQUE auto-gen),
                           booking_no,       customer_id (FK), vehicle_id (FK), pickup_branch_id
                           vehicle_id FK     (FK), return_branch_id (FK), start_at, end_at,
                                             pricing_plan \[Daily\|Hourly\], base_amount,
                                             discount_amount, deposit_amount, quoted_total, status
                                             \[Draft\|Confirmed\|Active\|Completed\|Cancelled\],
                                             created_by (FK), created_at, updated_at
  ------------------------ ----------------- -----------------------------------------------------

**Execution & Payments**

  ------------------------- --------------- -----------------------------------------
  **Table**                 **Key Fields**  **All Columns**

  **rental_transactions**   rental_id PK,   rental_id (PK), booking_id (FK),
                            booking_id FK   checked_out_by (FK), checked_in_by (FK),
                                            check_out_at, check_in_at, odometer_out,
                                            odometer_in, fuel_out
                                            \[Full\|3/4\|Half\|1/4\|Empty\], fuel_in,
                                            extra_charges, damage_notes,
                                            final_amount, status
                                            \[Active\|Completed\]

  **payments**              payment_id PK,  payment_id (PK), booking_id (FK), amount,
                            booking_id FK   payment_method \[Cash\|UPI\|Card\|Bank
                                            Transfer\], reference_no, payment_status
                                            \[Pending\|Paid\|Refunded\], paid_at,
                                            received_by (FK), notes
  ------------------------- --------------- -----------------------------------------

**3.3 Suggested Database Indexes**

-   vehicles: (plate_number), (status), (branch_id, status)

-   customers: (phone), (license_no), (customer_code)

-   bookings: (vehicle_id, start_at, end_at), (customer_id, status),
    > (booking_no)

-   payments: (booking_id, paid_at)

-   maintenance_records: (vehicle_id, scheduled_at)

**3.4 Availability Logic**

> *A vehicle is UNAVAILABLE when: (1) status = \'Active Rental\', OR (2)
> has a confirmed/reserved booking overlapping the requested window, OR
> (3) status = \'Maintenance\'. Overlap query must check bookings with
> status IN (Confirmed, Active) for the date range.*

**4. API Reference**

> *Base URL: https://api.yourdomain.com/api/v1 \| All endpoints require
> Authorization: Bearer \<token\> except /auth/login*

**4.1 Auth**

  ------------ ------------------------ -------------------------------- ----------
  **Method**   **Endpoint**             **Description**                  **Auth**

  **POST**     /auth/login              Email + password, returns        Public
                                        access + refresh token           

  **POST**     /auth/refresh            Exchange refresh token for new   Public
                                        access token                     

  **POST**     /auth/logout             Revoke refresh token             All

  **GET**      /auth/me                 Get current user profile +       All
                                        permissions                      
  ------------ ------------------------ -------------------------------- ----------

**4.2 Users & Roles**

  ------------ ------------------------ -------------------------------- ----------
  **Method**   **Endpoint**             **Description**                  **Auth**

  **GET**      /users                   List all staff users with        Admin
                                        pagination                       

  **POST**     /users                   Create new staff user            Admin

  **PUT**      /users/{id}              Update user details              Admin

  **PATCH**    /users/{id}/status       Activate / deactivate user       Admin

  **GET**      /roles                   List all roles with permissions  Admin

  **PUT**      /roles/{id}              Update role permissions          Admin
  ------------ ------------------------ -------------------------------- ----------

**4.3 Branches**

  ------------ ------------------------ -------------------------------- ----------
  **Method**   **Endpoint**             **Description**                  **Auth**

  **GET**      /branches                List all branches                All

  **POST**     /branches                Create branch                    Admin

  **PUT**      /branches/{id}           Update branch                    Admin

  **PATCH**    /branches/{id}/status    Activate / deactivate branch     Admin
  ------------ ------------------------ -------------------------------- ----------

**4.4 Vehicles**

  ------------ ---------------------------------- -------------------------------- ---------------
  **Method**   **Endpoint**                       **Description**                  **Auth**

  **GET**      /vehicles                          List vehicles with filters       All
                                                  (branch, status, type)           

  **POST**     /vehicles                          Add new vehicle to fleet         Admin,Manager

  **GET**      /vehicles/{id}                     Get vehicle detail + documents + All
                                                  history                          

  **PUT**      /vehicles/{id}                     Update vehicle details           Admin,Manager

  **PATCH**    /vehicles/{id}/status              Change vehicle status manually   Manager

  **GET**      /vehicles/availability             Search available vehicles by     All
                                                  branch + date range              

  **GET**      /vehicles/{id}/documents           List vehicle documents           All

  **POST**     /vehicles/{id}/documents           Upload vehicle document          Admin,Manager

  **DELETE**   /vehicles/{id}/documents/{docId}   Remove document                  Admin
  ------------ ---------------------------------- -------------------------------- ---------------

**4.5 Customers**

  ------------ ------------------------------ -------------------------------- ----------
  **Method**   **Endpoint**                   **Description**                  **Auth**

  **GET**      /customers                     List customers with search       All
                                              (name, phone, license)           

  **POST**     /customers                     Create customer profile          All

  **GET**      /customers/{id}                Customer detail + documents +    All
                                              booking history                  

  **PUT**      /customers/{id}                Update customer info             All

  **PATCH**    /customers/{id}/verification   Update verification status       Manager

  **GET**      /customers/search              Quick lookup by phone or license All
                                              no                               

  **POST**     /customers/{id}/documents      Upload ID/license document       All
  ------------ ------------------------------ -------------------------------- ----------

**4.6 Bookings**

  ------------ ------------------------ -------------------------------- ----------
  **Method**   **Endpoint**             **Description**                  **Auth**

  **GET**      /bookings                List bookings with filters       All
                                        (status, date, branch)           

  **POST**     /bookings                Create booking (returns draft    All
                                        with quoted price)               

  **GET**      /bookings/{id}           Booking detail with payments and All
                                        rental                           

  **PUT**      /bookings/{id}           Update booking before            All
                                        confirmation                     

  **POST**     /bookings/{id}/confirm   Confirm booking, lock vehicle    All

  **POST**     /bookings/{id}/cancel    Cancel booking, release vehicle  Manager

  **GET**      /bookings/{id}/quote     Recalculate price for given      All
                                        vehicle + dates                  

  **GET**      /bookings/today          Today\'s pickups and returns     All
  ------------ ------------------------ -------------------------------- ----------

**4.7 Rental Execution**

  ------------ ---------------------------- -------------------------------- ----------
  **Method**   **Endpoint**                 **Description**                  **Auth**

  **POST**     /rentals/checkout            Check out vehicle --- capture    All
                                            odometer, fuel, notes            

  **GET**      /rentals/active              All currently active rental      All
                                            transactions                     

  **GET**      /rentals/overdue             Rentals past their end_at with   All
                                            overdue duration                 

  **POST**     /rentals/{id}/checkin        Check in --- odometer, fuel,     All
                                            damage, extra charges            

  **POST**     /rentals/{id}/swap-vehicle   Manager assigns different        Manager
                                            vehicle to booking               

  **GET**      /rentals/{id}                Full rental detail               All
  ------------ ---------------------------- -------------------------------- ----------

**4.8 Payments**

  ------------ ------------------------------- -------------------------------- ----------
  **Method**   **Endpoint**                    **Description**                  **Auth**

  **GET**      /payments                       List all payments with filters   All

  **POST**     /payments                       Record a payment against a       All
                                               booking                          

  **GET**      /payments/booking/{bookingId}   All payments for a specific      All
                                               booking                          

  **POST**     /payments/{id}/refund           Mark payment as refunded         Manager

  **GET**      /payments/summary               Revenue summary by date range /  Manager
                                               branch                           

  **GET**      /payments/unpaid                Bookings with unpaid or partial  All
                                               balance                          
  ------------ ------------------------------- -------------------------------- ----------

**4.9 Maintenance**

  ------------ ---------------------------------- -------------------------------- ----------
  **Method**   **Endpoint**                       **Description**                  **Auth**

  **GET**      /maintenance                       List maintenance records         All

  **POST**     /maintenance                       Create maintenance record        Manager
                                                  (blocks vehicle)                 

  **GET**      /maintenance/vehicle/{vehicleId}   History for a vehicle            All

  **PATCH**    /maintenance/{id}/complete         Mark maintenance complete,       Manager
                                                  restore vehicle                  
  ------------ ---------------------------------- -------------------------------- ----------

**4.10 Dashboard & Reports**

  ------------ -------------------------- -------------------------------- ----------
  **Method**   **Endpoint**               **Description**                  **Auth**

  **GET**      /dashboard/summary         Available cars, active rentals,  All
                                          today pickups/returns, overdue,  
                                          unpaid, revenue today            

  **GET**      /reports/bookings          Bookings report --- filterable   Manager
                                          by date range, branch, status    

  **GET**      /reports/payments          Payments report by date range,   Manager
                                          method, status                   

  **GET**      /reports/expiries          Vehicles with expiring/expired   All
                                          documents                        

  **GET**      /reports/bookings/export   CSV export of booking data       Manager

  **GET**      /reports/payments/export   CSV export of payment data       Manager
  ------------ -------------------------- -------------------------------- ----------

**5. UI Screens & Component Specifications**

> *All screens use: Next.js App Router, Tailwind CSS, shadcn/ui
> components, TanStack Query for data, React Hook Form + Zod for forms.
> Sidebar navigation is always visible for authenticated users.*

**5.1 Login Screen**

  ---------------- -------------------------------------------------------
  **Property**     **Specification**

  **Route**        /login

  **Access**       Public (redirect to /dashboard if already
                   authenticated)

  **Layout**       Centered card, company logo top, tagline

  **Fields**       Email (text input), Password (input with show/hide
                   toggle)

  **Actions**      Login button (primary), \'Forgot Password\' link (Phase
                   2)

  **Validation**   Email format, password min 6 chars, show inline error

  **On Success**   Store JWT in httpOnly cookie, redirect to /dashboard

  **On Failure**   Show toast: \'Invalid credentials\'

  **Components**   Card, Input, Button, Form (shadcn)
  ---------------- -------------------------------------------------------

**5.2 Dashboard**

  --------------- -------------------------------------------------------
  **Property**    **Specification**

  **Route**       /dashboard

  **Access**      All authenticated roles

  **KPI Cards     Available Cars (green), Active Rentals (blue), Today
  (Row 1)**       Pickups (orange), Today Returns (purple)

  **KPI Cards     Overdue Rentals (red badge), Unpaid Bookings (yellow),
  (Row 2)**       Revenue Today (green), Revenue This Month

  **Table 1**     Today\'s Pickups --- columns: Booking No, Customer,
                  Vehicle, Pickup Time, Status, Action (Checkout)

  **Table 2**     Today\'s Returns --- columns: Booking No, Customer,
                  Vehicle, Return Time, Status, Action (Checkin)

  **Table 3**     Overdue Rentals --- columns: Customer, Vehicle,
                  Expected Return, Days Overdue (red highlight), Action

  **Table 4**     Expiring Documents --- Vehicle, Doc Type, Expiry Date,
                  Days Left (color coded)

  **Refresh**     Auto-refresh every 2 minutes or manual refresh button

  **Branch        Admin/Manager can filter dashboard by branch
  Filter**        
  --------------- -------------------------------------------------------

**5.3 Vehicles**

**5.3.1 Vehicle List (/vehicles)**

  ---------------- -------------------------------------------------------
  **Property**     **Specification**

  **Filters**      Branch dropdown, Status
                   (all/available/rented/maintenance), Fuel Type, Search
                   by plate/brand

  **Table          Plate, Brand/Model, Year, Type, Seats, Status (badge),
  Columns**        Daily Rate, Branch, Actions

  **Status         Available=green, Reserved=blue, Active Rental=orange,
  Badges**         Maintenance=yellow, Out of Service=red

  **Actions per    View detail, Edit, Change Status, View Documents
  row**            

  **Bulk Action**  Export to CSV

  **Top Button**   \+ Add Vehicle (Manager/Admin only)

  **Pagination**   Server-side, 20 per page default
  ---------------- -------------------------------------------------------

**5.3.2 Vehicle Detail (/vehicles/\[id\])**

-   Tab 1 --- Details: All vehicle fields, editable inline by
    > Manager/Admin

-   Tab 2 --- Documents: List of registration/insurance/pollution docs
    > with expiry countdown, upload new doc

-   Tab 3 --- Booking History: Last 20 bookings for this vehicle

-   Tab 4 --- Maintenance History: All maintenance records for this
    > vehicle

-   Status chip at top right --- color coded, click to change status
    > (with reason)

**5.3.3 Add / Edit Vehicle (/vehicles/new, /vehicles/\[id\]/edit)**

-   Section 1: Basic Info --- Plate number, VIN, Brand, Model, Year,
    > Color

-   Section 2: Specs --- Fuel type (dropdown), Transmission, Seats,
    > Vehicle type

-   Section 3: Rates --- Daily rate, Hourly rate, Security deposit
    > default

-   Section 4: Branch assignment, Initial status

-   Section 5: Documents --- Upload Registration/Insurance/Pollution
    > certs with expiry dates

-   Validation: Plate number uniqueness check (API), all required fields

**5.4 Customers**

**5.4.1 Customer List (/customers)**

  ---------------- -------------------------------------------------------
  **Property**     **Specification**

  **Search**       By name, phone, email, license number --- live search
                   with debounce

  **Table          Customer Code, Name, Phone, License No, Verification
  Columns**        Status, Total Bookings, Last Booking, Actions

  **Verification   Pending=yellow, Verified=green, Rejected=red
  Badge**          

  **Actions**      View, Edit, New Booking shortcut

  **Top Button**   \+ Add Customer
  ---------------- -------------------------------------------------------

**5.4.2 Customer Detail (/customers/\[id\])**

-   Profile section: All personal info with Edit button

-   Documents section: Uploaded ID/license files with preview and
    > verification toggle

-   Booking history: Sortable table of all bookings with status and
    > total paid

-   Quick action: \'Create New Booking\' button that pre-fills customer
    > in booking form

**5.4.3 Add / Edit Customer**

-   Full name, Phone (required, unique check), Email, Address

-   License Number, License Expiry Date

-   Verification status (Manager/Admin only)

-   Document uploads: License scan, ID proof (Aadhaar/Passport)

**5.5 Bookings**

**5.5.1 Booking List (/bookings)**

  --------------- -------------------------------------------------------
  **Property**    **Specification**

  **Filters**     Status, Date range (start_at), Branch, Search by
                  booking no / customer name

  **Table         Booking No, Customer, Vehicle, Start, End, Duration,
  Columns**       Total, Status, Payment Status, Actions

  **Status        Draft=gray, Confirmed=blue, Active=orange,
  Colors**        Completed=green, Cancelled=red

  **Actions**     View, Checkout (if confirmed), Checkin (if active),
                  Cancel

  **Quick Filter  All \| Today Pickups \| Today Returns \| Overdue \|
  Tabs**          Unpaid
  --------------- -------------------------------------------------------

**5.5.2 Create Booking (/bookings/new) --- 4-Step Wizard**

-   Step 1 --- Select Customer: Search existing or create new inline

-   Step 2 --- Select Vehicle: Date/time range first → availability
    > search → vehicle cards with rate

-   Step 3 --- Pricing: Auto-calculated base + deposit + discount
    > field + extra driver option → quoted total

-   Step 4 --- Confirm: Summary card, advance payment amount, confirm
    > button → sends confirmation

**5.5.3 Booking Detail (/bookings/\[id\])**

-   Header: Booking No (large), Status badge, Created by, Created at

-   Customer panel: Photo, name, phone, verification status, link to
    > profile

-   Vehicle panel: Plate, brand, model, status, link to vehicle detail

-   Booking details: All dates, locations, pricing breakdown table

-   Payment section: Payment history table + \'Record Payment\' button

-   Rental section: Checkout/Checkin details once active

-   Action buttons: Confirm, Checkout, Checkin, Cancel --- shown
    > contextually by status

**5.6 Rental Execution**

**5.6.1 Checkout Screen (/rentals/checkout/\[bookingId\])**

-   Customer & vehicle summary at top (read-only confirmation)

-   Odometer Out --- number input

-   Fuel Level Out --- dropdown: Full / 3/4 / Half / 1/4 / Empty

-   Accessories Checklist --- spare tyre, jack, first aid kit, etc.
    > (checkboxes)

-   Handover Notes --- textarea for any remarks

-   Signature / acknowledgment checkbox

-   \'Complete Checkout\' button --- sets booking status Active, creates
    > rental_transaction

**5.6.2 Check-In Screen (/rentals/checkin/\[rentalId\])**

-   Shows expected return datetime vs actual return datetime (late
    > return flag)

-   Odometer In --- number input (validates \>= odometer_out)

-   Fuel Level In --- dropdown

-   Late Return Charge --- auto-calculated if past end_at, editable

-   Fuel Charge --- if fuel_in \< fuel_out, enter charge

-   Damage Notes --- textarea + amount field

-   Other Extra Charges --- label + amount rows (add more button)

-   Final Amount Summary --- base + late + damage + fuel + extras

-   \'Complete Check-In\' button --- finalizes rental, triggers payment
    > reconciliation

**5.6.3 Active Rentals (/rentals/active)**

-   Cards showing: Vehicle, Customer, Checked out at, Expected return,
    > Duration so far

-   Overdue highlighted in red with overdue hours badge

-   Quick \'Start Check-In\' button on each card

**5.7 Payments**

**5.7.1 Record Payment (modal or /payments/new)**

-   Booking selector (or pre-filled from booking detail)

-   Amount --- number input with outstanding balance shown

-   Payment Method --- Cash / UPI / Card / Bank Transfer (radio or
    > select)

-   Reference No --- optional, shown for UPI/Card/Bank

-   Paid At --- date/time picker (defaults to now)

-   Notes --- optional textarea

**5.7.2 Payment History per Booking**

-   Table: Date, Method, Amount, Reference, Status, Received By

-   Balance row at bottom: Total Quoted / Total Paid / Outstanding

-   Refund button (Manager) per payment row

**5.8 Maintenance**

-   /maintenance --- list of all maintenance records filterable by
    > vehicle / status / date

-   /maintenance/new --- form: Vehicle selector, service type, scheduled
    > date, vendor, estimated cost, notes

-   Creating a record automatically changes vehicle status to
    > Maintenance

-   \'Mark Complete\' button --- sets completed_at, restores vehicle to
    > Available

**5.9 Settings**

**5.9.1 User Management (/settings/users) --- Admin only**

-   List of staff users with role, branch, active status

-   Add User: name, email, temp password, role, branch assignment

-   Deactivate/Activate toggle

**5.9.2 Branch Management (/settings/branches) --- Admin only**

-   List of branches with city, phone, active status

-   Add/Edit branch form

**5.9.3 Role Permissions (/settings/roles) --- Admin only**

-   Matrix view: rows = roles, columns = permissions

-   Toggle checkboxes for each permission

**6. Business Logic & Validation Rules**

**6.1 Booking & Pricing Rules**

-   Minimum rental duration: 1 hour for hourly bookings, 1 day for daily
    > bookings

-   Daily rate: ceil(duration hours / 24) × daily_rate

-   Hourly rate: duration hours × hourly_rate

-   Quoted total = base_amount - discount_amount + deposit_amount

-   Advance payment at booking confirmation must be \>= deposit_amount

-   Vehicle availability check: no overlapping bookings with status
    > Confirmed or Active for the same vehicle

-   Booking number format: BK-YYYYMMDD-XXXX (auto-generated)

**6.2 Rental Execution Rules**

-   Checkout is only allowed for bookings with status = Confirmed

-   Check-in is only allowed for bookings with status = Active

-   Late return charge formula: (hours overdue / 24 ) × daily_rate × 1.5
    > OR hourly_rate × hours overdue

-   Fuel charge: only charged if fuel_in level \< fuel_out level
    > (manager sets flat per-level charge)

-   Final amount = base_amount + late_charge + fuel_charge +
    > damage_charges + other_extras

**6.3 Vehicle Availability States**

  ----------------- --------------------------- ---------------------------
  **Status**        **Meaning**                 **Allowed Transitions**

  **Available**     Ready to be booked          → Reserved (booking
                                                confirmed) \| → Maintenance

  **Reserved**      Booking confirmed, not yet  → Active Rental (checkout)
                    checked out                 \| → Available (cancel)

  **Active Rental** Vehicle is currently out    → Available (check-in
                    with customer               complete)

  **Maintenance**   Undergoing scheduled        → Available (maintenance
                    service                     complete)

  **Out of          Not operational             → Available (manual by
  Service**                                     Admin)
  ----------------- --------------------------- ---------------------------

**6.4 Role Permissions Matrix**

  ----------------------- --------------- --------------- ---------------
  **Action**              **Admin**       **Manager**     **Staff**

  **Add / Edit Vehicles** ✓               ✓               ✗

  **Delete / Deactivate   ✓               ✗               ✗
  Vehicle**                                               

  **Create / Edit         ✓               ✓               ✓
  Booking**                                               

  **Cancel Booking**      ✓               ✓               ✗

  **Checkout / Check-In** ✓               ✓               ✓

  **Swap Vehicle**        ✓               ✓               ✗

  **Record Payment**      ✓               ✓               ✓

  **Refund Payment**      ✓               ✓               ✗

  **View Reports &        ✓               ✓               ✗
  Export**                                                

  **Manage Users &        ✓               ✗               ✗
  Roles**                                                 

  **Manage Branches**     ✓               ✗               ✗

  **Verify Customer**     ✓               ✓               ✗
  ----------------------- --------------- --------------- ---------------

**7. 30-Day Delivery Timeline**

> *Feature freeze at Day 25. Days 26--30 reserved strictly for testing,
> bug fixes, and deployment. No new features after Day 25.*

  ----------- ---------- ------------------- ------------------------------
  **Week**    **Days**   **Focus**           **Deliverables**

  **Week 1**  1--7       **Foundation**      Project setup (Next.js + API),
                                             MSSQL + EF Core migrations,
                                             JWT Auth module,
                                             Users/Roles/Branches CRUD,
                                             login screen

  **Week 2**  8--14      **Core CRUD**       Vehicle module (full CRUD +
                                             documents + availability
                                             search), Customer module
                                             (CRUD + doc upload), Booking
                                             create/confirm/cancel +
                                             pricing engine

  **Week 3**  15--21     **Operations**      Checkout/check-in workflows,
                                             late return + extra charge
                                             logic, Payments module (all
                                             methods), Maintenance module,
                                             Dashboard KPIs

  **Week 4**  22--30     **Polish & Deploy** Reports + CSV export, UI
                                             polish, mobile responsiveness
                                             check, integration testing,
                                             production deploy, UAT with
                                             client
  ----------- ---------- ------------------- ------------------------------

**7.1 Development Priorities (P0 → P2)**

  -------------- -------------------- ---------------------------------------------
  **Priority**   **Module**           **Reason**

  **P0**         **Auth + Users**     Nothing works without auth and roles

  **P0**         **Vehicles**         Core fleet data --- all other modules depend
                                      on it

  **P0**         **Customers**        Needed before any booking can be created

  **P0**         **Bookings**         Core business transaction --- must work
                                      flawlessly

  **P1**         **Rentals**          Checkout/checkin is daily operational need

  **P1**         **Payments**         Revenue tracking --- client needs this from
                                      day 1

  **P1**         **Dashboard**        Operators need the daily ops view

  **P2**         **Maintenance**      Important but can be simplified in MVP

  **P2**         **Reports/Export**   Useful but not blocking operations

  **P3**         **Notifications**    Only if time remains in Week 4
  -------------- -------------------- ---------------------------------------------

**8. Core Business Workflow**

**8.1 Main Operational Flow**

1.  Create Customer (or look up existing customer)

2.  Create Booking: select customer → search available vehicles for
    dates → get quote → confirm → record advance payment

3.  Day of Pickup: open booking → Checkout → enter odometer out, fuel
    level, accessories, notes → Complete Checkout

4.  During Rental: monitor Active Rentals dashboard, handle any issues
    or extensions

5.  Return Day: open rental → Check-In → enter odometer in, fuel level,
    damage notes, extra charges → Calculate final amount

6.  Settle Payment: record remaining balance payment → booking status =
    Completed

**8.2 Vehicle Availability Query Logic**

> *SELECT \* FROM vehicles WHERE branch_id = \@branch AND status =
> \'Available\' AND vehicle_id NOT IN (SELECT vehicle_id FROM bookings
> WHERE status IN (\'Confirmed\',\'Active\') AND NOT (end_at \<= \@start
> OR start_at \>= \@end))*

**8.3 Booking Number Generation**

Format: BK-{YYYYMMDD}-{4-digit-sequence} Example: BK-20250307-0042

Use a database sequence or application-level counter with date prefix
reset.

**8.4 Payment Reconciliation**

-   Booking payment_status is computed: sum(payments.amount where
    > status=Paid)

-   If sum = 0 → Unpaid

-   If sum \> 0 AND sum \< quoted_total + extra_charges → Partial

-   If sum \>= quoted_total + extra_charges → Paid

**9. Release & Deployment Checklist**

**9.1 Pre-Release Technical Checklist**

  --- ----------------------------------------------- -------------------
      **Item**                                        **Owner**

  ☐   All EF Core migrations applied to production DB Backend Dev

  ☐   Environment variables set: DB connection, JWT   DevOps
      secret, storage keys                            

  ☐   HTTPS enabled (SSL cert) on API and frontend    DevOps
      domains                                         

  ☐   CORS configured to allow only frontend domain   Backend Dev

  ☐   Seed data: Admin user created, Roles seeded, at Backend Dev
      least 1 Branch                                  

  ☐   Rate limiting on /auth/login endpoint (prevent  Backend Dev
      brute force)                                    

  ☐   File upload: max size limit set, allowed types  Backend Dev
      validated (jpg/png/pdf)                         

  ☐   API returns 401 (not 500) for unauthenticated   Backend Dev
      requests                                        

  ☐   All list endpoints return paginated responses   Backend Dev

  ☐   Input sanitization on all string fields         Backend Dev

  ☐   Frontend: JWT stored in httpOnly cookie (not    Frontend Dev
      localStorage)                                   

  ☐   Frontend: Axios interceptor handles 401 →       Frontend Dev
      redirect to login                               

  ☐   Frontend: Role-based UI (hide buttons user      Frontend Dev
      cannot access)                                  

  ☐   Database backup scheduled (daily minimum)       DevOps

  ☐   Application logs configured (write to           DevOps
      file/Azure Monitor)                             

  ☐   Error boundary in frontend --- no unhandled     Frontend Dev
      crashes                                         
  --- ----------------------------------------------- -------------------

**9.2 UAT Acceptance Checklist**

> *Client must sign off on each item below before go-live.*

  --- -------------------------------------------------------------------
      **Acceptance Criterion**

  ☐   Staff can register a vehicle with all required fields and documents

  ☐   Staff can register a customer with ID proof upload

  ☐   Staff can create a booking for a specific vehicle and date range

  ☐   System prevents double-booking the same vehicle for overlapping
      dates

  ☐   Staff can perform a complete checkout capturing odometer and fuel
      level

  ☐   Staff can perform a complete check-in with extra charges if
      applicable

  ☐   System correctly calculates late return charges

  ☐   Manager can view all active rentals and identify overdue ones

  ☐   Manager can view today\'s pickups and today\'s returns on the
      dashboard

  ☐   Staff can record a payment against a booking (cash/UPI/card)

  ☐   Manager can see unpaid and partially paid bookings

  ☐   Admin can export booking and payment data as CSV

  ☐   Admin can create/deactivate staff users and assign roles

  ☐   Dashboard shows accurate vehicle availability count

  ☐   Expiring vehicle documents appear in the dashboard alerts
  --- -------------------------------------------------------------------

**9.3 Critical Non-Goals for Month 1**

-   No customer-facing mobile app

-   No live GPS or IoT vehicle tracking

-   No full accounting / tax engine (GST invoicing is Phase 2)

-   No multi-tenant architecture (single client, single database)

-   No microservices --- modular monolith only

-   No automated SMS/WhatsApp notifications (manual only)

**10. Environment Setup & Configuration**

**10.1 Backend (ASP.NET Core 8) --- appsettings.json**

> {
>
> \"ConnectionStrings\": {
>
> \"DefaultConnection\":
> \"Server=.;Database=CarRentalDb;Trusted_Connection=True;\"
>
> },
>
> \"JwtSettings\": {
>
> \"SecretKey\": \"YOUR_SECRET_KEY_MIN_32_CHARS\",
>
> \"Issuer\": \"CarRentalApi\",
>
> \"Audience\": \"CarRentalApp\",
>
> \"AccessTokenExpiryMinutes\": 60,
>
> \"RefreshTokenExpiryDays\": 7
>
> },
>
> \"FileStorage\": {
>
> \"Provider\": \"Local\",
>
> \"BasePath\": \"/uploads\"
>
> }
>
> }

**10.2 Frontend (.env.local)**

> NEXT_PUBLIC_API_URL=https://api.yourdomain.com/api/v1
>
> NEXT_PUBLIC_APP_NAME=CarRental Manager

**10.3 NuGet Packages (Backend)**

-   Microsoft.EntityFrameworkCore.SqlServer --- MSSQL provider

-   Microsoft.EntityFrameworkCore.Tools --- EF migrations CLI

-   Microsoft.AspNetCore.Authentication.JwtBearer --- JWT auth

-   BCrypt.Net-Next --- password hashing

-   FluentValidation.AspNetCore --- request validation

-   Serilog.AspNetCore --- structured logging

-   Swashbuckle.AspNetCore --- Swagger UI for API docs

**10.4 NPM Packages (Frontend)**

-   \@tanstack/react-query --- server state management

-   axios --- HTTP client with interceptors

-   react-hook-form + \@hookform/resolvers + zod --- forms + validation

-   \@radix-ui/\* + shadcn/ui --- accessible UI components

-   date-fns --- date formatting and calculations

-   recharts --- dashboard charts

-   react-hot-toast --- notifications/toasts

-   lucide-react --- icon library

Document Version: 1.0 \| Last Updated: March 2026 \| Status: Ready for
Development
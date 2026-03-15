# Car Rental ERP

Car Rental ERP is an internal operations platform for running a car rental business. It covers fleet management, bookings, rentals, customers, payments, maintenance, branch operations, and role-based admin access in a single codebase.

The project is built as a modular monolith with a .NET 8 API and a Next.js 15 admin frontend.

## What is in the repo

This repository includes:

- an ASP.NET Core API for business workflows and admin operations
- a Next.js dashboard for staff users
- JWT authentication with role and permission checks
- EF Core with SQLite for local development
- vehicle pricing with daily, hourly, and per-kilometer rates
- Docker support for running the API in a container

The current codebase is not just a starter. It already has implemented modules, frontend routes, database migrations, and seeded demo users.

## Stack

**Backend**

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- SQLite
- JWT Bearer Authentication
- Swagger

**Frontend**

- Next.js 15 App Router
- React 19
- TypeScript 5
- Tailwind CSS 3
- `shadcn/ui` style components
- Radix UI primitives
- `class-variance-authority`
- `tailwind-merge`
- Lucide React

**Infrastructure**

- Docker
- Docker Compose

## Features

The application currently includes modules for:

- authentication
- dashboard
- vehicles
- bookings
- rentals
- customers
- payments
- maintenance
- owners
- users and roles
- branches
- system health

When opening the web app, unauthenticated users are sent to `/login`. Signed-in users are sent to `/dashboard`.

## Project structure

```text
.
├── docs/
├── docker-compose.yml
├── CarRentalERP.sln
└── src/
    ├── backend/
    │   ├── CarRentalERP.Api/
    │   ├── CarRentalERP.Application/
    │   ├── CarRentalERP.Domain/
    │   ├── CarRentalERP.Infrastructure/
    │   └── CarRentalERP.Shared/
    └── frontend/
        └── web/
            ├── app/
            ├── components/
            ├── lib/
            └── types/
```

## Architecture

The backend is split into five layers:

1. `CarRentalERP.Api` for controllers, auth, startup, and HTTP concerns.
2. `CarRentalERP.Application` for use cases and orchestration.
3. `CarRentalERP.Domain` for entities, enums, and business rules.
4. `CarRentalERP.Infrastructure` for persistence, repositories, and migrations.
5. `CarRentalERP.Shared` for shared contracts and cross-cutting models.

The frontend lives in `src/frontend/web` and uses the App Router, shared UI components, and server-side integrations against the API.

## Quick start

### Prerequisites

- .NET SDK 8
- Node.js 20 or newer
- Yarn 4 or newer
- Docker Desktop if you want to run the API with Compose

### Configure local secrets

Before starting the backend, provide a JWT signing key with at least 32 characters.

From the repository root:

```bash
dotnet user-secrets set "Authentication:SigningKey" "replace-with-a-long-random-development-secret" --project src/backend/CarRentalERP.Api
```

You can also use an environment variable instead:

```bash
export Authentication__SigningKey="replace-with-a-long-random-development-secret"
```

### Run the backend

From the repository root:

```bash
dotnet run --project src/backend/CarRentalERP.Api
```

The API runs on:

```text
http://127.0.0.1:5001
```

Swagger is available in development at:

```text
http://127.0.0.1:5001/swagger
```

### Run the frontend

```bash
cd src/frontend/web
yarn install
yarn dev:hot
```

The frontend runs on:

```text
http://localhost:3000
```

## Local development notes

- EF Core migrations are applied automatically when the API starts.
- Demo seed data runs only in `Development` and only when `Seeding:DemoDataEnabled=true`.
- API routes are served under `/api/v1`.
- Swagger is enabled in development only.
- The frontend uses `http://127.0.0.1:5001/api/v1` by default if no API URL is configured.
- Vehicle records now store `DailyRate`, `HourlyRate`, and `KmRate`.

## Configuration

Backend defaults from `src/backend/CarRentalERP.Api/appsettings.json`:

- `ConnectionStrings__DefaultConnection=Data Source=car-rental-erp.db`
- `Authentication__Issuer=CarRentalERP`
- `Authentication__Audience=CarRentalERP.Web`
- allowed origin `http://localhost:3000`

Development overrides from `src/backend/CarRentalERP.Api/appsettings.Development.json`:

- `ConnectionStrings__DefaultConnection=Data Source=car-rental-erp.dev.db`
- `Seeding__DemoDataEnabled=true`

Required secret/configuration:

- `Authentication__SigningKey` must be supplied through `dotnet user-secrets`, environment variables, or external secret management.
- The API fails fast on startup if the signing key is missing or shorter than 32 characters.

Frontend API URL resolution order:

1. `INTERNAL_API_URL`
2. `NEXT_PUBLIC_API_URL`
3. `http://127.0.0.1:5001/api/v1`

## Docker

To run the API with Docker Compose:

```bash
docker compose up --build
```

The container exposes:

```text
http://localhost:8080
```

Compose persists:

- SQLite database files
- ASP.NET Core data protection keys

## Recommended local workflow

1. Configure `Authentication__SigningKey`.
2. Start the backend with `dotnet run --project src/backend/CarRentalERP.Api`.
3. Start the frontend with `cd src/frontend/web && yarn install && yarn dev:hot`.
4. Sign in with one of the demo users when `Seeding:DemoDataEnabled=true`.

## Demo login

When development seeding is enabled on a fresh database:

- `admin@carrental.local` / `change-me`
- `manager@carrental.local` / `change-me`
- `staff@carrental.local` / `change-me`

## Useful files

- [README.md](/Users/drshk5/dev/car-rental-erp/README.md)
- [docker-compose.yml](/Users/drshk5/dev/car-rental-erp/docker-compose.yml)
- [src/backend/CarRentalERP.Api/Program.cs](/Users/drshk5/dev/car-rental-erp/src/backend/CarRentalERP.Api/Program.cs)
- [src/backend/CarRentalERP.Api/appsettings.json](/Users/drshk5/dev/car-rental-erp/src/backend/CarRentalERP.Api/appsettings.json)
- [src/backend/CarRentalERP.Api/Seed/SeedData.cs](/Users/drshk5/dev/car-rental-erp/src/backend/CarRentalERP.Api/Seed/SeedData.cs)
- [src/frontend/web/lib/config.ts](/Users/drshk5/dev/car-rental-erp/src/frontend/web/lib/config.ts)
- [src/frontend/web/components/ui](/Users/drshk5/dev/car-rental-erp/src/frontend/web/components/ui)
- [docs/architecture.md](/Users/drshk5/dev/car-rental-erp/docs/architecture.md)

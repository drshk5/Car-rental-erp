# Five-Tier Architecture

## Target structure

```text
src/
  backend/
    CarRentalERP.Api/              # Presentation tier
    CarRentalERP.Application/      # Application tier
    CarRentalERP.Domain/           # Domain tier
    CarRentalERP.Infrastructure/   # Infrastructure tier
    CarRentalERP.Shared/           # Shared tier
  frontend/
    web/
      app/
      components/
      lib/
      hooks/
      types/
```

## Backend rules

### 1. Presentation

- Hosts controllers and HTTP pipeline only.
- No domain logic.
- Talks to `Application`.

### 2. Application

- Implements use cases and orchestration.
- Owns DTOs, service interfaces, and validation entry points.
- Depends on `Domain` and `Shared`.
- Does not depend on `Presentation`.

### 3. Domain

- Owns entities, enums, value concepts, and core business rules.
- Has no dependency on framework/web concerns.

### 4. Infrastructure

- Implements repositories, storage providers, auth/token services, persistence wiring.
- Depends on `Application`, `Domain`, and `Shared`.

### 5. Shared

- Contains cross-cutting primitives used across layers.
- No business logic.

## Frontend rules

- `app/` defines routes and layouts.
- `components/` holds reusable UI and layout pieces.
- `lib/` holds API client, config, helpers.
- `hooks/` holds reusable state/data hooks.
- `types/` mirrors stable API contracts for the UI.

## Delivery sequence

1. Foundation
2. Security and identity
3. Master data modules
4. Booking workflow
5. Operational workflow
6. Reporting and deployment hardening

# Koperasi Tentera Onboarding API

A .NET 8 Web API implementing the Koperasi Tentera mobile onboarding design:

- **Flow A — New Customer Registration**: create account → mobile OTP → email OTP → privacy policy → create PIN → biometric preference → home.
- **Flow B — Migrate Existing User**: legacy IC lookup (masked contacts) → mobile OTP → email OTP (with a "change email" branch) → privacy policy → create PIN → biometric preference → home.

Built per [`KoperasiTentera-API-Execution-Plan.md`](../../../Downloads/KoperasiTentera-API-Execution-Plan.md) with a Clean Architecture layout, EF Core code-first migrations, FluentValidation, and a global exception handler that returns RFC 7807 `ProblemDetails` carrying stable error codes.

📖 **See [`SETUP-GUIDE.md`](./SETUP-GUIDE.md) for the full setup guide, database/seed data details, a step-by-step flow walkthrough, and the unit test suite.**

## Solution layout

```
KoperasiTentera.sln
├── KoperasiTentera.Domain            Entities & enums (Customer, OtpVerification, PrivacyPolicy, Banner)
├── KoperasiTentera.Application       DTOs, FluentValidation validators, service interfaces + implementations
├── KoperasiTentera.Infrastructure    EF Core AppDbContext, entity configurations, migrations, seed data
├── KoperasiTentera.API               Controllers, Swagger, global exception middleware, Program.cs
└── KoperasiTentera.Application.Tests xUnit + Moq + MockQueryable.Moq unit tests for the Application services
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or a newer SDK that can target `net8.0`)
- A SQL Server instance — either the shared local SQL Server on `localhost,1433` (used across FMG repos) **or** SQL Server LocalDB (ships with Visual Studio, zero setup, no Docker/credentials required)
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

## Configuration

The connection string lives in [`KoperasiTentera.API/appsettings.json`](./KoperasiTentera.API/appsettings.json) (safe committed default, SQL Server LocalDB):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KoperasiTenteraDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

For local testing against the shared SQL Server (`localhost,1433`), a **new, dedicated database** (`KoperasiTenteraDb` — separate from `CMS`) is used. Put the real connection string in `KoperasiTentera.API/appsettings.Development.json` (git-ignored — copy from the committed [`.example`](./KoperasiTentera.API/appsettings.Development.json.example) template and fill in your password):

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=localhost,1433;Initial Catalog=KoperasiTenteraDb;User ID=sa;Password=<your-local-sql-password>;TrustServerCertificate=True;MultipleActiveResultSets=True"
}
```

See [`SETUP-GUIDE.md`](./SETUP-GUIDE.md#2-connection-string-configuration) for the full explanation.

## Running locally

```bash
# from the LoginAPI folder
dotnet restore
dotnet build

# apply EF Core migrations (creates KoperasiTenteraDb + seed data on whichever server the active connection string points to)
dotnet ef database update --project KoperasiTentera.Infrastructure --startup-project KoperasiTentera.API

# run the API (also auto-applies pending migrations on startup)
dotnet run --project KoperasiTentera.API --urls "http://localhost:5199"
```

- Swagger UI: http://localhost:5199/swagger
- Base route: `http://localhost:5199/api/v1`

### Seed data

- One active `PrivacyPolicy` (version `1.0`).
- Two home-screen banners ("Oh My Cashback!", "New Shariah Savings").
- One legacy `ExistingUser`, **Mariam Abdul Rashid** (IC `880214566831`, mobile `+60123456675`, email `mariam.rashid@example.com`, `Status = PendingVerification`) so the migration flow is testable immediately. Masked as `•• •• ••• 6675` / `ma•••@•••••.com`, matching the design mockups.
- One already-**Active** demo customer, **Ali Zulkifli** (IC `900101011111`, PIN `111111`) so `POST /auth/pin-login` and `GET /home/{customerId}` can be exercised immediately without walking the full registration flow.

Defined in [`KoperasiTentera.Infrastructure/Persistence/SeedData.cs`](./KoperasiTentera.Infrastructure/Persistence/SeedData.cs) — see the [setup guide](./SETUP-GUIDE.md#4-seed-data) for details.

## Unit tests

`KoperasiTentera.Application.Tests` unit-tests the Application-layer services with mocked components (`Moq` + `MockQueryable.Moq` mocking `IAppDbContext`/`DbSet<T>`, plus mocked sibling services like `IOtpService`) — no database required:

```bash
dotnet test KoperasiTentera.Application.Tests
```

33 tests covering both happy paths and every stable error code. Full breakdown in [`SETUP-GUIDE.md`](./SETUP-GUIDE.md#7-unit-tests-mocked-components-no-database-required).

## Testing end-to-end

Use the [`KoperasiTentera.API.http`](./KoperasiTentera.API/KoperasiTentera.API.http) request collection (VS Code REST Client / Visual Studio `.http` support) to walk both flows, including:

1. Registration happy path: start → mobile OTP → email OTP → policy → PIN → biometric → home.
2. Duplicate IC → `409 ACCOUNT_ALREADY_EXISTS`.
3. Wrong OTP ×3 → `INCORRECT_OTP`, then a 4th attempt → `OTP_MAX_ATTEMPTS`; resending before the 120s cooldown → `RESEND_NOT_ALLOWED` (with `retryAfterSeconds`).
4. PIN mismatch → `UNMATCHED_PIN`.
5. Migration happy path with the seeded legacy user, including the change-email branch and masked-contact assertions.
6. PIN login for the now-active user.

> **Development-only convenience**: since no OTP/SMS/email provider is wired up, `otp/send` (and the auto-issued OTP from `registration/start` / `migration/start`) echoes the generated code back in the response body **only when `ASPNETCORE_ENVIRONMENT=Development`** (the default for `dotnet run`). This must never happen in non-Development environments.

## Response envelope & error codes

All responses use `{ success, data, error }`. Failures are also RFC 7807 `ProblemDetails` (`application/problem+json`) with a `code` extension the mobile client can key on:

`ACCOUNT_ALREADY_EXISTS`, `ACCOUNT_NOT_FOUND`, `INCORRECT_OTP`, `OTP_EXPIRED`, `OTP_MAX_ATTEMPTS`, `RESEND_NOT_ALLOWED`, `UNMATCHED_PIN`, `INVALID_STATE`, `VALIDATION_ERROR`.

## Business rules implemented

- OTP: 4 digits, 2-minute expiry, 120s resend cooldown, max 3 failed attempts before forcing a resend.
- PIN: 6 digits, hashed with BCrypt (never stored/logged in plain text), confirmation must match. Login is rate-limited: 5 consecutive wrong attempts locks the account for 15 minutes (`423 ACCOUNT_LOCKED`, with `retryAfterSeconds`), resetting on the next successful login.
- IC number: unique per customer, validated as a 12-digit Malaysian NRIC.
- Mobile: validated as `+60` E.164; Email: standard email format.
- Contact masking is always server-side (`•• •• ••• 6675`, `ma•••@•••••.com`) and only returned before OTP verification completes.
- Status state machine (`PendingVerification → MobileVerified → EmailVerified → PolicyAccepted → PinCreated → Active`) is enforced in the service layer — e.g. you cannot accept the policy before verifying email, or verify email OTP before mobile OTP.
- No authentication/authorization middleware and no third-party OTP provider, per the design's "no auth, simulated OTP" requirement.

## Baseline security hardening (OWASP Top 10)

- **CORS** is scoped to Development only ([Program.cs](./KoperasiTentera.API/Program.cs)) — no permissive `AllowAnyOrigin` policy applies outside dev.
- **Security response headers** (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Content-Security-Policy`, `Permissions-Policy`) are added on every response via [`SecurityHeadersMiddleware`](./KoperasiTentera.API/Middleware/SecurityHeadersMiddleware.cs), plus `UseHsts()` outside Development.
- **PIN login brute-force protection**: 5 failed attempts locks the account for 15 minutes (see above), mirroring the OTP max-attempts pattern.
- Still explicitly out of scope (per the design spec): no authentication/authorization tokens at all, so object-level access control (e.g. `GET /home/{customerId}`) relies entirely on the caller knowing the correct GUID. See the chat history / [`SETUP-GUIDE.md`](./SETUP-GUIDE.md) for the full OWASP Top 10 assessment.

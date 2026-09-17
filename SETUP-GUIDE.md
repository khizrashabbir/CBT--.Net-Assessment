# Koperasi Tentera Onboarding API — Setup Guide & Flow Explanation

This guide covers: environment setup, the database/seed data, a walkthrough of both onboarding flows, the error-code contract, and the unit test suite added for the Application layer.

---

## 1. Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (a newer SDK that can target `net8.0` also works)
- A SQL Server instance reachable from your machine — either:
  - **Option A (shared local SQL Server, used for this task)**: the same Dockerized SQL Server used by other FMG repos, reachable at `localhost,1433`.
  - **Option B (zero-setup fallback)**: SQL Server LocalDB (ships with Visual Studio), no Docker/credentials required.
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

## 2. Connection string configuration

Configuration is layered the standard ASP.NET Core way: `appsettings.json` (safe default, committed) → `appsettings.Development.json` (real/local values, **git-ignored**) → environment variables.

### appsettings.json (committed, safe default — SQL Server LocalDB)

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KoperasiTenteraDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### appsettings.Development.json (git-ignored — real local SQL Server)

A **new, dedicated database** (`KoperasiTenteraDb`) is used on the shared local SQL Server — it does **not** touch the `CMS` database used by other FMG projects:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=localhost,1433;Initial Catalog=KoperasiTenteraDb;User ID=sa;Password=<your-local-sql-password>;TrustServerCertificate=True;MultipleActiveResultSets=True"
}
```

> A committed template lives at [`KoperasiTentera.API/appsettings.Development.json.example`](./KoperasiTentera.API/appsettings.Development.json.example) — copy it to `appsettings.Development.json` and fill in your real local password (never commit the real value, following the same convention as `local.env` → `.env` in the other FMG repos).

`TrustServerCertificate=True` is required because modern SQL clients default to `Encrypt=True` and the local dev SQL Server doesn't present a trusted certificate — without it, the connection fails a TLS handshake.

## 3. Create the database

```bash
cd LoginAPI
dotnet ef database update --project KoperasiTentera.Infrastructure --startup-project KoperasiTentera.API
```

This creates the **new** `KoperasiTenteraDb` database (tables: `Customers`, `OtpVerifications`, `PrivacyPolicies`, `Banners`) on whichever server the active connection string points to, and applies the seed data below. The API also calls `dbContext.Database.Migrate()` on startup, so a fresh environment self-heals on first `dotnet run`.

## 4. Seed data

Defined in [`KoperasiTentera.Infrastructure/Persistence/SeedData.cs`](./KoperasiTentera.Infrastructure/Persistence/SeedData.cs) and applied via EF migration `HasData` (deterministic — same GUIDs/values every time `database update` runs):

| Entity | Details | Purpose |
|---|---|---|
| `PrivacyPolicy` | Version `1.0`, active | Backing data for `GET /privacy-policy` |
| `Banner` × 2 | "Oh My Cashback!", "New Shariah Savings" | Home dashboard banners |
| `Customer` — **Mariam Abdul Rashid** | IC `880214566831`, `ExistingUser`, `PendingVerification` | Lets you exercise the **Migrate Existing User** flow immediately. Masks to `•• •• ••• 6675` / `ma•••@•••••.com`, matching the design mockups. |
| `Customer` — **Ali Zulkifli** | IC `900101011111`, `NewCustomer`, **`Active`**, PIN `111111` | Lets you exercise `POST /auth/pin-login` and `GET /home/{customerId}` immediately, without walking the whole registration flow first. |

> PIN hashes in seed data are pre-computed BCrypt hashes (not hashed at seed time) so the EF model snapshot stays deterministic — `BCrypt.HashPassword` generates a new random salt on every call, which would make EF think the model changed on every build.

## 5. Flow walkthrough

### State machine (shared by both flows)

```
PendingVerification → MobileVerified → EmailVerified → PolicyAccepted → PinCreated → Active
```

Each transition is enforced in the service layer (`OtpService`, `RegistrationService`) — e.g. you cannot verify the email OTP before the mobile OTP, or create a PIN before accepting the privacy policy. Any out-of-order call returns `409 INVALID_STATE`.

### Flow A — New Customer Registration

1. `POST /auth/ic-lookup` — IC not found → client shows "Register now".
2. `POST /registration/start` — creates the `Customer` (`PendingVerification`) and **auto-issues** a mobile OTP (via `OtpService`, reused from #2 below). `409 ACCOUNT_ALREADY_EXISTS` if the IC is already taken.
3. `POST /otp/verify` (`Mobile`) — on success, `Customer.Status → MobileVerified`.
4. `POST /otp/send` (`Email`) then `POST /otp/verify` (`Email`) — on success, `Status → EmailVerified`.
5. `GET /privacy-policy` then `POST /registration/accept-policy` — stamps consent + version → `Status → PolicyAccepted`.
6. `POST /registration/create-pin` — mismatch → `400 UNMATCHED_PIN`; match → PIN hashed with BCrypt → `Status → PinCreated`.
7. `POST /registration/biometric` — records the Enable Now/Maybe Later choice → `Status → Active` (onboarding complete).
8. `GET /home/{customerId}` — "Hello, {FirstName}" + banners. `POST /auth/pin-login` now works.

### Flow B — Migrate Existing User

Same OTP/policy/PIN/biometric mechanics as Flow A, entered differently:

1. `POST /auth/ic-lookup` or `POST /migration/start` — finds the **legacy** `Customer` (`CustomerType = ExistingUser`, not yet `Active`) and returns **masked** contact details (never the real mobile/email) + auto-issues the mobile OTP.
2. `POST /otp/verify` (`Mobile`) → `MobileVerified`.
3. Optional: `POST /migration/change-email` — updates the email and re-issues the email OTP (the "Change Email Address" link on the design).
4. `POST /otp/verify` (`Email`) → `EmailVerified`.
5. Same `accept-policy` → `create-pin` → `biometric` steps as Flow A → `Status → Active` (migration complete).

### OTP & PIN rules (enforced in `OtpService` / `RegistrationService`)

- OTP is 4 digits, expires after 2 minutes, 120-second resend cooldown (`429 RESEND_NOT_ALLOWED` with `retryAfterSeconds` if you resend early).
- 3 wrong attempts → each returns `400 INCORRECT_OTP`; a 4th attempt (without resending) → `400 OTP_MAX_ATTEMPTS`.
- PIN is 6 digits, hashed with BCrypt, never logged/stored in plain text; mismatch between `pin`/`confirmPin` → `400 UNMATCHED_PIN`.
- `otp/send` (and the auto-issued OTP from `registration/start` / `migration/start`) only echoes the generated code in the response **when `ASPNETCORE_ENVIRONMENT=Development`** — see `IAppEnvironment`/`AppEnvironment`.

### Error codes

`ACCOUNT_ALREADY_EXISTS` · `ACCOUNT_NOT_FOUND` · `INCORRECT_OTP` · `OTP_EXPIRED` · `OTP_MAX_ATTEMPTS` · `RESEND_NOT_ALLOWED` · `UNMATCHED_PIN` · `INVALID_STATE` · `VALIDATION_ERROR`

All are returned as RFC 7807 `ProblemDetails` (`application/problem+json`) with a `code` extension, via [`ExceptionHandlingMiddleware`](./KoperasiTentera.API/Middleware/ExceptionHandlingMiddleware.cs).

## 6. Running the API

```bash
dotnet run --project KoperasiTentera.API --urls "http://localhost:5199"
```

- Swagger UI: http://localhost:5199/swagger
- Base route: `http://localhost:5199/api/v1`
- Request collection: [`KoperasiTentera.API.http`](./KoperasiTentera.API/KoperasiTentera.API.http) walks both flows end-to-end (VS Code REST Client / Visual Studio).

## 7. Unit tests (mocked components, no database required)

Project: [`KoperasiTentera.Application.Tests`](./KoperasiTentera.Application.Tests) (xUnit + Moq + FluentAssertions + [MockQueryable.Moq](https://github.com/romantitov/MockQueryable)).

**Why mocked, not a real database:** every Application-layer service depends only on the `IAppDbContext` interface (see [`IAppDbContext.cs`](./KoperasiTentera.Application/Interfaces/IAppDbContext.cs)) and sibling service interfaces (e.g. `RegistrationService` depends on `IOtpService`). Tests build a `Moq`-based fake of `IAppDbContext` — no SQL Server, no LocalDB, fully isolated and fast (33 tests run in under a second).

### Test helper: `MockAppDbContextFactory`

[`TestHelpers/MockAppDbContextFactory.cs`](./KoperasiTentera.Application.Tests/TestHelpers/MockAppDbContextFactory.cs) builds a `Mock<IAppDbContext>` backed by plain `List<T>` instances, using `MockQueryable.Moq`'s `BuildMockDbSet()` so the async LINQ operators the services call (`FirstOrDefaultAsync`, `AnyAsync`, `ToListAsync`, ...) work against an in-memory list instead of a real `DbSet<T>`/database:

```csharp
Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(
    customers: new List<Customer> { existingCustomer },
    otpVerifications: new List<OtpVerification> { existingOtp });

OtpService sut = new(dbContext.Object, CreateEnvironment().Object);
```

`Customers.Add(...)` / `OtpVerifications.Add(...)` calls are captured via an optional `addedCustomers` / `addedOtpVerifications` list passed to the factory, so tests can assert exactly what a service tried to persist without a real `SaveChangesAsync`.

### Test coverage (36 tests)

| Class | Scenarios covered |
|---|---|
| `OtpServiceTests` | customer not found; new OTP issued (+ code only when `ReturnOtpInResponse` is true); resend within cooldown → `RESEND_NOT_ALLOWED`; resend after cooldown invalidates the old OTP; verify with no active OTP → `OTP_EXPIRED`; max attempts reached → `OTP_MAX_ATTEMPTS`; expired OTP → `OTP_EXPIRED`; wrong code increments attempts → `INCORRECT_OTP`; correct mobile OTP advances status; email OTP before mobile → `INVALID_STATE`; correct email OTP advances status |
| `RegistrationServiceTests` | duplicate IC → `ACCOUNT_ALREADY_EXISTS` (and OTP never sent); new IC creates customer + auto-issues mobile OTP; accept-policy before email verified → `INVALID_STATE`; accept-policy stamps consent + version; PIN mismatch → `UNMATCHED_PIN`; matching PIN is BCrypt-hashed and advances status; biometric before PIN created → `INVALID_STATE`; biometric completes onboarding (`Active`) |
| `AuthServiceTests` | IC not found; active customer → no masked contacts; legacy/not-yet-active customer → masked contacts; PIN login for non-Active customer → `ACCOUNT_NOT_FOUND`; wrong PIN → `UNMATCHED_PIN`; correct PIN → customer summary; already-locked account → `ACCOUNT_LOCKED` even with the correct PIN; 5th consecutive failure locks the account; correct PIN after prior failures resets the failed-attempt counter |
| `MigrationServiceTests` | legacy customer not found → `ACCOUNT_NOT_FOUND`; already-migrated customer → `INVALID_STATE`; migration start returns masked contacts + issues mobile OTP; change-email on active customer → `INVALID_STATE`; change-email updates email + re-issues email OTP |
| `HomeServiceTests` | customer not found → `ACCOUNT_NOT_FOUND`; greeting uses first name only, banners filtered to `IsActive` and ordered by `SortOrder` |

### Running the tests

```bash
dotnet test KoperasiTentera.Application.Tests
```

Expected output:

```
Passed!  - Failed: 0, Passed: 36, Skipped: 0, Total: 36
```

## 8. OWASP Top 10 (2021) assessment

| # | Category | Status | Notes |
|---|---|---|---|
| A01 | Broken Access Control | ❌ Gap (by design) | No auth tokens/sessions at all — an explicit spec requirement. Any caller who knows/guesses a `customerId` GUID can call any endpoint for it. |
| A02 | Cryptographic Failures | ✅ Mostly good | PIN hashed with BCrypt, never logged/stored plain; real DB credentials git-ignored. `TrustServerCertificate=True` is acceptable for local dev only. |
| A03 | Injection | ✅ Good | 100% EF Core LINQ — no raw SQL. FluentValidation constrains every input before it reaches a query. |
| A04 | Insecure Design | ✅ Addressed | State machine prevents skipping onboarding steps; OTP has expiry/cooldown/max-attempts; PIN login now has a 5-attempt/15-minute lockout (`ACCOUNT_LOCKED`), matching the OTP pattern. |
| A05 | Security Misconfiguration | ✅ Addressed | Swagger only in Development; CORS scoped to Development only; security headers + HSTS added (see below). |
| A06 | Vulnerable/Outdated Components | ✅ Good | All packages current at time of writing (EF Core 8.0.11, FluentValidation 11.11, BCrypt.Net-Next 4.0.3). No automated dependency scanning configured. |
| A07 | Identification & Authentication Failures | ❌ Gap (by design) | Same root cause as A01 — no authentication mechanism exists (explicit spec requirement). PIN brute-force is now mitigated (see A04). |
| A08 | Software/Data Integrity Failures | ✅ N/A | No untrusted deserialization; EF migrations are checked in and deterministic. |
| A09 | Security Logging & Monitoring | ⚠️ Partial | Exceptions are logged with stable codes, never leaking stack traces to clients; no dedicated security audit trail (e.g. failed-login alerting) beyond app logs. |
| A10 | SSRF | ✅ N/A | The API makes no outbound calls to user-supplied URLs. |

### What was fixed as a direct result of this review

1. **CORS** — the permissive `AllowAnyOrigin/AllowAnyHeader/AllowAnyMethod` policy now only applies `if (app.Environment.IsDevelopment())` in [Program.cs](./KoperasiTentera.API/Program.cs); it no longer runs unconditionally.
2. **PIN login brute-force protection** — `Customer` gained `FailedPinAttempts` / `PinLockedUntilUtc` (migration `AddPinLockoutFields`). [`AuthService.PinLoginAsync`](./KoperasiTentera.Application/Services/AuthService.cs) now locks the account for 15 minutes after 5 consecutive wrong PINs, returning `423 ACCOUNT_LOCKED` with `retryAfterSeconds` — even a *correct* PIN is rejected while locked. Resets to 0 on the next successful login. Covered by 4 new unit tests in `AuthServiceTests`.
3. **Security headers + HSTS** — new [`SecurityHeadersMiddleware`](./KoperasiTentera.API/Middleware/SecurityHeadersMiddleware.cs) adds `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Content-Security-Policy`, and `Permissions-Policy` to every response; `app.UseHsts()` is added outside Development.

### Remaining gaps (explicit spec trade-off, not an oversight)

No authentication/authorization exists anywhere in this API (A01/A07) — every endpoint trusts the caller-supplied `customerId`/IC number. This was an explicit requirement of the execution plan ("no auth, no third-party services"). Before this API is used beyond a demo/training context, it would need: JWT or session-based auth, per-customer authorization checks on every `customerId`-scoped endpoint, and a security audit/monitoring pipeline (A09).


# M1 Stores — Architecture

Production e-commerce marketplace for shoes, handbags, cosmetics, jewelry and accessories.
This document explains **every architectural decision** before code is written.

---

## 1. High-level architecture

```
┌─────────────────┐         HTTPS/JSON          ┌──────────────────────┐
│  React SPA       │  ──────────────────────▶   │  ASP.NET Core 9 API  │
│  (Vercel)        │  ◀──────────────────────   │  (Render, Docker)    │
└─────────────────┘                             └──────────┬───────────┘
                                                           │ EF Core
                                        ┌──────────────────┼──────────────────┐
                                        ▼                  ▼                  ▼
                                  PostgreSQL          SMTP (Brevo)      Payments
                                  (Neon, prod)        email service     M-Pesa Daraja sandbox
                                  SQL Server                            + Stripe test mode
                                  (Docker, local dev)
```

**Why SPA + API instead of MVC?** The storefront and the admin dashboard share one API.
A separate REST API also lets us add a mobile app later without touching the backend,
and it produces Swagger documentation that doubles as a portfolio artifact.

**Why PostgreSQL in production but SQL Server locally?** SQL Server has no free cloud
tier; Neon PostgreSQL is free forever. EF Core abstracts the difference — the provider is
chosen by configuration (`Database:Provider`), so the same code runs on both. SQL Server
stays in `docker-compose` for local development to keep the CV-relevant skill real.

## 2. Backend — Clean Architecture

```
server/
├── M1Stores.sln
├── src/
│   ├── M1.Domain/          # Entities, enums, domain errors. Zero dependencies.
│   ├── M1.Application/     # Use cases, DTOs, interfaces (IRepository, IEmailService…),
│   │                       # validation. Depends only on Domain.
│   ├── M1.Infrastructure/  # EF Core DbContext, repositories, JWT, email, payments,
│   │                       # file storage. Implements Application interfaces.
│   └── M1.Api/             # Controllers, middleware, DI composition root, Swagger.
└── tests/
    └── M1.Tests/           # xUnit: unit tests for Application services + domain rules.
```

**Why Clean Architecture?** Dependencies point inward: `Api → Infrastructure → Application → Domain`.
Business rules never depend on EF Core or ASP.NET, which is what makes them unit-testable
without a database, and what lets us swap SQL Server ↔ PostgreSQL freely.

**Why Repository Pattern on top of EF Core?** EF's `DbContext` is already a unit of work,
but repositories (a) keep LINQ out of controllers, (b) give tests a seam to mock, and
(c) were explicitly requested. We use a generic `IRepository<T>` plus focused repositories
(e.g. `IProductRepository`) for query-heavy aggregates.

**Cross-cutting decisions**

| Concern | Choice | Why |
|---|---|---|
| Logging | Serilog → console (structured) | Render/Azure capture stdout; structured logs are queryable |
| Errors | `ProblemDetails` middleware | RFC 7807 standard; one place turns exceptions into safe JSON |
| Validation | FluentValidation | Declarative rules, auto-registered, testable in isolation |
| Auth | JWT access (15 min) + refresh token rotation | Short-lived tokens limit stolen-token damage |
| Google login | ID-token verification server-side | Never trust the client; server validates with Google |
| Passwords | ASP.NET Identity `PasswordHasher` (PBKDF2) | Vetted, salted, upgradeable |
| Images | Cloudinary free tier (prod) / local disk (dev) | Render's disk is ephemeral; Cloudinary CDN is free to 25GB |
| Email | Brevo SMTP free tier (300/day) | Real deliverable email without a credit card |
| IDs | `Guid` (v7 sequential) | Non-guessable public IDs; v7 keeps index locality |
| Money | `decimal(18,2)`, currency code column | Never floats for money |

## 3. Frontend architecture

```
client/
├── src/
│   ├── api/           # Typed API client (fetch wrapper + endpoints per feature)
│   ├── components/    # Reusable UI (Button, Card, Modal, Rating, …)
│   ├── features/      # Feature folders: auth/, catalog/, cart/, checkout/,
│   │                  # orders/, wishlist/, reviews/, admin/
│   ├── hooks/         # useAuth, useCart, useDebounce, useTheme…
│   ├── layouts/       # StorefrontLayout, AdminLayout, AuthLayout
│   ├── pages/         # Route components composing features
│   ├── stores/        # Zustand stores (auth, cart, theme)
│   └── types/         # Shared TypeScript models mirroring API DTOs
```

**Why Vite?** Fastest dev server, first-class TS, static output deploys anywhere.
**Why Zustand over Redux?** Cart/auth/theme are small global states; Zustand is ~1KB,
no boilerplate, and easy to persist to localStorage (cart survives refresh).
**Why TanStack Query?** Server state (products, orders) needs caching, retries and
pagination — hand-rolling that with useEffect is where bugs live.
**Dark/light mode:** Tailwind `dark:` classes driven by a `data-theme` attribute,
persisted, defaulting to `prefers-color-scheme`.

## 4. Environments & configuration

| | Local dev | Production |
|---|---|---|
| Database | SQL Server 2022 (docker-compose) | Neon PostgreSQL (free) |
| API | `dotnet run` / Docker | Render free web service (Docker) |
| Frontend | Vite dev server | Vercel (free) |
| Images | `/wwwroot/uploads` | Cloudinary (free) |
| Email | Console/log sink | Brevo SMTP (free) |
| Payments | — | M-Pesa Daraja **sandbox** + Stripe **test mode** |

All secrets come from environment variables (never committed). `appsettings.json` holds
safe defaults; `appsettings.Development.json` points at docker-compose services.

## 5. CI/CD

GitHub Actions on every push/PR: restore → build (warnings as errors) → test → build
frontend. Deploys: Render auto-deploys `main` via its GitHub integration; Vercel does the
same for the frontend. This keeps deployment config out of secrets-sensitive CI files.

## 6. Security checklist

- HTTPS everywhere (Render/Vercel terminate TLS)
- JWT signing key ≥ 256-bit from env var
- Refresh tokens hashed at rest, rotated on use, revocable
- Role-based `[Authorize(Roles = "Admin")]` on all admin endpoints
- FluentValidation on every write endpoint; EF Core parameterization stops SQLi
- CORS locked to the Vercel origin
- Rate limiting on auth endpoints (ASP.NET `RateLimiter`)
- Account enumeration resistance (identical responses for unknown emails)

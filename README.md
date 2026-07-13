# M1 Stores 🛍️

A production-grade e-commerce marketplace for **shoes, handbags, cosmetics, jewelry and accessories** — built with ASP.NET Core 9, React and Clean Architecture.

> 🚧 **In active development.** Built module by module — follow the roadmap below.

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | React 18 · TypeScript · Tailwind CSS 4 · Framer Motion · TanStack Query · Zustand |
| Backend | ASP.NET Core 9 · C# · Clean Architecture · Repository Pattern |
| Data | Entity Framework Core · SQL Server (local dev) · PostgreSQL (production) |
| Auth | JWT + refresh token rotation · Google Login · role-based authorization |
| Payments | M-Pesa Daraja (sandbox) · Stripe (test mode) |
| Infra | Docker · GitHub Actions CI · Render (API) · Vercel (frontend) · Neon (DB) |
| Observability | Serilog structured logging · global ProblemDetails error handling |

## Documentation

- 📐 [Architecture & decisions](docs/ARCHITECTURE.md)
- 🗃️ [Database schema & ER diagram](docs/DATABASE.md)
- 🔌 [API reference](docs/API.md) — interactive Swagger at `/swagger`

## Getting started

**Prerequisites:** .NET 9 SDK, Node 22+, Docker Desktop.

```bash
# 1. Database (SQL Server in Docker)
docker compose up -d db

# 2. API → http://localhost:5199 (Swagger at /swagger)
cd server/src/M1.Api
dotnet run --urls http://localhost:5199

# 3. Frontend → http://localhost:5173
cd client
npm install
npm run dev
```

Run tests:

```bash
cd server && dotnet test
```

## Roadmap

- [x] Module 0 — Architecture, Clean Architecture solution, React scaffold, Docker, CI
- [ ] Module 1 — Domain model, EF Core, migrations, seed data
- [ ] Module 2 — Auth: JWT, refresh tokens, email verification, password reset, Google login, roles
- [ ] Module 3 — Product catalog: search, filtering, pagination, image upload
- [ ] Module 4 — Cart, wishlist, checkout, orders, M-Pesa + Stripe payments
- [ ] Module 5 — Reviews, admin dashboard API, sales reports
- [ ] Module 6 — Storefront UI
- [ ] Module 7 — Admin dashboard UI
- [ ] Module 8 — Deployment: Render + Neon + Vercel

## Project structure

```
m1-stores/
├── client/                 # React + TypeScript SPA
├── server/
│   ├── src/
│   │   ├── M1.Domain/          # Entities & business rules (no dependencies)
│   │   ├── M1.Application/     # Use cases, DTOs, interfaces
│   │   ├── M1.Infrastructure/  # EF Core, JWT, email, payments
│   │   └── M1.Api/             # Controllers, middleware, Swagger
│   └── tests/M1.Tests/     # xUnit tests
├── docs/                   # Architecture, database, API docs
├── Dockerfile              # API container (multi-stage)
└── docker-compose.yml      # Local dev: SQL Server + API
```

## Author

**Mary Wainaina** — Software Developer · Founder, [WayneTech Studio](https://marywayne.github.io/Mary-Wainaina-Portfolio/)
📧 waynmary9@gmail.com · [GitHub](https://github.com/MaryWayne) · [LinkedIn](https://www.linkedin.com/in/mary-wainaina-dev)

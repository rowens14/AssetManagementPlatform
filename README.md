There needs to be a couple changes made to the readme file. Mainly being the project structure change. This layout is no longer accurate after integrating the actual db into the app rather than the in memory db.
---

## Project Structure

```
AssetManagement.sln
├── AssetManagement.Server/     ← ASP.NET Core 10 host + Web API + EF Core 9
│   ├── Controllers/            ← REST API (Auth, Assets, Assignments, Lifecycle, Licenses, Users, Audit)
│   ├── Data/
│   │   ├── AppDbContext.cs     ← EF Core DbContext with Fluent API, snake_case columns
│   │   ├── DbSeeder.cs         ← Seeds all initial data on first run
│   │   └── Entities.cs         ← EF Core entity models
│   ├── Migrations/             ← InitialCreate migration
│   ├── Program.cs              ← Minimal hosting, DI, seeding
│   └── appsettings.json        ← Connection string (Postgres 18, port 5432)
│
├── AssetManagement.Client/     ← Blazor WebAssembly 10 frontend
│   ├── Pages/                  ← 6 page components matching UI mockups
│   │   ├── LoginPage.razor     ← Screen 1: Login & RBAC
│   │   ├── AssetsPage.razor    ← Screen 2: Asset Inventory List
│   │   ├── AssetDetailPage.razor ← Screen 4: Detail with 4 tabs
│   │   ├── LicensesPage.razor  ← Screen 7: Software & Licenses
│   │   ├── AuditPage.razor     ← Screen 8: Immutable Audit Log
│   │   └── ReportsPage.razor   ← Screen 9: Reports
│   ├── Services/
│   │   ├── ApiService.cs       ← Typed HTTP client (no custom DateOnly converter — .NET 10 native)
│   │   └── AppState.cs         ← Client-side state + async API orchestration
│   ├── Shared/
│   │   ├── StatusBadge.razor
│   │   └── RoleBadge.razor
│   └── wwwroot/
│       ├── index.html
│       └── css/app.css         ← Full light theme matching mockups
│
└── AssetManagement.Shared/     ← DTOs shared by Client and Server
    └── Models/Dtos.cs
```

---
---

## Prerequisites

- **.NET 10 SDK** — `dotnet --version` should show `10.0.x`




## Setup

- Open project in visual studio. Make sure AssetManagement.Server is the project set to run. 

- When you run the project, server will initialize and the browser should load the login screen. 

- The default logins are still going to be used for testing. This is going to be finalized soon.

---



## Default Logins

| Username | Password | Role |
|---|---|---|
| admin | admin123 | Admin |
| manager | pass123 | Manager |
| normal user | view123 | Viewer |

---
**Role permissions:**
- **Admin** — full access including user management and all security/compliance fields
- **Manager** — can create, edit and assign assets; can view security fields
- **Normal User** — read-only access across all pages
## API Endpoints

| Method | Route | Notes |
|---|---|---|
| POST | `/api/auth/login` | Returns `LoginResponse` with user DTO |
| GET/POST/PUT/DELETE | `/api/assets` | CRUD — PUT/DELETE require `CanEdit` |
| GET/POST | `/api/assignments` | POST auto-deploys asset |
| PUT | `/api/assignments/{id}/return` | Sets end date |
| GET/POST | `/api/lifecycleevents` | POST updates asset status |
| GET/POST/PUT/DELETE | `/api/licenses` | Software license management |
| GET/POST | `/api/users` | User management |
| PUT | `/api/users/{id}/toggle` | Activate/deactivate |
| GET | `/api/sites` | Reference data |
| GET | `/api/departments` | Reference data |
| GET/POST | `/api/audit` | Immutable audit log |



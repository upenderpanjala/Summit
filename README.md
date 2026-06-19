# Summit VMS — Victim Management System

An enterprise-grade SaaS platform for law-enforcement victim records, built on
**ASP.NET Core 8 MVC**. It demonstrates a clean, layered architecture with
role-based access control where the **police hierarchy** and the **Home Minister**
can sign in but may only **view** victim details — every create/edit/delete path
is blocked for them, in both the web UI and the REST API.

---

## Tech stack

| Concern | Technology |
|---|---|
| Web framework | ASP.NET Core 8 MVC + Razor views |
| ORM / data | Entity Framework Core 8 |
| Database | SQL Server (LocalDB by default) |
| Identity | ASP.NET Core Identity (users + roles) |
| Web auth | Cookie authentication |
| API auth | JWT bearer tokens |
| Authorization | Role-based policies |
| UI | Bootstrap 5 + Bootstrap Icons (CDN) |
| API docs | Swagger / OpenAPI (Swashbuckle) |

---

## Roles & access model

| Role | Victims | Cases | Users |
|---|---|---|---|
| **Administrator** | full CRUD | full CRUD | manage |
| **Investigator** | full CRUD | full CRUD | — |
| **PoliceHierarchy** | **view only** | view only | — |
| **HomeMinister** | **view only** | — | — |

Access is enforced by two policies (see `Authorization/Policies.cs`):

- `ViewVictims` → all four roles (gates the list/details pages and API `GET`).
- `ManageVictims` → Administrator + Investigator only (gates every create/edit/delete
  page and API `POST`/`PUT`/`DELETE`).

So a `PoliceHierarchy` or `HomeMinister` user receives **HTTP 403** on any mutating
route, and the UI hides edit/delete controls entirely. Every victim view is written
to an immutable **audit log** so view-only access stays accountable.

---

## Project layout

```
Summit.VMS/
├─ Summit.VMS.sln
├─ Summit.VMS.http               # REST API smoke tests
└─ src/Summit.VMS/
   ├─ Program.cs                 # DI, Identity, JWT + cookie auth, policies, pipeline
   ├─ appsettings.json           # connection string + JWT settings
   ├─ Models/
   │  ├─ Entities/               # ApplicationUser, Victim, CaseRecord, PoliceStation, AuditLog
   │  └─ Enums/                  # AppRoles, PoliceRank, Gender, CaseType, CaseStatus
   ├─ Data/
   │  ├─ ApplicationDbContext.cs
   │  ├─ Configurations/         # EF Core fluent configs
   │  └─ DbSeeder.cs             # roles + demo users + sample data
   ├─ Authorization/Policies.cs
   ├─ DTOs/                      # API request/response records
   ├─ ViewModels/                # MVC form models
   ├─ Services/                  # Interfaces + implementations (Victim, Case, Token, Audit)
   ├─ Controllers/
   │  ├─ *.cs                    # MVC: Home, Account, Victims, Cases, Users
   │  └─ Api/                    # REST: Auth, Victims, Cases (JWT-protected)
   ├─ Views/                     # Razor + Bootstrap 5
   └─ wwwroot/css/site.css
```

---

## Getting started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server — **LocalDB** (ships with Visual Studio) works out of the box, or any
  SQL Server instance (edit the connection string).
- EF Core tools: `dotnet tool install --global dotnet-ef`

### 1. Configure
Edit `src/Summit.VMS/appsettings.json` if needed:
- `ConnectionStrings:DefaultConnection` — point at your SQL Server.
- `Jwt:Key` — **replace** with a long random secret (≥ 32 chars) before any real use.
- `SeedAdmin` — the bootstrap administrator account.

> Tip: keep secrets out of source control with user-secrets:
> `dotnet user-secrets set "Jwt:Key" "<your-long-random-secret>"`

### 2. Create the database schema (recommended: migrations)
```bash
cd src/Summit.VMS
dotnet ef migrations add InitialCreate
dotnet ef database update
```
If you skip this step the app still boots — on first run the seeder falls back to
`EnsureCreated()` — but migrations are the recommended path for evolving the schema.

### 3. Run
```bash
dotnet run --project src/Summit.VMS
```
On startup the app applies the schema and seeds roles, demo users, and sample data.
Browse to the HTTPS URL shown in the console (e.g. `https://localhost:5001`).
Swagger UI is at `/swagger` in Development.

---

## Demo accounts

> ⚠️ For local evaluation only. Remove or rotate these in `DbSeeder.cs` before
> deploying anywhere real.

| Role | Email | Password |
|---|---|---|
| Administrator | `admin@summit.gov` | `Admin#12345` |
| Investigator | `investigator@summit.gov` | `Invest#12345` |
| PoliceHierarchy (DGP) | `dgp@summit.gov` | `Police#12345` |
| HomeMinister | `minister@summit.gov` | `Minister#12345` |

Sign in as **minister** or **dgp** to see view-only behaviour: the victim list and
details are visible, but no create/edit/delete controls appear, and direct attempts
to reach those routes return *Access denied*.

---

## REST API quick reference

Authenticate first, then send the returned token as `Authorization: Bearer <token>`.

| Method | Endpoint | Policy |
|---|---|---|
| `POST` | `/api/auth/login` | anonymous |
| `GET` | `/api/auth/me` | any authenticated |
| `GET` | `/api/victims` | ViewVictims |
| `GET` | `/api/victims/{id}` | ViewVictims |
| `POST` | `/api/victims` | ManageVictims |
| `PUT` | `/api/victims/{id}` | ManageVictims |
| `DELETE`| `/api/victims/{id}` | ManageVictims |
| `GET` | `/api/cases` | ViewCases |
| `POST` | `/api/cases` | ManageCases |

Example:
```bash
# 1) get a token
curl -k -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"investigator@summit.gov","password":"Invest#12345"}'

# 2) call a protected endpoint
curl -k https://localhost:5001/api/victims \
  -H "Authorization: Bearer <token-from-step-1>"
```
See `Summit.VMS.http` for a full set of ready-to-run requests, including the
view-only 403 demonstration.

---

## Security notes for production

- Replace `Jwt:Key` and all seeded passwords; load secrets from a vault / env vars.
- Enforce HTTPS and HSTS (already wired for non-Development).
- Consider refresh tokens and shorter access-token lifetimes.
- Review the audit-log retention policy for your jurisdiction.
- The seeded demo users and sample victims should be removed before go-live.

---

## Notes on this build

### Recent additions

- **Brand colours.** Navy `#004B8E` and orange `#F68026` are applied as CSS
  variables in `wwwroot/css/site.css` (navbar, primary/accent buttons, badges,
  stat cards, the notification bell).
- **Document upload (inspector).** Investigators/Administrators can attach files
  (pdf, doc(x), xlsx, csv, txt, jpg, png — ≤ 10 MB) to a victim from the victim
  **Details** page. Files are stored outside the web root under
  `Storage/documents` and streamed back through an authorized download action, so
  view-only roles can read them but only managers can upload/delete. Configure the
  path and size limit under `Storage` in `appsettings.json`.
- **Mobile numbers.** Officers/officials now have a `Mobile` field (shown in the
  Users list and seeded for demo accounts); the victim's contact field is a mobile
  number.
- **Email + notifications on victim insert.** When a victim is created (via the UI
  *or* the REST API) the app records a global **Notification** shown to every
  signed-in user — a bell with a recent-count badge in the navbar, plus a
  `Notifications` page — and emails the oversight chain (all active
  Administrator / Investigator / PoliceHierarchy / HomeMinister accounts).

  Email delivery is configured under `Smtp` in `appsettings.json`. **Leave
  `Smtp:Host` empty** (the default) and messages are written as `.eml` files to
  `Storage/mail-drop` so the feature works without a live mail server — open those
  files in any mail client to inspect them. Set `Host`/`Port`/`User`/`Password`/
  `EnableSsl` to send through a real SMTP relay. Set `App:BaseUrl` so the links in
  emails point at your deployment.

 from a written specification (core stack + the
"police hierarchy and Home Minister are view-only on victim details" rule). The
sample victim **L99** reflects the reference label from that spec. If you have a
more detailed requirements document (exact victim fields, the precise rank ladder,
or the meaning of specific reference codes), align the entities in
`Models/Entities/` and the seeder accordingly.

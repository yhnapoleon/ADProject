## EcoLens README (Development / Deployment / Integration)

This repository contains the full EcoLens system (clients + services):

- **Web frontend**: `web/` (Vite + React + TypeScript)
- **Backend API**: `.NET/EcoLens.Api/` (.NET 8 Web API + Swagger + JWT + SQL Server)
- **AI food recognition service (optional)**: `VisionService/` (FastAPI)
- **Android app**: `mobile/`
- **Monitoring configs (optional)**: `monitoring/`

---

### Production (Deployed)

- **Web frontend (prod)**: `https://kind-coast-04498401e.6.azurestaticapps.net`
- **Backend API (prod)**: `https://ecolens-api-daa7a0e4a3d4d7e8.southeastasia-01.azurewebsites.net`
- **Swagger (prod)**: `https://ecolens-api-daa7a0e4a3d4d7e8.southeastasia-01.azurewebsites.net/swagger`

> Note: backend business endpoints are exposed under `/api/...`; most endpoints require `Authorization: Bearer <token>`.

---

### Run Locally (Recommended: One-Click)

#### Prerequisites

- **Node.js**: 18+ recommended (Vite 7 requires a relatively new Node version)
- **.NET SDK**: 8.0+
- **Database**: SQL Server (local or remote)
- Optional: **Python 3.10+** (only if you need to run `VisionService/`)

#### One-click start (PowerShell)

From the repository root:

```powershell
.\start-all.ps1
```

The script starts:

- **Backend**: `http://localhost:5133` (and opens Swagger at `/swagger`)
- **Frontend**: `http://localhost:5173`

If ports are already in use, the script will try to stop old processes occupying `5133/5173`.

---

### Run Locally (Manual)

#### 1) Start backend (.NET)

```powershell
cd .\.NET\EcoLens.Api
dotnet run --launch-profile http
```

After startup:

- Swagger: `http://localhost:5133/swagger`

#### 2) Start frontend (Vite)

The frontend uses the environment variable `VITE_API_URL` to specify the API server **origin**. Examples:

- Connect to **local backend**: `http://localhost:5133`
- Connect to **production backend (Azure)**: `https://ecolens-api-daa7a0e4a3d4d7e8.southeastasia-01.azurewebsites.net`

PowerShell example (only affects the current shell):

```powershell
cd .\web
npm install
$env:VITE_API_URL="http://localhost:5133"
npm run dev
```

> Tip: most frontend request paths start with `/api/...`. `VITE_API_URL` can be either `http://localhost:5133` or `http://localhost:5133/api` (the code normalizes to avoid duplicated `/api`).

You can also refer to `web/env.example` to prepare environment variables quickly.

---

### How to Connect to the Server (Frontend → Backend)

#### Connect to local backend (development)

- **Backend**: `http://localhost:5133`
- **Frontend**: set `VITE_API_URL=http://localhost:5133`

#### Connect to production backend (development integration / demo)

- **Backend**: `https://ecolens-api-daa7a0e4a3d4d7e8.southeastasia-01.azurewebsites.net`
- **Frontend**: set `VITE_API_URL=https://ecolens-api-daa7a0e4a3d4d7e8.southeastasia-01.azurewebsites.net`

> If you deploy the frontend build artifacts to a static site, make sure you inject `VITE_API_URL` **at build time** and rebuild with `npm run build` (Vite env vars are baked into the build output).

---

### Backend Key Configuration (Database / JWT)

#### Config file

Backend config is in `/.NET/EcoLens.Api/appsettings.json` (recommended: override secrets via **user-secrets** or environment variables):

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer` / `Jwt:Audience` / `Jwt:Key` / `Jwt:ExpirationMinutes`

---

### Database (Local Setup / Cloud Database Access)

This system uses **SQL Server** (via EF Core). You can choose:

- **Local database**: restore quickly using scripts in this repo (great for offline review/demo)
- **Cloud database (Azure SQL / remote SQL Server)**: backend connects directly via a connection string

> Important: the **web frontend never connects to the database directly**. It only talks to the backend API. The database is accessed by the backend only.

#### Option A: Set up a local database (recommended for offline review/demo)

The `Database/` folder contains the scripts required to restore locally:

- `Database/Schema.sql`: tables/indexes/foreign keys/migration history + some baseline seed data
- `Database/SeedData.sql`: demo seed data (includes a demo account)

Steps:

1) Install SQL Server (or LocalDB/SQL Express), connect using SSMS / Azure Data Studio  
2) Create an empty database:

```sql
CREATE DATABASE EcoLensDb;
GO
```

3) **Run scripts in order (important)**:
   - First run `Database/Schema.sql`
   - Then run `Database/SeedData.sql`

4) Point backend connection string to your local DB (recommended: user-secrets):

```powershell
dotnet user-secrets init --project ".NET/EcoLens.Api"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=EcoLensDb;Trusted_Connection=True;TrustServerCertificate=True;" --project ".NET/EcoLens.Api"
```

Common local connection string examples (adjust for your instance):

- LocalDB:
  - `Server=(localdb)\mssqllocaldb;Database=EcoLensDb;Trusted_Connection=True;TrustServerCertificate=True;`
- Default local instance:
  - `Server=.;Database=EcoLensDb;Integrated Security=true;TrustServerCertificate=True;`

**Demo account (available after running SeedData.sql)**:

- Email: `demo@ecolens.local`
- Password: `Demo123!`

#### Option B: Access a cloud database (Azure SQL / remote SQL Server)

If you want to connect to the cloud DB directly using SSMS / Azure Data Studio, you typically need:

- **Server address**: e.g. `xxx.database.windows.net` for Azure SQL (port `1433`)
- **Username/password**: obtain from the project owner
- **Network / firewall**: add your public IP to Azure SQL “Firewall and virtual networks” allowlist (sometimes also enable “Allow Azure services and resources to access this server”)
- **Encryption**: Azure SQL typically requires `Encrypt=True`

For the backend to use the cloud DB, simply set `ConnectionStrings:DefaultConnection` to the cloud connection string (recommended: user-secrets/env vars; do not commit credentials to the repo).

Azure SQL connection string template (replace user/password/db):

```text
Server=tcp:<your-server>.database.windows.net,1433;
Initial Catalog=<your-database>;
Persist Security Info=False;
User ID=<your-user>;
Password=<your-password>;
MultipleActiveResultSets=False;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

#### User-secrets (recommended for development)

```powershell
dotnet user-secrets init --project ".NET/EcoLens.Api"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=EcoLensDb;Trusted_Connection=True;TrustServerCertificate=True;" --project ".NET/EcoLens.Api"
dotnet user-secrets set "Jwt:Key" "<your secure random key (32+ chars)>" --project ".NET/EcoLens.Api"
```

#### EF Core migrations / database update (if needed)

```powershell
dotnet tool install --global dotnet-ef
dotnet ef database update -p ".NET/EcoLens.Api" -s ".NET/EcoLens.Api"
```

---

### Optional: Run VisionService (Food Image Recognition)

`start-all.ps1` will try to probe `http://localhost:8000/docs`. If it is not running, it will print a warning (recognition disabled), but the rest of the system still works.

Follow the script hints to start FastAPI under `VisionService/` (venv recommended).

---

### Troubleshooting

- **Frontend gets 404 or cannot reach backend**
  - Ensure `VITE_API_URL` is set and the backend is running at that address.
- **CORS errors (blocked cross-origin requests)**
  - Backend must allow the frontend origin (local: `http://localhost:5173`; production: your Azure Static Web Apps domain).
- **First request to Azure backend is slow**
  - This is a common cold-start behavior; frontend request timeout is set to 60s (see `web/src/utils/request.ts`).

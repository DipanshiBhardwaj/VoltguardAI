# Voltaguard-AI

An intelligent energy analytics platform with multi-tier file validation and local
agentic AI auditing. A consumer exports their smart-meter usage ledger from their
utility portal, uploads it, and the platform validates it, computes the bill against
tiered Time-of-Day tariff slabs, and a fully offline AI agent explains the result in
plain English. Nothing leaves the local machine.

IGNOU MCA major project (MCSP-232).

## Architecture (4 layers + AI agent + database)

| Component | Stack | Folder | Port |
|-----------|-------|--------|------|
| Database  | PostgreSQL 16 (Docker) | `docker-compose.yml` | 5432 |
| Backend   | .NET 9 Web API + EF Core, layered as Common / DAL / BLL / API | `backend/` | 5286 |
| AI Agent  | Python + LangGraph + Ollama | `ai-agent/` | (worker) |
| Frontend  | Vue 3 + Vite + Pinia + Tailwind | `frontend/` | 5173 |

The backend reference direction is strictly one-way: **API → BLL → DAL → Common**.

- **Common** — DTOs, requests, enums, constants shared across layers.
- **DAL** — EF Core entities, configurations, `VoltaguardDbContext`, repositories.
- **BLL** — ingestion (line-by-line validation), deterministic tiered billing, audit
  reports, and JWT issuance.
- **API** — JWT-secured controllers, exception middleware (RFC 7807), seeding.

The billing math is plain compiled C# — no AI is involved in computing a bill. When a
bill is computed, the backend files an `EnergyAuditReport` with status `Pending`. The
Python agent polls for pending reports, asks a local Ollama model to write a
plain-English explanation, and writes it back — so the two services run independently.

## Database: PostgreSQL 16

The backend and the AI agent both use one PostgreSQL 16 database with these
local-development credentials: user `voltaguard`, password `voltaguard_dev_pw`,
database `voltaguard`, port 5432. It can run either way; the app does not care which.

**Option A: Docker** (needs Docker Desktop with WSL 2):

```
docker compose up -d          # then `docker ps` shows voltaguard-postgres as healthy
```

**Option B: native Windows install.** Install PostgreSQL 16 with the EDB installer
from postgresql.org/download/windows (keep port 5432), then in pgAdmin or SQL Shell
(psql), signed in as `postgres`:

```sql
CREATE USER voltaguard WITH PASSWORD 'voltaguard_dev_pw';
CREATE DATABASE voltaguard OWNER voltaguard;
```

The credentials match `docker-compose.yml`, so `backend/Voltaguard.Api/appsettings.json`
and `ai-agent/.env.example` work unchanged with either option.

## Quick start

```
cd backend/Voltaguard.Api && dotnet run                # terminal 1  (http://localhost:5286)
cd frontend && npm install && npm run dev              # terminal 2  (http://localhost:5173)
cd ai-agent && python main.py                          # terminal 3  (needs Ollama running)
```

The API applies EF migrations and seeds data on startup (roles, an admin, a demo
customer, the tariff slabs, and a demo meter).

The JWT signing key in `appsettings.json` is for local development only. A real
deployment would supply it (and the connection string) through environment
variables or .NET user-secrets.

Sign in at http://localhost:5173 with `customer@voltaguard.local` / `Customer@123`
(or the admin: `admin@voltaguard.local` / `Admin@123`).

## Using it

1. On the dashboard, pick the demo meter (or add, edit or delete your own).
2. Go to **Upload Ledger** and drop a `.csv` or `.txt` file with these 8 columns:
   `IntervalEnding, Voltage, ActivePower, PhaseCurrent, NeutralCurrent, PowerFactor, Frequency, CumulativeKWH`.
   Invalid rows are rejected with a reason (downloadable as an error log); valid
   rows are saved. Abnormal readings raise a red warning straight away.
3. Back on the dashboard, **Run audit** for a period to see the tiered bill breakdown.
   The dashboard also charts past bills and projects next month's bill.
4. **Consumption** charts power and voltage over time; **Anomaly Log** lists
   voltage sags/swells and phase/neutral current mismatches.
5. Open **Audit Reports**. Once the AI agent has run, each report shows its
   plain-English explanation.

### Timestamp format

`IntervalEnding` must be an ISO date and time:

| Accepted | Meaning |
|----------|---------|
| `2026-05-01 18:15` or `2026-05-01T18:15:00` | IST (no offset means Indian time) |
| `2026-05-01T12:45:00Z` | UTC |
| `2026-05-01T18:15:00+05:30` | the offset as written |

That is `YYYY-MM-DD`, a space or `T`, `HH:mm` or `HH:mm:ss`, then optionally `Z` or
an offset. Any other format (for example `01/05/2026 18:15` or `01-05-2026 18:15`) is
rejected with *"Timestamp must be in YYYY-MM-DD HH:mm format."*, because a date like
01/05/2026 could mean 1 May or 5 January. Readings are stored in UTC and shown in IST.

### Sample ledger and expected results

`samples/sample-ledger-2026-05.csv` holds one meter's 15-minute readings for
1–31 May 2026 (IST), including 4 deliberately invalid lines, a voltage sag on 12 May
(19:00–19:45) and a phase/neutral current mismatch on 20 May (08:00–08:30). It is
generated by `python tools/generate_sample_ledger.py` (standard library only,
fixed seed, so the file is identical every time).

Signed in as the demo customer, with the demo meter (Domestic Standard tariff):

1. **Upload the file.** Expect **2,976 accepted, 4 rejected**:

   | Line | Reason |
   |------|--------|
   | 235  | Expected 8 columns but found 4. |
   | 828  | Column 1 is not a valid date and time. |
   | 1529 | One or more numeric columns could not be read. |
   | 2338 | Voltage 999 is outside the plausible 100-300 V range. |

   The red anomaly warning shows **7 abnormal readings: 4 voltage sag, 3 current
   mismatch**. The dashboard card shows 2,976 readings stored and a red badge with 7.
2. **Upload the same file again.** Expect **0 accepted, 2,980 rejected** (2,976
   "Duplicate reading for this time interval." plus the same 4 invalid lines). The
   totals do not change.
3. **Run an audit** for period `2026-05`, from 1 May to 31 May. Expect **323.01 kWh**
   and **₹1,647.52**:

   | Slab (units) | Charge |
   |--------------|--------|
   | 0–125 at ₹4.00 + ₹70 fixed | ₹570.00 |
   | 125–300 at ₹5.25 | ₹918.75 |
   | 23.01 above 300 at ₹6.00 × 1.15 ToD = ₹6.90 | ₹158.77 |

4. **Projection** (dashboard): the last 30 days of readings (1 May 23:45 – 31 May
   23:45 IST) used 312.89 kWh, 10.43 kWh a day, so June 2026 (30 days) projects to
   **312.89 kWh, ₹1,577.69** (₹570 + ₹918.75 + 12.89 × ₹6.90).
5. **Anomaly Log** for the demo meter lists the 7 abnormal readings, newest first.

## AI agent

Requires [Ollama](https://ollama.com) with a model pulled (default `mistral`):

```
ollama pull mistral
cd ai-agent
python -m venv .venv && .venv/Scripts/activate      # Windows
pip install -r requirements.txt
python main.py
```

Configuration is read from `ai-agent/.env` (copy from `.env.example`).

## Tests

```
cd backend && dotnet test                     # xUnit unit tests (no database needed)
cd frontend && npm run test:e2e               # Playwright end-to-end tests
```

The backend tests cover the deterministic tariff math, the line-by-line ingestion
validation (timestamp formats, duplicates, file type, anomalies and tamper flag),
the bill projection, meter edit/delete, and repository owner-isolation / soft-delete
behaviour.

The Playwright tests start the Vite dev server and mock every `/api` call, so they
need neither the backend nor the database. Install the browser once with
`npx playwright install chromium`.

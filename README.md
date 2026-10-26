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

## Quick start

```
docker compose up -d                                  # PostgreSQL
cd backend/Voltaguard.Api && dotnet run                # terminal 1  (http://localhost:5286)
cd frontend && npm install && npm run dev              # terminal 2  (http://localhost:5173)
cd ai-agent && python main.py                          # terminal 3  (needs Ollama running)
```

The API applies EF migrations and seeds data on startup (roles, an admin, a demo
customer, the tariff slabs, and a demo meter).

Sign in at http://localhost:5173 with `customer@voltaguard.local` / `Customer@123`
(or the admin: `admin@voltaguard.local` / `Admin@123`).

## Using it

1. On the dashboard, pick the demo meter (or add your own).
2. Go to **Upload Ledger** and drop a CSV with these 8 columns:
   `IntervalEnding, Voltage, ActivePower, PhaseCurrent, NeutralCurrent, PowerFactor, Frequency, CumulativeKWH`.
   Invalid rows are rejected with a reason; valid rows are saved.
3. Back on the dashboard, **Run audit** for a period to see the tiered bill breakdown.
4. Open **Audit Reports** — once the AI agent has run, each report shows its
   plain-English explanation.

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
cd backend && dotnet test
```

Covers the deterministic tariff math, the line-by-line ingestion validation, and
repository owner-isolation / soft-delete behavior.

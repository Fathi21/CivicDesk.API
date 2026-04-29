# CivicDesk — API

CivicDesk is a council resident self-service portal for Cardiff Council. Residents can report local issues (potholes, missed bins, noise complaints, etc.), track their requests by reference number, and chat with an AI assistant that can pre-fill the report form on their behalf. Council administrators have a separate dashboard to view and update the status of all requests.

This repository contains the **ASP.NET Core Web API** backend and its test suite.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 / ASP.NET Core |
| Database | SQL Server (via Entity Framework Core 8) |
| Auth | JWT Bearer tokens (admin + resident roles) |
| AI Chat | Ollama (local LLM — gemma3:4b) |
| API Docs | Swagger / Swashbuckle |
| Tests | xUnit + Moq + EF InMemory |
| Container | Docker / Docker Compose |

---

## Project Structure

```
CivicDesk/
├── CivicDesk.API/
│   ├── Controllers/        # AuthController, ChatController, ServiceRequestsController
│   ├── Services/           # ServiceRequestService, ChatService
│   ├── Data/               # AppDbContext
│   ├── DTOs/               # Request/response records
│   ├── Models/             # Entities and enums
│   ├── Migrations/         # EF Core migrations
│   └── appsettings*.json
├── CivicDesk.Tests/
│   ├── Controllers/        # Controller unit tests
│   ├── Services/           # Service unit tests
│   └── Helpers/            # DbContextFactory
├── docker-compose.yml      # Full stack (DB + API + client)
└── docker-compose.dev.yml  # DB only (for local dev)
```

---

## Running the API

### Option 1 — Local Development

Requires: .NET 10 SDK, Docker (for the DB), Ollama running natively on your machine.

**1. Start the database**
```bash
docker compose -f docker-compose.dev.yml up -d
```

**2. Start Ollama natively (Apple Silicon — uses Metal GPU)**
```bash
ollama serve
ollama pull gemma3:4b
```

**3. Run the API**
```bash
cd CivicDesk.API
dotnet run --launch-profile http
```

API available at: `http://localhost:5136`
Swagger UI at: `http://localhost:5136/swagger`

---

### Option 2 — Full Docker Stack

Requires: Docker Desktop, Ollama running natively on your machine.

**1. Copy and configure environment variables**
```bash
cp .env.example .env   # edit DB_PASSWORD, JWT_SECRET
```

**2. Start everything**
```bash
docker compose up -d
```

| Service | URL |
|---|---|
| API | `http://localhost:5050` |
| Swagger | `http://localhost:5050/swagger` |
| Client | `http://localhost:5173` |

> **Note:** Ollama runs natively on your Mac for best performance on Apple Silicon.
> The API connects to it via `host.docker.internal:11434`.

---

## Environment Variables

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Gemma__BaseUrl` | Ollama base URL (e.g. `http://localhost:11434/v1/`) |
| `Gemma__Model` | Model name (e.g. `gemma3:4b`) |
| `Jwt__Secret` | JWT signing secret (min 32 chars) |
| `Jwt__Issuer` | JWT issuer |
| `Jwt__Audience` | JWT audience |
| `Jwt__ExpiresInHours` | Token lifetime in hours |

For local development these are set in `appsettings.Development.json`.
For Docker they are set in `docker-compose.yml` and the `.env` file.

---

## API Endpoints

### Auth
| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/auth/login` | Admin login | None |
| POST | `/api/auth/resident/login` | Resident login via email + reference | None |

### Service Requests
| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/servicerequests` | Submit a new request | None |
| GET | `/api/servicerequests` | List all requests | Admin |
| GET | `/api/servicerequests/{id}` | Get by ID | None |
| GET | `/api/servicerequests/reference/{ref}` | Get by reference number | None |
| GET | `/api/servicerequests/my` | Get current resident's requests | Resident |
| PATCH | `/api/servicerequests/{id}/status` | Update status + notes | Admin |

### Chat
| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/chat` | Send message to CivicAssist AI | None |

---

## Default Credentials

An admin account is seeded automatically on first run:

| Field | Value |
|---|---|
| Username | `admin` |
| Password | `Admin123!` |

---

## Running Tests

```bash
dotnet test CivicDesk.Tests/CivicDesk.Tests.csproj
```

36 tests covering all service methods and controller endpoints.

---

## Request Types

`Pothole` · `MissedBin` · `NoiseComplaint` · `PlanningQuery` · `StreetLighting` · `Other`

## Request Statuses

`Pending` → `InProgress` → `Resolved` / `Rejected`

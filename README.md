# Domus Aura

**SWE 4743 Object-Oriented Design | Spring 2026**

Full-stack smart home simulator. Control lights, fans, thermostats, and door locks through a slick web interface. Built with Angular + .NET + SQLite.

**Team:**

- Amelia -- Frontend (Angular, PrimeNG)
- Amy -- Backend (.NET, EF Core, API)

------

## Status

Backend core functionality is implemented, including:

- EF Core + SQLite persistence
- Device APIs (CRUD + commands)
- Simulation engine (thermostat ticking, ambient temperature)
- Scenes and command execution
- Server-Sent Events (event streaming)
- JWT authentication via Keycloak

Frontend integration and UI polish are in progress.

---

## Project Structure

```
domus-aura/
├── backend/
│   └── src/
│       ├── SmartHome.Api/            # Controllers, middleware, HTTP concerns only
│       ├── SmartHome.Domain/         # State machines, business logic, interfaces
│       └── SmartHome.Infrastructure/ # EF Core, repositories, persistence
├── frontend/                         # Angular 19 + PrimeNG
├── tests/
│   ├── SmartHome.Api.Tests/
│   └── SmartHome.Domain.Tests/
├── data/                             # SQLite db and seed data
├── bruno/                            # API test collection
└── docker-compose.yml
```

------

## Local Setup

### Prerequisites

- .NET 10 SDK
- Node.js 22+
- Angular CLI (`npm install -g @angular/cli`)
- Rider (recommended) or VS Code

### First Time Setup

**Backend -- create this file locally (not committed to git):**

```
backend/src/SmartHome.Api/appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=../../../data/smarthome.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

**Run backend:**

```bash
cd backend/src/SmartHome.Api
dotnet run
```

**Run frontend:**

```bash
cd frontend
npm install
ng serve
```

**Run tests:**

```bash
# Backend
dotnet test

# Frontend
cd frontend
ng test
```

------

## Authentication (JWT via Keycloak)

This application uses Keycloak as an external identity provider. The API is protected using JWT bearer tokens.

### Running the full stack

Run the application with Docker Compose:

```bash
docker compose up
```

This will start:
- Backend API
- Frontend
- Keycloak identity provider

### Keycloak Access

Open Keycloak admin console:

```text
http://localhost:8080
```

Admin credentials:

// Dev-only credentials
```text
username: admin
password: admin 
```

### Test User for Grading

Use the following account to log in via the frontend:

```text
username: demouser
password: TheAnswerIs42!
```

### API Authentication

All API endpoints require a JWT:

```http
Authorization: Bearer <access_token>
```

Unauthenticated requests return:

```text
401 Unauthorized
```

### Notes

- The frontend handles authentication using OpenID Connect Authorization Code Flow with PKCE.
- The backend validates JWTs issued by Keycloak.
- All API endpoints are protected and require a valid token.

------

## Branching Strategy

- `main` -- stable, deployable. Never push directly here.
- `dev` -- integration branch. All features merge here first.
- `feature/your-feature-name` -- cut from `dev`, PR back into `dev`.

**Workflow:**

```bash
git checkout dev
git pull origin dev
git checkout -b feature/your-feature-name
# do your work
git push -u origin feature/your-feature-name
# open PR into dev on GitHub
```

**Commit message convention:**

- `feat:` new feature
- `fix:` bug fix
- `chore:` setup/config
- `test:` adding tests

## Kanban Workflow

We use a GitHub Projects Sprint Board to track all work.
Find it under the Projects tab in this repo.

**Column definitions:**
| Column | Meaning |
|---|---|
| **Backlog** | Everything that needs to be done. Defined but not yet scheduled. |
| **Todo** | Picked up and ready to start this week. |
| **In Progress** | Actively being worked on. Should have an assignee. |
| **In Review** | PR is open and waiting for review. Don't start anything new until this is cleared. |
| **Done** | Merged to dev. Verified working. |

**The flow:**
1. Pick a card from **Todo** and assign yourself
2. Move it to **In Progress** and create your feature branch
3. When your PR is open, move the card to **In Review**
4. Other person reviews and approves the PR
5. Merge into `dev` and move the card to **Done**

**Rules:**
- Every piece of work gets a card before anyone starts it
- Cards in **In Progress** must have an assignee
- Don't leave cards in **In Review** -- reviewing open PRs is the highest priority
- Only move to **Done** after the PR is merged to `dev`, not just approved

------

## Device Types

All devices have base metadata:

```json
{
  "id": "uuid",
  "name": "Living Room Overhead",
  "location": "Living Room",
  "type": "Light | Fan | Thermostat | DoorLock"
}
```

### Light

- **States:** Off, On
- **Attributes:** brightness (10-100%), color (RGB)
- **Rules:**
    - Brightness and color can only be changed while On
    - Settings are retained when powered off and back on
    - Brightness clamped to [10, 100]

### Fan

- **States:** Off, On
- **Attributes:** speed (Low, Medium, High)
- **Rules:**
    - Speed can only be changed while On
    - Default speed is Medium on first power on
    - Speed retained when powered off and back on

### Thermostat

- **States:** Off, Idle, Heating, Cooling
- **Attributes:** mode (Heat/Cool/Auto), desiredTemp (60-80°F), ambientTemp
- **Rules:**
    - Only one thermostat allowed per location (API must enforce this)
    - Desired temp restricted to [60° - 80°]
    - While Heating: ambient increases 1°F every 5 seconds toward desired
    - While Cooling: ambient decreases 1°F every 5 seconds toward desired
    - Transitions to Idle when ambient == desired
    - Heat mode: can only heat (ambient < desired triggers Heating)
    - Cool mode: can only cool (ambient > desired triggers Cooling)
    - Auto mode: heats or cools based on ambient vs desired
    - Thermostat in Idle is NOT considered "on" for UI filtering

### Door Lock

- **States:** Locked, Unlocked
- **Rules:**
    - Latch device -- no power state, always considered "on"
    - No Off state

------

##  API Endpoints

Base URL: `http://localhost:5137/api`

> Here's a rough sketch of what we're going to need to endpoints.

------

### Devices

#### Get all devices

```
GET /api/devices
```

Optional query params:

- `location` -- filter by location string
- `type` -- filter by device type (Light, Fan, Thermostat, DoorLock)
- `state` -- filter by on/off

Response `200 OK`:

```json
[
  {
    "id": "uuid",
    "name": "Living Room Overhead",
    "location": "Living Room",
    "type": "Light",
    "isOn": true,
    "state": {
      "brightness": 75,
      "color": "#FF8800"
    }
  }
]
```

------

#### Get single device

```
GET /api/devices/{id}
```

Response `200 OK` -- same shape as above, single object.

Response `404 Not Found`:

```json
{
  "type": "https://domus-aura.com/problems/device-not-found",
  "title": "Device not found",
  "status": 404,
  "detail": "No device with id {id} exists."
}
```

------

#### Register a new device

```
POST /api/devices
```

Request body:

```json
{
  "name": "Kitchen Light",
  "location": "Kitchen",
  "type": "Light"
}
```

Response `201 Created` -- returns the newly created device.

Response `409 Conflict` (thermostat already exists in location):

```json
{
  "type": "https://domus-aura.com/problems/duplicate-thermostat",
  "title": "Duplicate thermostat",
  "status": 409,
  "detail": "A thermostat already exists in Kitchen."
}
```

Response `400 Bad Request` (invalid input):

```json
{
  "type": "https://domus-aura.com/problems/invalid-request",
  "title": "Invalid request",
  "status": 400,
  "detail": "Device type must be one of: Light, Fan, Thermostat, DoorLock."
}
```

------

#### Remove a device

```
DELETE /api/devices/{id}
```

Response `204 No Content`

Response `404 Not Found` -- same shape as above.

------

### Device Control

#### Send a command to a device

```
PUT /api/devices/{id}/state
```

**Light commands:**

```json
// Power on/off
{ "command": "setPower", "value": "on" }
{ "command": "setPower", "value": "off" }

// Set brightness (only valid when on)
{ "command": "setBrightness", "value": 75 }

// Set color (only valid when on)
{ "command": "setColor", "value": "#FF8800" }
```

**Fan commands:**

```json
{ "command": "setPower", "value": "on" }
{ "command": "setPower", "value": "off" }
{ "command": "setSpeed", "value": "Low" }
{ "command": "setSpeed", "value": "Medium" }
{ "command": "setSpeed", "value": "High" }
```

**Thermostat commands:**

```json
{ "command": "setPower", "value": "on" }
{ "command": "setPower", "value": "off" }
{ "command": "setMode", "value": "Heat" }
{ "command": "setMode", "value": "Cool" }
{ "command": "setMode", "value": "Auto" }
{ "command": "setDesiredTemperature", "value": 72 }
```

**Door Lock commands:**

```json
{ "command": "lock" }
{ "command": "unlock" }
```

Response `200 OK` -- returns updated device state.

Response `400 Bad Request` (invalid transition):

```json
{
  "type": "https://domus-aura.com/problems/invalid-transition",
  "title": "Invalid state transition",
  "status": 400,
  "detail": "Cannot set brightness while device is off."
}
```

------

### Locations

#### Set ambient temperature for a location

```
PUT /api/locations/{location}/ambient-temperature
```

Request body:

```json
{ "temperature": 78 }
```

Response `200 OK`:

```json
{ "location": "Living Room", "ambientTemperature": 78 }
```

------

### Device History

#### Get command history for a device

```
GET /api/devices/{id}/history
```

Response `200 OK`:

```json
[
  {
    "timestamp": "2026-04-08T14:23:00Z",
    "deviceId": "uuid",
    "operation": "setPower: on"
  }
]
```

------

### Simulation Settings

#### Set simulation speed

```
PUT /api/simulation/speed
```

Request body:

```json
{ "speedMultiplier": 5 }
```

Valid values: `1, 2, 5, 10`

Response `204 No Content`
#### Reset all devices

```
POST /api/simulation/reset
```

Response `204 No Content`
## Database

We're using EF Core + SQLite. Database file lives at `data/smarthome.db` (gitignored).

### Seed Data Requirements

The database must ship with pre-configured devices so the reviewer sees a populated dashboard on first run. Minimum required:

| Type        | Count | Locations                              |
| ----------- | ----- | -------------------------------------- |
| Lights      | 4+    | At least 2 different locations         |
| Fans        | 2+    | At least 2 different locations         |
| Door Locks  | 2+    | At least 2 different locations         |
| Thermostats | 2+    | One per location, at least 2 locations |

Seed data must run automatically on `docker compose up` -- no manual migration commands.

------

##  Design Patterns

These are required by the spec. Document each one here as you implement it.

| Pattern    | Location | Rationale                             |
| ---------- | -------- | ------------------------------------- |
| State      | TBD      | Device state machines                 |
| Factory    | TBD      | Device creation by type               |
| Strategy   | TBD      | Thermostat heating/cooling/auto modes |
| Repository | TBD      | Persistence abstraction               |
| Observer   | TBD      | SSE state change notifications        |
| Command    | TBD      | Device operations + scene execution   |
| Composite  | TBD      | Scene composition                     |

------

## Feature Checklist

### Core (mandatory)

- [ ] Light -- power, brightness, color, state retention
- [ ] Fan -- power, speed, state retention
- [ ] Thermostat -- power, modes, temp simulation, one-per-location invariant
- [ ] Door Lock -- lock/unlock, always on
- [ ] EF Core + SQLite persistence + seed data
- [ ] State machine dehydration/rehydration
- [ ] RESTful API + Swagger
- [ ] CORS configured
- [ ] RFC 9457 error responses
- [ ] Device command history
- [ ] Angular dashboard grouped by location
- [ ] PrimeNG component library
- [ ] Device filtering (On/Off/Location/Type)
- [ ] Inline device controls
- [ ] Simulation settings panel
- [ ] Register + remove devices
- [ ] Responsive/mobile layout
- [ ] Docker + docker-compose
- [ ] Bruno API collection
- [ ] Unit tests + integration tests
- [ ] Frontend tests

### Extra Credit

- [ ] ORM (EF Core) ← already doing this
- [ ] Server-Sent Events
- [ ] LLM/MCP natural language control
- [ ] JWT Authentication (Keycloak)
- [ ] Device Scenes
- [ ] CI/CD Pipeline

------

## Demo Videos

*Added upon completion.*

- Application Demo: TBD
- Architecture Tour: TBD

------

## Bruno Collection

API test collection is in `/bruno`. Open with [Bruno](https://www.usebruno.com/). Endpoints will be added as the API is built.
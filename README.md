# Domus Aura — Smart Home Simulator

**SWE 4743 — Object-Oriented Design | Spring 2026**

Domus Aura is a full-stack smart home simulator that allows users to monitor and control lights, fans, thermostats, and door locks through a web-based dashboard or natural language commands.

The system emphasizes clean architecture, SOLID principles, formal state machines, persistence, authentication, and Docker-based delivery.

---

## What Does the Application Do?

Domus Aura simulates a smart home environment where users can:

- View devices grouped by location
- Control lights, fans, thermostats, and door locks
- Filter devices by on/off state, location, and device type
- Register new devices and remove existing devices
- Simulate thermostat behavior using ambient temperature and simulation speed
- View device command history and activity
- Execute scenes that apply multiple device commands
- Receive real-time updates through Server-Sent Events
- Control devices with natural language using an LLM integration

---

## Tech Stack

| Area              | Technology                                              |
|-------------------|---------------------------------------------------------|
| Frontend          | Angular SPA (PrimeNG)                                   |
| Backend           | .NET 10 Web API                                         |
| Database          | SQLite                                                  |
| ORM               | Entity Framework Core (.NET)                            |
| Authentication    | Keycloak (JWT)                                          |
| Real-time Updates | Server-Sent Events                                      |
| API Testing       | Bruno                                                   |
| CI/CD             | GitHub Actions → AWS ECR → EC2 (Docker Compose and Caddy) |
| Infrastructure    | Terraform (one-time provisioning), AWS ECR, EC2, and S3   |
| LLM Integration   | OpenAI API and Model Context Protocol (MCP)               |

---

## Setup and Run Instructions

The application is intended to be clone-and-go. A reviewer should only need Docker and Docker Compose installed.

### Prerequisites

- Docker
- Docker Compose

No .NET SDK, Node.js, Angular CLI, or IDE is required to run the application with Docker.

---

## Run the Application

```bash
git clone https://github.com/Ousterhout-and-Bothered/domus-aura.git
cd domus-aura
docker compose up --build
```

The application should start with the frontend, backend API, Keycloak identity provider, and seeded SQLite database.

---

## Test Credentials

### Keycloak Admin

```text
username: admin
password: admin
```

### Application Test User

```text
username: demouser
password: TheAnswerIs42!
```

---

## Docker Setup

The application is fully containerized using Docker Compose.

### Access URLs

Once the containers are running, the application is available at:

| Service     | URL                              |
|-------------|----------------------------------|
| Frontend    | http://localhost:4200            |
| API Docs    | http://localhost:5137/scalar/v1  |
| Keycloak    | http://localhost:8080            |

### Services

The following services are started automatically:

- Backend API (.NET)
- Frontend (Angular)
- Keycloak (authentication provider)
- SQLite database (via volume)

### Data Persistence

- Application data is stored in the `data/` directory at the repo root
- Device state and history persist across restarts
- The SQLite database file is stored at:

```text
data/smarthome.db
```

Before running the application with Docker for the first time, ensure the local data directory exists:

```bash
mkdir -p data
```

If you encounter SQLite permission issues on Linux/macOS, run:

```bash
chmod 775 data
```

### Authentication

- Keycloak is preconfigured and imported automatically
- No manual setup is required
- Test users are available immediately after startup

### First Run Behavior

On initial startup, the application will:

- Apply database migrations
- Seed default devices and scenes
- Import Keycloak realm configuration

---

## Hosted Deployment (AWS)

A live instance is deployed to AWS and available at the URL below. Sign in with the demo credentials above to explore the full experience (including the LLM chat feature), which is fully wired up server-side.

| URL                                  |
|--------------------------------------|
| https://domus-aura.com               |

---

## CI/CD Pipeline

The repository ships a GitHub Actions pipeline (`.github/workflows/ci-cd.yml`) that lints, builds, tests, containerizes, and deploys the application end-to-end. Every push triggers CI; merges to `main` additionally trigger CD.

### Pipeline Flow

```text
git push
   │
   ├── backend job   ── dotnet restore → format → build → test
   ├── frontend job  ── npm ci → lint → build → test
   │
   └── (only on main, requires both jobs green)
       build-and-push ── AWS auth → ECR login → docker build/push (api and frontend)
       deploy         ── scp compose files → ssh to EC2 → docker compose pull → up -d
```

### Stack

| Layer                  | Technology                                                                                                  |
|------------------------|-------------------------------------------------------------------------------------------------------------|
| CI runner              | GitHub Actions (`ubuntu-latest`)                                                                            |
| Backend tooling        | .NET 10 SDK via `actions/setup-dotnet@v4`, NuGet cache via `actions/cache@v4`                               |
| Frontend tooling       | Node 22 via `actions/setup-node@v4` (built-in npm cache)                                                    |
| Container registry     | Amazon ECR (private repos in `us-east-1`, lifecycle policy: 5 tagged and 1-day untagged)                      |
| Image tagging          | `:latest` and `:${{ github.sha }}` for traceability                                                         |
| Transport              | `appleboy/scp-action` for compose files, `appleboy/ssh-action` for the deploy script                        |
| Production host        | Ubuntu 22 EC2 instance (IAM instance profile for ECR pull, no static keys on host)                          |
| Orchestration          | Docker Compose merging `docker-compose.yml` and `docker-compose.prod.yml`                                     |
| Reverse proxy          | Caddy 2-alpine (auto-Let's-Encrypt for `domus-aura.com`, `api.domus-aura.com`, `auth.domus-aura.com`)       |
| Identity               | Keycloak 26 and Postgres 16-alpine                                                                            |
| Persistence            | SQLite on a bind-mounted host volume (`/opt/domus-aura/data`); Caddy and Keycloak use Docker volumes        |
| Infrastructure-as-code | Terraform (ECR repos, IAM users/policies, EC2 instance profile); state in S3 with DynamoDB locking          |

### Infrastructure (Terraform)

Provisioning is one-time and lives outside the CI/CD pipeline in the `infrastructure/` directory:

```text
infrastructure/
├── ec2.tf            # EC2 instance and IAM instance profile (pull-only ECR access)
├── ecr.tf            # ECR repos and lifecycle policies (5 tagged, 1-day untagged)
├── iam.tf            # github-actions-deployer user and scoped push-only policy
├── outputs.tf
├── providers.tf      # AWS provider and region pinning
├── versions.tf       # Terraform and provider version constraints
└── .terraform.lock.hcl
```

State is stored in an S3 backend with DynamoDB-based locking, so runs from any machine see consistent state.

### Secrets

Long-lived AWS access keys for the deployer IAM user and the EC2 SSH key are stored in GitHub repository secrets. Production runtime secrets (`OPENAI_API_KEY`, Keycloak admin and DB passwords, all 32-byte values from `openssl rand -base64 32`) live only on the EC2 host in `/opt/domus-aura/.env` (chmod 600) and never travel through the pipeline.

---

## Optional LLM Natural Language Control

Domus Aura supports controlling devices through natural language ("turn off the bedroom lights," "set the thermostat to 70," "lock all the doors"). The integration is built on the **Model Context Protocol (MCP)**: the backend exposes each device capability as an MCP tool, and an OpenAI model calls those tools to fulfill the user's request. This means the LLM never touches the database or the device state machines directly and every action flows through the same validated service path the dashboard uses.

### On the deployed site

**The LLM is fully wired up at [https://domus-aura.com](https://domus-aura.com).** Any logged-in user can open the chat drawer and start issuing commands: no key setup required, no configuration to perform. The OpenAI API key is provisioned server-side as part of the production environment, so the feature works the moment you sign in with the demo credentials above.

### Running it locally

When running locally with `docker compose up`, the chat feature requires you to supply your own OpenAI API key (the deployed key is not committed to the repo). Add it to a `.env` file at the repo root:

```text
OPENAI_API_KEY=sk-...
```

The application still runs without this key! Every other feature works normally, but the chat drawer will return a configuration error if used.

### How it works

| Step                                                                                             | Component                              |
|--------------------------------------------------------------------------------------------------|----------------------------------------|
| 1. User types a natural-language command in the chat drawer                                      | `chat-panel.ts` (Angular)              |
| 2. Frontend POSTs to `/api/chat`                                                                 | `ChatApiService`                       |
| 3. API forwards the prompt to OpenAI's `/v1/responses` endpoint with the MCP server URL attached | `OpenAiChatService`                    |
| 4. OpenAI invokes one or more MCP tools to read state and issue commands                         | `Services/Chat/Mcp/Tools/*`            |
| 5. Each MCP tool delegates to `IDeviceService` — the same service layer the REST API uses        | `DeviceService`                        |
| 6. State changes propagate to the UI via SSE — the dashboard updates in real time as the LLM works | `DeviceEventBroker`                    |

Because the LLM uses the same service path as the dashboard, every natural-language action is automatically logged to the device history with full audit fidelity.

---

## Running Tests

### Backend Tests

```bash
dotnet test
```

The backend test solution covers all three projects: `SmartHome.Api.Tests`, `SmartHome.Domain.Tests`, and `SmartHome.Infrastructure.Tests`.

### Frontend Tests

```bash
cd frontend
npm install
npm test
```

---

## Bruno API Collection

The Bruno collection lives at the project root:

```text
/bruno
```

To use it:

1. Install Bruno from https://www.usebruno.com
2. Open `bruno/SmartHome` as a Bruno collection
3. Select the Local environment (top right). It is pre-configured to:

```text
http://localhost:5137
```

The collection includes requests for device CRUD, device commands, simulation controls, scenes, authentication, and error cases.

---

## Video Deliverables

### Application Demo Video

**Loom Link:** `[INSERT APPLICATION DEMO LOOM LINK]`

The demo video covers:

- Device dashboard grouped by location
- Filtering by on/off state, location, and device type
- Controlling lights, fans, thermostats, and door locks
- Registering a new device
- Removing an existing device
- Thermostat environment simulation
- Device command history or activity feed
- Extra credit features

### Architecture Tour Video

[Architecture Tour](https://www.loom.com/share/3de7a37434c642e384657991a70149c3)

The architecture video covers:

- Project structure and layer organization
- Controllers, services, repositories, and persistence
- SOLID principle examples
- State machine implementation
- Design patterns used in the codebase
- Dehydration and rehydration
- Validation and error handling
- Key architectural decisions

### CI/CD Demo Video

[Domus Aura CI/CD Pipeline Breakdown](https://www.loom.com/share/0a8c5f83f18442c69c88eb477cf67a37)

This video covers the GitHub Actions pipeline, including linting, build, tests, Docker image creation, and deployment if enabled.  

As for the sheer length of these videos: _"With great features comes great documentation."_ (Sorry in advance)

---

## Project Structure

```text
domus-aura/
├── .github/workflows/
│   └── ci-cd.yml                       # GitHub Actions pipeline
├── infrastructure/                     # Terraform — AWS provisioning (ECR, EC2, IAM)
│   ├── ec2.tf
│   ├── ecr.tf
│   ├── iam.tf
│   ├── outputs.tf
│   ├── providers.tf
│   └── versions.tf
├── backend/
│   └── src/
│       ├── SmartHome.Api/              # ASP.NET controllers, DI wiring, validators, MCP tools, Dockerfile
│       │   ├── Controller/
│       │   ├── Services/
│       │   │   └── Chat/Mcp/           # OpenAI integration and per-device MCP tool definitions
│       │   ├── Validation/             # FluentValidation rules for inbound DTOs
│       │   ├── Mapping/                # Domain → DTO mapping
│       │   ├── Middleware/             # Cross-cutting HTTP middleware (e.g., problem-details)
│       │   └── Contracts/              # Inbound/outbound DTOs
│       ├── SmartHome.Domain/           # Pure C#, no framework dependencies
│       │   ├── Device/                 # Device aggregate, state machines, commands, builders
│       │   │   ├── Commands/           # IDeviceCommand + concrete commands + builders + factory
│       │   │   ├── Light/              # Light aggregate + LightBuilder + IDimmable / IColorable
│       │   │   ├── Fan/                # Fan aggregate + FanBuilder + IFanControllable
│       │   │   ├── Thermostat/         # Thermostat + Heat/Cool/Auto strategies + provider
│       │   │   ├── DoorLock/           # DoorLock aggregate + ILockable
│       │   │   ├── StateMachine/       # Reusable generic StateMachine<TState>
│       │   │   ├── Registration/       # IDeviceFactory + IDeviceBuilder
│       │   │   └── Events/             # IDeviceEventNotifier (Observer contract)
│       │   ├── Scene/                  # CompositeCommand, DeviceScene, SceneAction, resolver
│       │   ├── Simulation/             # Simulation speed registry + interfaces
│       │   └── Common/                 # Guard clauses, value parsers, domain exceptions
│       ├── SmartHome.Infrastructure/   # EF Core, SQLite, SSE broker, simulation host
│       │   ├── Persistence/            # DbContext, EF configurations, migrations, seed
│       │   ├── Device/
│       │   │   ├── Repository/         # DeviceRepository (EF Core impl)
│       │   │   ├── Service/            # DeviceService (application service)
│       │   │   └── Events/             # DeviceEventBroker (Channel<T>-based pub/sub)
│       │   ├── Scene/                  # SceneRepository
│       │   └── Simulation/             # Simulation background service + clock
│       └── SmartHome/                  # Bruno API collection
├── frontend/                           # Angular SPA (PrimeNG, SCSS)
│   └── src/app/
│       ├── authentication/             # Keycloak PKCE flow, guards, HTTP interceptor
│       ├── device/                     # Device list, cards, detail, register/edit dialogs
│       ├── scene/                      # Scene list, editor, execution dialog
│       ├── history/                    # Filterable, paginated audit feed
│       ├── chat/                       # Slide-in LLM chat drawer
│       ├── simulation/                 # Speed controls, simulation header
│       └── shared/                     # Bottom tab bar (mobile nav), shared services
├── tests/
│   ├── SmartHome.Api.Tests/
│   ├── SmartHome.Domain.Tests/
│   └── SmartHome.Infrastructure.Tests/
├── keycloak/                           # Realm export, imported on first boot
├── data/                               # SQLite database file (bind-mounted into the API container)
├── docker-compose.yml                  # Base compose: api, frontend, keycloak, postgres
├── docker-compose.prod.yml             # Production overlay: Caddy, env wiring, image tags
├── Caddyfile                           # Reverse-proxy + automatic HTTPS configuration
└── README.md
```

The frontend uses Angular's `@angular/build:application` builder and exposes three build configurations: `development` (sourcemaps, no optimization), `production` (with file replacement for `environment.prod.ts`), and `docker` (optimized but without the file-replacement step, used by the production Dockerfile).

---

## Architecture Overview

```text
Frontend SPA
    ↓
Controller Layer
    ↓
Service Layer
    ↓
Repository Layer
    ↓
SQLite Database
```

### Layer Responsibilities

| Layer          | Responsibility                                                          |
|----------------|-------------------------------------------------------------------------|
| Frontend       | User interface, filtering, controls, authentication flow                |
| Controllers    | HTTP routing, request/response mapping, status codes                    |
| Services       | Business logic, state transitions, simulation, orchestration            |
| Domain         | Device models, state machines, commands, domain rules                   |
| Repositories   | Persistence abstraction                                                 |
| Infrastructure | EF Core, SQLite, external services, event publishing                    |

Controllers remain thin and delegate business logic to services. Services depend on repository abstractions rather than EF Core directly.

---

## Mobile Responsiveness

Domus Aura is designed mobile-first and adapts cleanly across phone, tablet, and desktop viewports.

- **Adaptive navigation.** A dedicated bottom tab bar (`shared/components/bottom-tab-bar.ts`) appears on phone-width viewports for primary navigation between Devices, Scenes, and History. At the `md` breakpoint and above, the bottom bar is hidden via CSS and the standard horizontal header navigation takes over.
- **Breakpoint-driven grids.** The device dashboard re-flows across phone, tablet, and desktop using explicit breakpoint rules: a single column on mobile, two columns at the `md` breakpoint, three columns at `lg`. Card layouts are sized to remain readable at every step rather than stretching arbitrarily wide.
- **Touch-friendly controls.** Tap targets meet a 44 px minimum on mobile. Brightness sliders, color pickers, and the thermostat dial are sized for touch first.
- **Safe-area aware.** The bottom tab bar respects iOS safe-area insets so it doesn't collide with the home indicator, and page content reserves bottom padding so the last row isn't obscured by the tab bar.
- **Modal dialogs and the chat drawer** adapt to narrow viewports with sizing and positioning suited to small screens, matching native mobile UX expectations.

---

## Component Library and Styling

The UI is built on **PrimeNG** for all standard interactive elements: cards,
dialogs, form inputs, dropdowns, datepickers, sliders, color pickers, buttons,
and toasts — combined with **PrimeFlex** for layout utilities and **PrimeIcons**
for iconography. Every form, dialog, and data-display surface in the application
is a PrimeNG component.

The custom SCSS in this repository covers three things and three things only:

1. **A theme layer** — design tokens for the Domus Aura brand (colors, typography,
   spacing) defined as CSS custom properties in `src/styles.scss`, with parallel
   light and dark mode definitions. PrimeNG's CSS variables are mapped to these
   tokens so the entire component library inherits the theme.

2. **A mobile-first dialog override** — a single `.aura-dialog` class that
   transforms PrimeNG's centered modal into a full-screen iOS-style sheet on
   phones (with safe-area insets for the home indicator) and reverts to a
   floating dialog at the `md` breakpoint and above.

3. **Domain-specific SVG visualizations** — the thermostat radial gauge, the
   fan blade, and the light bulb. These are device-specific affordances, not
   reimplementations of standard UI primitives.

No standard UI element is hand-rolled. Buttons are `p-button`. Form fields are
`p-inputtext`, `p-select`, `p-datepicker`. Dialogs are `p-dialog` styled through
the `aura-dialog` wrapper class.

---

## Device Categories

Devices fall into two categories:

- **Powered devices** have an explicit Off/On power state. Their functional substates (brightness, speed, mode) are only meaningful while On. Examples: Light, Fan, Thermostat.
- **Latch devices** are always energized and have no power state. Their state machine operates entirely at the substate level. Examples: Door Lock.

State transitions are validated within the domain layer rather than the controller, ensuring consistency regardless of how the device is accessed (API, scenes, or LLM).

### Light

Lights support:

- Power on/off
- Brightness from 10% to 100%
- RGB color
- State retention across power cycles

Brightness and color can only be changed while the light is on.

### Fan

Fans support:

- Power on/off
- Speed values: Low, Medium, High
- State retention across power cycles

Speed can only be changed while the fan is on.

### Thermostat

Thermostats support:

- Power on/off
- Modes: Heat, Cool, Auto
- Desired temperature from 60°F to 80°F
- Ambient temperature simulation
- Heating and cooling state transitions

A thermostat changes the ambient temperature by 1°F every 5 seconds at normal simulation speed until the ambient temperature reaches the desired temperature.

Only one thermostat is allowed per location.

### Door Lock

Door locks support:

- Lock
- Unlock

Door locks are latch devices. They do not have a power state and are always considered on for filtering.

---

## API Overview

Base URL:

```text
http://localhost:5137/api
```

### Devices

| Method | Endpoint                       | Description                                         |
|--------|--------------------------------|-----------------------------------------------------|
| GET    | `/api/devices`                 | List devices                                        |
| GET    | `/api/devices/{id}`            | Get one device                                      |
| POST   | `/api/devices`                 | Register a new device                               |
| PATCH  | `/api/devices/{id}`            | Rename or relocate a device                         |
| DELETE | `/api/devices/{id}`            | Remove a device                                     |
| PUT    | `/api/devices/{id}/state`      | Send a command to a device                          |
| GET    | `/api/devices/{id}/history`    | Get device command history                          |
| GET    | `/api/devices/history`         | Get history for all devices (filterable, paginated) |
| GET    | `/api/devices/events`          | Subscribe to device updates (SSE)                   |

**A note on PUT vs PATCH.** `PATCH /api/devices/{id}` performs a partial update of device metadata (name and location). `PUT /api/devices/{id}/state` issues a full-state command. The client provides the complete intended action and the server validates it against the device's state machine. PATCH is for the device record; PUT is for the device's behavior.

### Locations

| Method | Endpoint                                          | Description                |
|--------|---------------------------------------------------|----------------------------|
| PUT    | `/api/locations/{location}/ambient-temperature`   | Set ambient temperature    |

### Simulation

| Method | Endpoint                          | Description                         |
|--------|-----------------------------------|-------------------------------------|
| GET    | `/api/simulation`                 | Get simulation state                |
| GET    | `/api/simulation/allowed-speeds`  | List allowed simulation speeds      |
| PUT    | `/api/simulation/speed`           | Set simulation speed                |
| POST   | `/api/simulation/reset`           | Reset devices and simulation        |

### Scenes

| Method | Endpoint                       | Description           |
|--------|--------------------------------|-----------------------|
| GET    | `/api/scenes`                  | List scenes           |
| GET    | `/api/scenes/{id}`             | Get one scene         |
| POST   | `/api/scenes`                  | Create a scene        |
| PUT    | `/api/scenes/{id}`             | Update a scene        |
| DELETE | `/api/scenes/{id}`             | Delete a scene        |
| POST   | `/api/scenes/{id}/execute`     | Execute a scene       |

### Chat

| Method | Endpoint     | Description                       |
|--------|--------------|-----------------------------------|
| POST   | `/api/chat`  | Send a natural language command   |

---

## Example Device Command Payloads

### Power On or Off

```json
{
  "command": "setPower",
  "value": "on"
}
```

```json
{
  "command": "setPower",
  "value": "off"
}
```

### Set Light Brightness

```json
{
  "command": "setBrightness",
  "value": 75
}
```

### Set Light Color

```json
{
  "command": "setColor",
  "value": "#FF8800"
}
```

### Set Fan Speed

```json
{
  "command": "setSpeed",
  "value": "High"
}
```

### Set Thermostat Mode

```json
{
  "command": "setMode",
  "value": "Auto"
}
```

### Set Thermostat Desired Temperature

```json
{
  "command": "setDesiredTemperature",
  "value": 72
}
```

### Lock Door

```json
{
  "command": "lock"
}
```

### Unlock Door

```json
{
  "command": "unlock"
}
```

---

## Design Patterns

Domus Aura applies a coordinated set of GoF patterns across the domain and infrastructure layers. Each entry below identifies the participating types, the rationale for the pattern, and the reasoning behind the deviation.

### Behavioral Patterns

#### State

|                    |                                                                                                                                  |
|--------------------|----------------------------------------------------------------------------------------------------------------------------------|
| **Key types**      | `Device`, `PoweredDevice`, `Thermostat`, `Light`, `Fan`, `DoorLock`, `StateMachine<TState>`, `IStateMachine<TState>`             |
| **Where it lives** | `SmartHome.Domain/Device/StateMachine/`, plus per-device aggregates                                                              |

Each device aggregate owns a generic `StateMachine<TState>` configured at construction with an allowed-transitions table. The machine throws on illegal transitions rather than silently ignoring them, so invariants are guaranteed regardless of caller. The thermostat's `Off → Idle → Heating | Cooling → Idle → Off` transition graph is a particularly clean example: power, mode, and ambient-vs-desired temperature comparisons all flow through the same enforced state graph.

#### Strategy (Thermostat modes)

|                    |                                                                                                                              |
|--------------------|------------------------------------------------------------------------------------------------------------------------------|
| **Key types**      | `IThermostatModeStrategy`, `HeatModeStrategy`, `CoolModeStrategy`, `AutoModeStrategy`, `IThermostatStrategyProvider`, `ThermostatStrategyProvider` |
| **Where it lives** | `SmartHome.Domain/Device/Thermostat/`                                                                                        |

Each mode encapsulates its own decision logic ("should I heat? should I cool? should I idle?"). The thermostat tick loop selects the active strategy through a provider keyed on ThermostatMode, so the simulation loop never branches on mode. It simply asks the current strategy what to do. Adding a new mode, such as EcoModeStrategy, requires no changes to the thermostat itself, the tick loop, or any existing strategy.

#### Command

|                    |                                                                                                                                                                                                                  |
|--------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Key types**      | `IDeviceCommand`, `DeviceCommandBase`, `SetPowerCommand`, `SetBrightnessCommand`, `SetColorCommand`, `SetSpeedCommand`, `SetModeCommand`, `SetDesiredTemperatureCommand`, `LockCommand`, `UnlockCommand` |
| **Where it lives** | `SmartHome.Domain/Device/Commands/`                                                                                                                                                                              |

Every device action is encapsulated as a self contained command object that knows its target, its operation, and how to execute and report a result. This is what makes scenes, the LLM integration, and the REST API all share a single execution path because each is simply a different way of constructing and submitting commands. It also gives the audit log a uniform shape since every history entry corresponds to a command lifecycle.

#### Observer (real-time updates)

|                    |                                                                                                                              |
|--------------------|------------------------------------------------------------------------------------------------------------------------------|
| **Key types**      | `IDeviceEventNotifier` (domain contract), `DeviceEventBroker`, `DeviceEventNotifier`, `IDeviceEventPublisher`, `IDeviceEventStream` |
| **Where it lives** | `SmartHome.Domain/Device/Events/` (contracts), `SmartHome.Infrastructure/Device/Events/` (implementation)                    |

Application services publish a `DeviceChangedEvent` after every successful state change. The broker fans out to all currently-subscribed SSE clients via a per-subscriber bounded `Channel<DeviceChangedEvent>` (capacity 100, configured with `BoundedChannelFullMode.DropOldest`) so a slow consumer drops old events instead of blocking the producer. The contract lives in the Domain project so application services depend on the abstraction; the channel-based implementation is purely an Infrastructure concern.

### Creational Patterns

#### Factory and Builder (device construction)

|                    |                                                                                                                  |
|--------------------|------------------------------------------------------------------------------------------------------------------|
| **Key types**      | `IDeviceFactory`, `DeviceFactory`, `IDeviceBuilder`, `LightBuilder`, `FanBuilder`, `ThermostatBuilder`, `DoorLockBuilder` |
| **Where it lives** | `SmartHome.Domain/Device/Registration/` and per-device folders                                                   |

The factory holds a dictionary of builders keyed by `DeviceType`. Each builder is a self-contained DI component declaring `HandledType` and a `Build(name, location)` method. Adding a new device type requires writing one new aggregate and one new builder, and the factory picks it up automatically through DI without modification. This is the Open-Closed Principle made physical.

#### Factory / Builder (command construction)

|                    |                                                                                                                                       |
|--------------------|---------------------------------------------------------------------------------------------------------------------------------------|
| **Key types**      | `IDeviceCommandFactory`, `DeviceCommandFactory`, `IDeviceCommandBuilder`, `SetPowerCommandBuilder`, `SetBrightnessCommandBuilder`, etc. |
| **Where it lives** | `SmartHome.Domain/Device/Commands/`                                                                                                   |

The same factory / builder pattern is repeated for command construction. The factory groups builders by command name (case-insensitive) and asks each candidate `CanBuild(device)` before delegating. This handles the polymorphism of `setPower` working on lights, fans, and thermostats — but `setBrightness` only on lights — without a single `if (device is Light)` anywhere in the dispatch path.

### Structural Patterns

#### Composite (scenes)

|                    |                                                                                |
|--------------------|--------------------------------------------------------------------------------|
| **Key types**      | `CompositeCommand`, `DeviceScene`, `SceneAction`, `ResolvedScene`, `ISceneResolver` |
| **Where it lives** | `SmartHome.Domain/Scene/`                                                      |

A scene is a collection of `IDeviceCommand`s that executes its children in order, tolerates per-child failures, and produces one `CommandResult` per child. Scenes can target individual devices or *groups* (e.g., "all lights in the kitchen") that the resolver expands at execution time, so newly-added devices are automatically picked up.

> **A note on the deviation from canonical GoF Composite.** `CompositeCommand` does *not* implement `IDeviceCommand`. The canonical form would have a composite share the leaf interface so callers cannot tell them apart, but `IDeviceCommand.Execute()` returns a single `CommandResult` and a scene genuinely produces many (one per child action). Forcing the composite into the leaf interface would either require fabricating a misleading summary result or losing per-action audit detail. Single-device execution and scene execution have legitimately different return types and live in different controllers, so keeping them as distinct interfaces preserves Liskov Substitution rather than violating it. The "treat many as one" benefit of Composite is preserved at the level that matters for this domain at the scene boundary, not the leaf boundary.

### Cross-Cutting

#### Repository

|                    |                                                                                              |
|--------------------|----------------------------------------------------------------------------------------------|
| **Key types**      | `IDeviceRepository`, `ISceneRepository` (Domain); `DeviceRepository`, `SceneRepository` (Infrastructure) |
| **Where it lives** | Domain interfaces in `SmartHome.Domain/`, EF Core implementations in `SmartHome.Infrastructure/` |

Repositories sit at the Domain/Infrastructure boundary. Application services depend only on the interfaces, so swapping the persistence layer (e.g., to PostgreSQL or an in-memory test double) requires no changes outside the Infrastructure project. This is also what makes the unit tests possible without spinning up a database.

### Pattern Interaction

The patterns aren't isolated — they compose:

```text
HTTP request → DeviceCommandFactory builds an IDeviceCommand
            → Command executes against the device's StateMachine
            → Thermostat tick consults the active IThermostatModeStrategy
            → Successful change is published via IDeviceEventNotifier
            → DeviceEventBroker fans out to all SSE subscribers
            → DeviceRepository persists the new state
```

A scene execution is the same flow with `CompositeCommand` orchestrating step 2, and the LLM integration is the same flow with the MCP tool layer constructing commands at step 1. Three entry points, one execution spine.

---

## SOLID Principles

### Single Responsibility Principle

Controllers handle HTTP concerns only. Services handle business logic. Repositories handle persistence.

### Open-Closed Principle

New device behavior can be added by introducing new device classes, commands, or strategies without rewriting unrelated device logic.

### Liskov Substitution Principle

Device subtypes can be handled through shared abstractions without breaking expected behavior.

### Interface Segregation Principle

Device capabilities are separated by focused interfaces. For example, a door lock does not implement brightness or fan speed behavior.

### Dependency Inversion Principle

High-level services depend on abstractions such as repository interfaces, not concrete EF Core implementations.

---

## Design Decisions

### Why This Architecture?

The system uses a layered architecture (Controller → Service → Repository → Domain) to balance clarity, testability, and extensibility.

Key decisions include:

- **Separation of API and Domain Logic**
  - Business rules are enforced in the domain and service layers rather than controllers
  - This allows the same logic to be reused across API calls, scenes, and LLM-driven commands

- **Explicit State Machines for Devices**
  - Device behavior is modeled through controlled state transitions rather than simple property mutation
  - This prevents invalid operations and ensures consistent behavior across all interaction paths

- **Repository-Based Persistence**
  - Data access is abstracted behind repositories instead of being embedded in services
  - This isolates persistence concerns and simplifies testing and future database changes

- **Event-Driven Updates (SSE)**
  - Real-time updates are pushed to clients instead of requiring polling
  - This improves responsiveness and reduces unnecessary API traffic

- **Command-Based Device Operations**
  - Device actions are encapsulated as commands
  - This enables consistent execution, logging, and reuse in features like scenes and LLM integration

- **LLM Integration via MCP Tools**
  - Natural language commands are translated into structured operations using the Model Context Protocol
  - The LLM never bypasses domain rules; it routes through the same service layer as the dashboard

---

## Persistence

Domus Aura uses SQLite with Entity Framework Core.

Persistence includes:

- Device metadata
- Device state
- Device-specific attributes
- Thermostat ambient temperature
- Command history
- Scene definitions

The database is seeded automatically on startup and state survives application restart.

---

## Dehydration and Rehydration

Device state is persisted to SQLite and restored when the application starts.

This allows the system to rehydrate device state machines from persisted state, including:

- Light power, brightness, and color
- Fan power and speed
- Thermostat state, mode, desired temperature, and ambient temperature
- Door lock state
- Scene definitions
- Command history

---

## Validation and Error Handling

The API validates input at the HTTP boundary using FluentValidation and enforces business rules in the domain and service layers.

Error responses follow the RFC 9457 Problem Details format.

Example error response:

```json
{
  "type": "https://domus-aura.com/problems/invalid-transition",
  "title": "Invalid state transition",
  "status": 400,
  "detail": "Cannot set brightness while the light is off."
}
```

---

## And A Very Special Thanks...

...to Professor Adkisson for pushing us to be more ambitious, for refusing to let "good enough" be the goal, and for treating us like engineers long before we'd earned it. This may be our last class with you; but, we are walking out as better developers than we were when we first walked in. Our modules may be deep, but our gratitude is deeper. 

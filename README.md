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

| Area              | Technology                        |
|-------------------|-----------------------------------|
| Frontend          | Angular and PrimeNG               |
| Backend           | .NET 10 Web API                   |
| Database          | SQLite                            |
| ORM               | Entity Framework Core (.NET)      |
| Authentication    | Keycloak (JWT)                    |
| Real-time Updates | Server-Sent Events                |
| API Testing       | Bruno                             |
| CI/CD             | GitHub Actions and AWS Deployment |
| LLM Integration   | OpenAI API (.NET HttpClient)      |

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

## Access the Application (Local)

| Service | URL |
|---|---|
| Frontend | http://localhost:4200 |
| Backend API | http://localhost:5137 |
| API Docs | http://localhost:5137/scalar/v1 |
| Keycloak | http://localhost:8080 |

---

## Hosted Deployment (AWS)


| Service | URL                               |
|---|-----------------------------------|
| Frontend | https://domus-aura.com            |
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

### Services

The following services are started automatically:

- Backend API (.NET)
- Frontend (Angular)
- Keycloak (authentication provider)
- SQLite database (via volume)

### Data Persistence

- Application data is stored in the `/data` directory
- Device state and history persist across restarts

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

## Optional LLM Setup

The natural language control feature requires an OpenAI API key.

Set the key as an environment variable:

```bash
export OpenAI__ApiKey=your_api_key_here
```

Or place it in a local `.env` file if the Docker configuration supports environment file loading:

```text
OpenAI__ApiKey=your_api_key_here
```

The application can still run without this key, but LLM-based commands will not work unless the key is provided.

---

## Running Tests

### Backend Tests

```bash
dotnet test
```

### Frontend Tests

```bash
cd frontend
npm install
npm test
```

---

## Bruno API Collection

The Bruno collection is located in:

```text
/bruno
```

To use it:

1. Install Bruno from https://www.usebruno.com
2. Open the `/bruno` folder as a Bruno collection
3. Run requests against the backend API:

```text
http://localhost:5137/api
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

**Loom Link:** `[INSERT ARCHITECTURE TOUR LOOM LINK]`

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

**Loom Link:** `[INSERT CI/CD LOOM LINK IF APPLICABLE]`

This video covers the GitHub Actions pipeline, including linting, build, tests, Docker image creation, and deployment if enabled.

---

## Docker Architecture

The `docker-compose.yml` file starts the full application stack:

- Frontend SPA
- Backend API
- Keycloak identity provider
- SQLite-backed persistence

The application is seeded automatically on first run. No manual migrations or manual data imports should be required.

---

## Project Structure

```text
domus-aura/
├── backend/
│   └── src/
│       ├── SmartHome.Api/
│       ├── SmartHome.Domain/
│       └── SmartHome.Infrastructure/
├── frontend/
├── tests/
│   ├── SmartHome.Api.Tests/
│   ├── SmartHome.Domain.Tests/
│   └── SmartHome.Infrastructure.Tests/
├── bruno/
├── data/
├── keycloak/
├── docker-compose.yml
└── README.md
```

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

| Layer | Responsibility |
|---|---|
| Frontend | User interface, filtering, controls, authentication flow |
| Controllers | HTTP routing, request/response mapping, status codes |
| Services | Business logic, state transitions, simulation, orchestration |
| Domain | Device models, state machines, commands, domain rules |
| Repositories | Persistence abstraction |
| Infrastructure | EF Core, SQLite, external services, event publishing |

Controllers remain thin and delegate business logic to services. Services depend on repository abstractions rather than EF Core directly.

---

## Device Categories

Devices fall into two distinct categories:

- **Powered Devices**
    - Devices that have a power state (On/Off)
    - Examples: Lights, Fans, Thermostats

- **Latch Devices**
    - Devices that do not have a power state
    - Instead, they maintain a discrete state (e.g., Locked/Unlocked)
    - Example: Door Locks

Latch devices are always considered "on" for filtering purposes and are not affected by power state logic.

---

## Device Types

All device types are governed by formal state machines that define valid states and transitions.

Each device enforces its own rules at the domain level, preventing invalid operations such as:

- Changing brightness while a light is off
- Changing fan speed while the fan is off
- Invalid thermostat mode transitions

State transitions are validated within the domain layer rather than the controller, ensuring consistency 
regardless of how the device is accessed (API, scenes, or LLM).

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

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/devices` | List devices |
| GET | `/api/devices/{id}` | Get one device |
| POST | `/api/devices` | Register a new device |
| DELETE | `/api/devices/{id}` | Remove a device |
| PUT | `/api/devices/{id}/state` | Send a command to a device |
| GET | `/api/devices/{id}/history` | Get device command history |
| GET | `/api/devices/events` | Subscribe to device updates |

### Locations

| Method | Endpoint | Description |
|---|---|---|
| PUT | `/api/locations/{location}/ambient-temperature` | Set ambient temperature |

### Simulation

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/simulation` | Get simulation state |
| PUT | `/api/simulation/speed` | Set simulation speed |
| POST | `/api/simulation/reset` | Reset devices and simulation |

### Scenes

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/scenes` | List scenes |
| GET | `/api/scenes/{id}` | Get one scene |
| POST | `/api/scenes` | Create a scene |
| PUT | `/api/scenes/{id}` | Update a scene |
| DELETE | `/api/scenes/{id}` | Delete a scene |
| POST | `/api/scenes/{id}/execute` | Execute a scene |

### Chat

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/chat` | Send a natural language command |

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

| Pattern | Implementation / Key Classes                                                             | Rationale |
|---|------------------------------------------------------------------------------------------|---|
| **State** | `Device`, `PoweredDevice`, `Thermostat`, 'Light', 'Fan', 'DoorLock'                       | Models formal state machines where transitions are governed by rules (e.g., cannot set brightness if Off). |
| **Factory** | `DeviceFactory`, `DeviceCommandFactory`                                                  | Decouples device creation and command instantiation from the service layer, allowing for easy extension of new types. |
| **Strategy (Thermostat)** | `IThermostatStrategyProvider`, `HeatModeStrategy`, `CoolModeStrategy`, `AutoModeStrategy` | Encapsulates thermostat mode-specific behavior, allowing the simulation loop to remain independent of mode logic. |
| **Repository** | `IDeviceRepository`, `DeviceRepository`                                                  | Provides an abstraction over EF Core and SQLite, ensuring the domain and services remain independent of persistence details. |
| **Observer** | `DeviceEventBroker`, `IDeviceEventStream`, `IDeviceEventPublisher`                       | Implements a pub/sub mechanism for real-time updates; SSE clients subscribe to device state changes. |
| **Command** | `IDeviceCommand`, `SetPowerCommand`, `SetBrightnessCommand`                              | Encapsulates actions as objects, enabling consistent execution and tracking of command history. |
| **Composite** | `CompositeCommand`, `DeviceScene`                                                        | Allows a collection of commands (a Scene) to be treated as a single command, enabling complex automation sequences. |
| **Strategy (LLM)** | `IChatToolHandler`, `LightColorToolHandler`, `ThermostatTempToolHandler`                 | Enables dynamic selection of LLM tool processing logic based on the capability requested by the language model. |

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

- **LLM Integration via Tool Handlers**
  - Natural language commands are translated into structured operations using pluggable handlers
  - This avoids hardcoding logic and allows new capabilities to be added incrementally

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

The API does not expose stack traces, SQL errors, internal file paths, or implementation details in client-facing responses.




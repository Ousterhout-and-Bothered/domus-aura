# CLAUDE.md — Domus Aura (Smart Home Simulator)

## Project Overview

**Domus Aura** is a Smart Home Simulator built for SWE 4743 Object-Oriented Design. It is both a graded course project and a portfolio piece. The application simulates a smart home where users monitor and control household devices (lights, fans, thermostats, door locks) through a SPA front end backed by a RESTful API.

The course's emphasis — and therefore Claude's bias when proposing solutions — is on **rigorous OO design**: SOLID principles, formal state machines, clean architecture, and deliberately applied design patterns. Code that "works" but violates these principles is not acceptable here.

## Tech Stack

| Layer        | Technology                                           |
| ------------ | ---------------------------------------------------- |
| Front end    | Angular (latest stable) with PrimeNG                 |
| Back end     | .NET 10 / ASP.NET Core (Web API)                     |
| Persistence  | SQLite via Entity Framework Core (ORM extra credit)  |
| Architecture | Clean Architecture — Domain / Infrastructure / Api   |
| Tests        | xUnit (back end), AAA format                         |
| API tooling  | Scalar / OpenAPI, Bruno collection committed to repo |
| CI/CD        | GitHub Actions                                       |
| IDE          | JetBrains Rider                                      |

## Repository Structure

The solution file lives at the repository root. The three backend projects live under `backend/src/`, tests under `backend/tests/`, the SPA under `frontend/`, and the Bruno collection under `bruno/`.

```
domus-aura/
├── SmartHome.sln
├── docker-compose.yml
├── backend/
│   ├── src/
│   │   ├── SmartHome.Api/             # Controllers, middleware, DI wiring, Program.cs
│   │   ├── SmartHome.Domain/          # Entities, value objects, capability interfaces, repository interfaces, domain services
│   │   └── SmartHome.Infrastructure/  # EF Core DbContext, repository implementations, SSE broadcaster
│   └── tests/
│       └── SmartHome.Domain.Tests/    # xUnit, AAA format
├── frontend/                           # Angular + PrimeNG SPA
├── data/                               # SQLite db (gitignored), seed data
└── bruno/                              # Bruno collection
```

Front end and back end are **sibling directories** — never nest one inside the other.

## Architecture Rules

### Layering

The dependency direction is strict and one-way:

```
Controller → Service → Repository → Storage
```

- **Controllers** are thin. They handle HTTP routing, request/response mapping, status codes, and HTTP-level input validation. No business logic. Ever.
- **Services** contain business logic, state machine orchestration, and domain rules. They depend on repository **interfaces**, never on EF Core or SQL.
- **Repositories** abstract persistence behind interfaces in `SmartHome.Domain`. The `DbContext` is injected only into the repository implementations in `SmartHome.Infrastructure`.
- **Domain models** know nothing about HTTP, JSON, or the database.

If a proposed change would push a concern across one of these layer boundaries, stop and propose a different design.

### SOLID

Every class is held to SOLID. The most common violations to watch for in this codebase:

- **SRP** — controllers leaking business logic; services leaking persistence concerns.
- **OCP** — `switch` or `if` chains on device type. Adding a device type must not require editing existing infrastructure. The `DeviceFactory` was deliberately refactored to an `IDeviceBuilder` dispatch pattern via DI to eliminate this; do not regress it.
- **LSP** — device subtypes must honor the contract of their base type / interface.
- **ISP** — capability interfaces (`IPowerable`, `IDimmable`, `IColorable`, `ILockable`, `IThermostatControllable`) are intentionally fine-grained. A door lock must not implement a dimming interface.
- **DIP** — every dependency flows through an interface and is registered in the .NET DI container. **Service Locator is prohibited.** No `new` for services inside other services.

### Deep Modules

Abstractions follow Ousterhout's deep-module concept: a small, simple interface that hides significant complexity. If an interface is wider than the implementation, redesign it.

## Required Design Patterns

These patterns are mandatory for the project and must be implemented and documented in the README pattern catalog:

| Pattern                 | Where it lives                                               |
| ----------------------- | ------------------------------------------------------------ |
| **State**               | Each device's state machine — formal states, explicit transitions, invalid transitions rejected (not silently ignored) |
| **Factory**             | Device creation, currently via `IDeviceBuilder` dispatch (OCP-compliant variant of Factory) |
| **Strategy**            | Thermostat Heat / Cool / Auto modes — selected at runtime, no `if`/`switch` on mode inside thermostat logic |
| **Repository**          | All EF Core access wrapped behind `IDeviceRepository`, `ILocationRepository`, etc. |
| **Observer**            | SSE broadcaster — required by the SSE extra credit being pursued |
| **Builder**             | When complex construction emerges (e.g., scenes if pursued)  |
| **Composite + Command** | If/when the Scenes extra credit is pursued                   |

Patterns must solve real problems. Do not apply a pattern superficially just to claim it. When applying a pattern, also update the README pattern catalog with the class/file references and rationale.

## State Machines

- Each device type has a **formally defined** state machine.
- Invalid transitions are **rejected explicitly** (throw a typed domain exception). Never silently ignored.
- The state machine engine is **generic and reusable** — adding a new device type means defining states and transitions, not modifying the engine.
- State machines must support **dehydration / rehydration** so device state survives an application restart.

## Persistence

- SQLite via EF Core. Single-file DB lives in `data/smarthome.db` (gitignored).
- **Auto-migration and auto-seeding on startup are intentional and graded.** `docker compose up` must produce a fully seeded, ready-to-use application with no manual migration commands. Do not "fix" this by removing it.
- Seeding must be **idempotent** — re-running must not duplicate data.
- The `DbContext` is **never** injected into services or controllers. Only repositories see it.

## Validation & Error Handling

- **Never trust data from the browser.** All API inputs validated server-side regardless of any client-side validation.
- Validation library: **FluentValidation**. Controller-layer validation handles HTTP shape (returns 400 for malformed). Service-layer validation handles business rules (brightness 10–100, temperature 60–80°F, one thermostat per location, etc.).
- **Global error handler / middleware** for all exceptions. No ad-hoc `try`/`catch` scattered across controllers.
- Error responses follow **RFC 9457 Problem Details** format.
- **Never leak implementation details** to the client — no stack traces, no SQL errors, no class names. Log the full exception server-side; return a sanitized response.
- Use **typed domain exceptions** (e.g., `InvalidStateTransitionException`, `DuplicateThermostatException`) rather than raw `Exception` or `InvalidOperationException`. When wrapping an exception, always pass the original as `innerException` so the stack trace is preserved.

## C# Code Style

- **Primary constructors** by default for any class that takes constructor parameters — including exceptions, services, and classes with private readonly state.
- **Value objects** over primitive obsession. A domain concept like `DeviceId` should be a value object, not a raw `string` or `Guid`. Validate at construction; downstream code can then trust the type.
    - Note: `DeviceId` was created but **deferred** from integration pending resolution of EF Core rehydration concerns. Do not re-integrate without verifying EF Core can rehydrate it correctly.
- **Override `ToString`** on entities and value objects so log lines and debugger output are meaningful. Use `nameof()` in sealed subclasses; use `GetType().Name` only in abstract base classes that need to identify the runtime subtype.
- **Override `Equals` and `GetHashCode`** as a pair on entities and value objects. Equality must be stable — never base hash codes on mutable state.
- **XML doc comments** on all public interfaces and abstractions. Document the *contract*, not the implementation. Do not leak persistence concerns into interface documentation.
- Concise **inline comments** on non-obvious decisions, written as the code is being written.

## Testing

- **xUnit**, **AAA format** (Arrange / Act / Assert).
- Test project: `SmartHome.Domain.Tests
- Required coverage:
    - State machine transitions — every valid transition, every invalid transition rejected.
    - Boundary conditions — brightness (10, 100, out-of-range), temperature (60, 80, out-of-range), fan speed.
    - Service / domain logic in isolation from HTTP and persistence.
    - Factory / device creation produces correctly initialized devices.
    - Invariants — second thermostat in a location is rejected.
- Integration tests cover API contract, persistence round-trip (write → restart → read back), and thermostat simulation behavior over time.
- AI-generated tests are explicitly permitted by the spec.

## API Design

- RESTful conventions. Correct HTTP status codes (200, 201, 400, 401, 404, 409, 500).
- **Scalar / OpenAPI** served at runtime.
- **CORS** configured to allow the front-end origin.
- **Command history** — every device operation persisted; exposed via `GET /api/devices/{id}/history`.
- The Bruno collection in `bruno/` covers every endpoint with both success and error cases.

## Front End

- Angular + PrimeNG (theming already configured).
- Components are focused and single-responsibility.
- API calls live in **service classes**, not scattered across components.
- Mock data strategy: `DeviceService` returns `of(...)` observables until backend wiring is complete.
- Environment configuration via `src/environments/environment.ts` and `environment.prod.ts`. **Never hard-code the API URL.**
- Must be usable at 375px width (mobile-friendly).

## Extra Credit Status

- **ORM (EF Core)** — actively pursued and partially implemented.
- **SSE** — actively pursued. Architecture sketched: Domain owns event types and publisher/broadcaster interfaces; Infrastructure owns a singleton channel-based broadcaster; service layer publishes after persistence. Implementation not yet started.
- **JWT** — in progress
- **Scenes**, **LLM/MCP**, **CI/CD pipeline (deployment)** — not currently planned. CI/CD GitHub Actions YAML is ready for build/test only.

## Workflow & Collaboration

- GitHub repo owned by **emmyatri**. Teammate **Amy** owns the data/persistence layer. Be cautious about changes to EF Core configuration — coordinate rather than refactoring unilaterally.
- **Feature branches per contributor.** PRs target the teammate's branch during active collaboration, not `main` directly.
- Commit in **logical chunks** with meaningful commit messages — do not lump unrelated changes into one commit.
- When responding to an AI code review suggestion (e.g., from Copilot), evaluate the *observation* on its merits and rewrite or reject the *fix* if it would degrade the design. Preserving OCP, DIP, and clean abstractions takes precedence over a reviewer's suggested patch.

## Out-of-Scope "Best Practices" That Don't Apply Here

A few things that look like smells aren't, in this project's context:

- **Auto-migration and auto-seeding on startup** — intentional, graded. Don't remove.
- **SQLite-only persistence** — chosen deliberately. Don't suggest Postgres or SQL Server swaps.
- **Single-file DB at `data/smarthome.db`** — committed seed JSON exists; the DB itself is gitignored. Don't suggest committing the DB or removing the seed.

## When in Doubt

Prefer:

- pushing complexity *down* into implementations (deep modules) rather than leaking it upward,
- a typed domain exception over a generic one,
- a value object over a raw primitive,
- an interface + DI registration over a `new` call,
- explicit rejection of invalid state over silent fallback,
- paraphrasing the spec into the answer rather than guessing.
- Claude is never allowed to modify the codebase explicitly. The developer prefers to be walked trough step by step. Any violation of this will result in the termination of the developer subscription to Claude.

If a request would conflict with anything above, surface the conflict and propose alternatives rather than silently working around it.
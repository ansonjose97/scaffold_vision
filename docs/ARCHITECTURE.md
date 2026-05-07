# Architecture

This document describes the technical decisions behind ScaffoldVision and the trade-offs considered.

## Design Goals

1. **Clear separation of concerns** — the 3D rendering, the domain logic, and the recommendation engine each live behind explicit boundaries so any of them could be replaced without rewriting the others.
2. **Approachable** — a developer should be able to clone, run, and understand the system within an hour. No package-restore failures, no missing services, no docker-compose required.
3. **Honest scope** — the MVP simulates a production system without over-engineering. Persistence is in-memory; the production path is described here, not built. Premature complexity is harder to remove than to add.

## High-Level Components

### Frontend (Three.js + TypeScript)

The frontend is a Vite-built single-page app. Three.js handles 3D rendering. State is local to a few modules — there is no Redux or similar. For an MVP, scene-graph state plus straightforward DOM event handlers are enough.

**Module layout:**

- `viewport/` — Three.js scene, camera, orbit controls, lighting, building and scaffold meshes
- `components/` — UI rendering helpers (recommendation panel)
- `api/` — typed fetch wrapper around the backend
- `domain/` — shared types matching the backend contracts

**Why Three.js over Unity for the MVP:** Unity WebGL builds are large (50+ MB), slow to iterate on, and the C# tooling does not actually share code between the Unity runtime and the ASP.NET runtime. Three.js fits the MVP's iteration cadence. A Unity-based high-fidelity viewer remains on the roadmap as a separate module — appropriate when fidelity, advanced lighting, or AR previews on construction sites are required.

### Backend (ASP.NET Core 8)

The API is a single deployable unit organised by feature. Controllers are thin HTTP adapters; logic lives in services; the recommendation engine is isolated behind an interface.

**Layers:**

- **Controllers** — HTTP adapters. No business logic.
- **Services** — domain operations (`IComponentCatalog`, `IConfigurationService`).
- **AI** — recommendation engine, behind `IScaffoldRecommender`.
- **Models** — plain C# records representing the domain.

**Persistence in the MVP** is in-memory (`InMemoryComponentCatalog`, `InMemoryConfigurationStore`). Both implementations sit behind interfaces, so swapping them for a Postgres-backed implementation later is a matter of writing a new class and changing one DI registration. The MVP deliberately avoids pulling in Entity Framework Core to keep the project buildable with zero external NuGet dependencies — which means anyone cloning the repo can run it without first sorting out package restores or database setup.

### Recommendation Engine

The recommender is intentionally simple in this version:

1. **Geometric solver** — given a building's width, height, and depth, it computes the number of bays and lifts needed, then derives component counts from straightforward formulas:
   - standards = `(bays + 1) × (lifts + 1)`
   - ledgers = `bays × (lifts + 1) × 2`
   - platforms = `bays × lifts`
   - braces = `⌈bays / brace_every_n⌉ × lifts`
2. **Catalog matching** — picks the catalog entries that best fit the chosen lift height and bay width. If no exact match exists, the nearest is chosen and a note is added to the response.
3. **Engineering notes** — flags conditions that warrant attention (heights above 8m typically need additional anchoring, sparse bracing intervals may not satisfy stability requirements).

This split keeps the deterministic core auditable and testable while leaving a clear extension point. The interface `IScaffoldRecommender` allows swapping the implementation entirely — for example, to a learned model trained on real configurations once labelled data is available.

## Data Flow: Recommendation Request

```
User adjusts inputs → Frontend POST /api/recommendations
                                    ↓
                          RecommendationsController
                                    ↓
                          IScaffoldRecommender
                                    ↓
                       RuleBasedRecommender
                          ↓             ↓
                  GeometricSolver   CatalogMatcher
                          ↓             ↓
                          └──────┬──────┘
                                 ↓
                       RecommendationResponse
                                 ↓
                       Frontend renders panel + 3D scaffold
```

## Key Decisions and Trade-offs

**Why no external NuGet dependencies?**
The MVP runs on the ASP.NET Core 8 framework reference alone — no EF Core, no Swashbuckle, no test framework packages. The result is a project that builds offline, has a clean dependency graph, and has nothing for someone cloning the repo to misconfigure. The cost is that persistence is in-memory and there is no auto-generated Swagger UI. Both are explicit trade-offs documented here, not oversights.

**Why a custom test runner instead of xUnit?**
The same reasoning. A console app with a hand-rolled `Assert` helper covers the recommendation engine's geometry — which is the part most likely to regress — without dragging in xUnit, MSTest, or NUnit. `dotnet run` from the test project executes the suite and exits non-zero on failure, which is all CI needs. If the test surface grows, switching to xUnit is trivial.

**Why a rule-based recommender rather than a learned model?**
Without a labelled dataset of real scaffold configurations, a learned model would be guessing. A rule-based core that mirrors how planners actually think is more honest, more debuggable, and produces correct results today. The architecture leaves a clean seam for a learned model to plug in later.

**Why one project instead of microservices?**
The system is small. Splitting it into services would create deployment complexity without real benefits. The boundaries inside the codebase are clear enough that extraction into separate services would be straightforward if scale required it.

## Testing Strategy

- **Unit tests** for the recommendation engine. These are the highest-value tests because the geometric solver is the part most likely to regress when formulas or defaults change.
- **No frontend unit tests** in the MVP — the surface area is small and the value is low; visual regressions are caught faster by running the app.

CI runs all tests on every push (see `.github/workflows/ci.yml`).

## What Would Change at Scale

If this project needed to support real engineering workflows:

1. **Persist configurations** — swap `InMemoryConfigurationStore` for a Postgres-backed implementation. The interface already exists; only the implementation changes.
2. **Add a constraints engine** for load-bearing safety — likely a separate Python service using a physics library, called by the API as part of the recommendation flow.
3. **Move 3D rendering to Unity** for higher fidelity and AR previews on site. The current Three.js viewport remains useful for browser-based previews; Unity becomes a second front-end for richer use cases.
4. **Introduce CQRS** for the configuration save/load path once collaboration is added — read patterns will diverge from write patterns.
5. **Replace the rule-based recommender with a learned model** trained on real configurations. The interface `IScaffoldRecommender` is already the seam for this.

These are deferred deliberately. Each one is justified by a real requirement when it arrives, not before.

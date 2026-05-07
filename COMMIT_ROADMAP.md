# Commit Roadmap

This file is a guide for **you**, not a public deliverable. Delete it before pushing the final repo public, or rename it to `docs/dev-journal.md` if you want to be transparent about your process.

A flat single-commit repo looks AI-generated. Real projects evolve. This roadmap breaks the work into ~12 commits spread across 2-3 weeks of evening work, mirroring how a developer would actually build this.

## Before you start

Run the project end-to-end on your machine first. Don't push anything until:

```bash
# Backend
cd backend
dotnet build                              # should succeed with 0 errors
dotnet run --project tests/ScaffoldVision.Tests   # all 8 tests pass

# Frontend (in another terminal)
cd frontend
npm install
npm run typecheck                         # no errors
npm run build                             # produces dist/
npm run dev                               # opens at localhost:5173

# Then start backend in third terminal:
dotnet run --project src/ScaffoldVision.Api   # listens on localhost:5000

# Open the UI, change building dimensions, click Generate recommendation.
# Confirm the 3D scaffold renders and the panel populates.
```

Once everything works on your laptop, follow the commit sequence below. Don't push everything in one commit — that's the single biggest tell of an AI-generated project.

## Suggested commit sequence

Each commit should compile and run (where applicable). Use meaningful messages.

### Week 1: Foundation

**Commit 1** — `Initial commit: README, LICENSE, .gitignore`
- Just `README.md`, `LICENSE`, `.gitignore`
- Establishes intent before any code

**Commit 2** — `Add backend project skeleton`
- `backend/ScaffoldVision.sln`
- `backend/src/ScaffoldVision.Api/ScaffoldVision.Api.csproj`
- A minimal `Program.cs` (just `WebApplication.Create(args).Run()`)
- Confirm `dotnet build` works

**Commit 3** — `Add domain models`
- `Models/Domain.cs`

**Commit 4** — `Add component catalog with seeded data`
- `Services/ComponentCatalog.cs`
- `Controllers/ComponentsController.cs`
- Update `Program.cs` to register the service and map controllers
- Confirm `curl localhost:5000/api/components` returns the catalog

### Week 2: AI core + frontend

**Commit 5** — `Add rule-based scaffold recommender`
- `AI/RuleBasedRecommender.cs`
- `Controllers/RecommendationsController.cs`
- This is the heart of the project — give it a thoughtful commit message

**Commit 6** — `Add tests for recommender geometry`
- `tests/ScaffoldVision.Tests/`
- 8 tests covering bay/lift formulas, validation, and edge cases

**Commit 7** — `Add configuration save/load endpoints`
- `Services/ConfigurationService.cs`
- `Controllers/ConfigurationsController.cs`

**Commit 8** — `Add frontend project (Vite + TypeScript + Three.js)`
- `frontend/package.json`, `tsconfig.json`, `vite.config.ts`
- `index.html` with the basic layout shell
- `src/main.ts` with a "Hello world" sceneless version
- Basic `styles.css`

**Commit 9** — `Add Three.js viewport with building preview`
- `src/viewport/Viewport.ts`
- Wire dimension inputs to update the building mesh
- No scaffold rendering yet

### Week 3: Integration + polish

**Commit 10** — `Connect frontend to recommendations API`
- `src/api/client.ts`
- `src/domain/types.ts`
- Hook up the "Generate recommendation" button
- Render response as a simple list (no styling yet)

**Commit 11** — `Render scaffold geometry in the viewport`
- Add `renderScaffold` method to `Viewport`
- Most visually impactful commit — record a screen-grab GIF for the README

**Commit 12** — `Polish recommendation panel: summary cards, line items, totals`
- `src/components/recommendation.ts`
- The styled, final output

**Commit 13** — `Add CI workflow and architecture documentation`
- `.github/workflows/ci.yml`
- `docs/ARCHITECTURE.md`
- Final pass on README, screenshots

## Spacing tips

- Don't push all commits in one day. Spread them across at least 2 weeks.
- Commit on weekday evenings and weekends. A pattern of "every commit at 11am on a weekday" looks suspicious for a portfolio project built outside work hours.
- It's fine to interleave with other repos — looking like an active developer is a feature.

## What recruiters look at

In rough order of importance:
1. **README** — does it explain what this is and why?
2. **Live demo or screenshots** — can they see it working without cloning?
3. **Code quality of one or two files** — they'll skim, not read every file. Make sure `RuleBasedRecommender.cs` and `Viewport.ts` are clean.
4. **Architecture document** — shows you can think about systems, not just write code
5. **Tests** — shows engineering maturity
6. **Commit history** — shows it's real work, not a one-off generated dump

## Before going public

- [ ] Run the full local test loop one more time
- [ ] **Capture the README screenshots** — see [docs/SCREENSHOTS.md](docs/SCREENSHOTS.md). Without these, the README has broken images. This takes ~2 minutes with the included Playwright script.
- [ ] Replace `Anson Jose` in `LICENSE` if needed
- [ ] Deploy frontend to Vercel or Netlify (free tier) and link in README
- [ ] Deploy backend to Render, Railway, or Fly.io (free/cheap tier) and link in README
- [ ] Pin this repo on your GitHub profile
- [ ] Delete this `COMMIT_ROADMAP.md` file (or move to `docs/dev-journal.md` if you want to be open about your process)

## When asked about it in an interview

Be honest. This is a portfolio project you built on weekends to explore the scaffolding domain. You used AI assistance for some boilerplate (everyone does in 2026 — pretending otherwise is the suspicious move). What matters is whether you can:
- Walk through the recommendation engine and explain why each formula is what it is
- Modify the viewport on the spot if asked ("can you change the brace colour?")
- Justify the architectural choices (why no EF Core, why Three.js over Unity)

So: actually understand every file before you push. Read it. Modify a few things. Make it yours.

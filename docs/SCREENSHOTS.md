# Capturing Screenshots for the README


The README references images in `docs/screenshots/`. To regenerate them, do this once locally:

## Quick way (recommended)

1. Make sure the backend and frontend are both running:
   ```bash
   # Terminal 1
   cd backend && dotnet run --project src/ScaffoldVision.Api

   # Terminal 2
   cd frontend && npm install && npm run dev
   ```

2. In a third terminal, install the screenshot tool and run it:
   ```bash
   cd docs
   npm init -y
   npm install playwright
   npx playwright install chromium
   node capture-screenshots.js
   ```

This produces three PNGs in `docs/screenshots/`:
- `hero.png` — initial state with the default building
- `recommendation.png` — after clicking "Generate recommendation"
- `tall-building.png` — a 15×12×10m wraparound configuration

## Manual way

If you'd rather skip Playwright, just take screenshots yourself:

1. Run the app (steps 1 and 2 above).
2. Open `http://localhost:5173` in Chrome at 1440×900.
3. Use your OS screenshot tool (Cmd+Shift+4 on macOS, Snipping Tool on Windows) to capture:
   - The full window before clicking anything → save as `docs/screenshots/hero.png`
   - The full window after clicking "Generate recommendation" → save as `docs/screenshots/recommendation.png`
   - The full window with width=15, height=12, depth=10, wrap-around checked, after clicking "Generate" → save as `docs/screenshots/tall-building.png`

The README's image references will pick them up automatically.

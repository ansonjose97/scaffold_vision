#!/usr/bin/env node
/**
 * Captures screenshots of the running ScaffoldVision app for the README.
 *
 * Prerequisites:
 *   1. Backend running on http://localhost:5000
 *      cd backend && dotnet run --project src/ScaffoldVision.Api
 *   2. Frontend running on http://localhost:5173
 *      cd frontend && npm install && npm run dev
 *   3. Playwright installed in this folder:
 *      cd docs && npm init -y && npm install playwright && npx playwright install chromium
 *
 * Then run:
 *   node capture-screenshots.js
 *
 * Output: docs/screenshots/{hero,recommendation,tall-building}.png
 */

const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const APP_URL = 'http://localhost:5173';
const OUT_DIR = path.join(__dirname, 'screenshots');

if (!fs.existsSync(OUT_DIR)) {
  fs.mkdirSync(OUT_DIR, { recursive: true });
}

async function setInput(page, id, value) {
  const selector = `#${id}`;
  await page.fill(selector, '');
  await page.fill(selector, String(value));
  await page.dispatchEvent(selector, 'input');
}

async function main() {
  const browser = await chromium.launch();
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 2  // retina-quality screenshots
  });
  const page = await context.newPage();

  console.log('→ Loading app...');
  await page.goto(APP_URL, { waitUntil: 'networkidle' });
  await page.waitForSelector('#viewport canvas');
  await page.waitForTimeout(1000);  // let the 3D scene settle

  // Screenshot 1: Hero — initial state with default building
  console.log('→ Capturing hero shot (default building)...');
  await page.screenshot({
    path: path.join(OUT_DIR, 'hero.png'),
    fullPage: false
  });

  // Screenshot 2: With recommendation generated for default building
  console.log('→ Generating recommendation for default 10x6x8m building...');
  await page.click('#btn-recommend');
  await page.waitForSelector('.summary-grid', { timeout: 5000 });
  await page.waitForTimeout(800);
  await page.screenshot({
    path: path.join(OUT_DIR, 'recommendation.png'),
    fullPage: false
  });

  // Screenshot 3: Tall building, wraparound — produces engineering note
  console.log('→ Generating recommendation for tall wraparound building...');
  await setInput(page, 'building-width', 15);
  await setInput(page, 'building-height', 12);
  await setInput(page, 'building-depth', 10);
  await page.check('#wrap-around');
  await page.waitForTimeout(500);
  await page.click('#btn-recommend');
  await page.waitForTimeout(1000);
  await page.screenshot({
    path: path.join(OUT_DIR, 'tall-building.png'),
    fullPage: false
  });

  console.log('✓ Done. Screenshots saved to docs/screenshots/');
  await browser.close();
}

main().catch((err) => {
  console.error('Screenshot capture failed:', err);
  console.error('\nMake sure the app is running:');
  console.error('  Terminal 1: cd backend && dotnet run --project src/ScaffoldVision.Api');
  console.error('  Terminal 2: cd frontend && npm install && npm run dev');
  process.exit(1);
});

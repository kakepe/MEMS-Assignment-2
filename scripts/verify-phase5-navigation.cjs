const { chromium, expect } = require('../.tools/browser/node_modules/@playwright/test');
const fs = require('node:fs'), assert = require('node:assert/strict');
const origin = 'https://localhost:7290', proxy = '/mems-public/api/mems-api';
(async () => {
  const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
  let release;
  try {
    const context = await browser.newContext({ ignoreHTTPSErrors: true }); context.setDefaultTimeout(20000);
    const page = await context.newPage(), errors = []; page.on('pageerror', e => errors.push(e.message));
    await page.goto(origin + '/burnaby/candidates'); await expect(page.locator('.candidate-card')).toHaveCount(6);
    const oldNames = await page.locator('.candidate-card h2').allTextContents();
    let requested;
    const hit = new Promise(resolve => requested = resolve), gate = new Promise(resolve => release = resolve);
    await page.route(url => url.pathname === proxy + '/api/surrey/candidates', async route => { requested(); await gate; try { await route.continue(); } catch { } });
    await page.getByRole('link', { name: 'Explore municipalities', exact: true }).click();
    await page.getByRole('link', { name: 'Surrey', exact: true }).click(); await hit;
    await expect(page.getByRole('status')).toContainText('Loading election information');
    for (const name of oldNames) assert.ok(!(await page.locator('main').innerText()).includes(name));
    await page.getByRole('link', { name: 'Explore municipalities', exact: true }).click();
    await page.getByRole('link', { name: 'Richmond', exact: true }).click(); await expect(page.locator('.tenant-name')).toHaveText('Richmond');
    release(); await expect(page.locator('.candidate-card')).toHaveCount(3);
    await page.waitForTimeout(300);
    await expect(page.locator('.tenant-name')).toHaveText('Richmond');
    const ids = await page.locator('[data-candidate-id]').evaluateAll(xs => xs.map(x => Number(x.dataset.candidateId)));
    const expected = await (await context.request.get(origin + proxy + '/api/richmond/candidates?pageSize=3')).json();
    assert.deepEqual(ids, expected.items.map(x => x.candidateId));
    await page.screenshot({ path: 'artifacts/phase5/rapid-tenant-switch.png', fullPage: true });
    await page.locator('.skip-link').focus(); await page.keyboard.press('Enter');
    await expect(page.locator('#public-content')).toBeFocused();
    await page.goto(origin + '/bad.slug'); await expect(page.getByRole('heading', { name: 'Page not found', exact: true })).toBeVisible();
    assert.equal((await context.request.get(origin + '/missing-script.js')).status(), 404);
    for (const query of ['page=oops', 'pageSize=999999999999999', 'positionId=bad', 'page=0', 'sort=bad']) {
      await page.goto(origin + '/burnaby/candidates?' + query);
      await expect(page.locator('[data-error-status="400"]')).toBeVisible();
      await page.getByRole('link', { name: 'Reset filters', exact: true }).click();
      await expect(page.locator('.candidate-card')).toHaveCount(6);
    }
    const positions = await (await context.request.get(origin + proxy + '/api/burnaby/positions')).json();
    await page.goto(origin + '/burnaby/positions/' + positions[0].positionId + '?page=oops');
    await expect(page.locator('[data-error-status="400"]')).toBeVisible();
    assert.deepEqual(errors, []);
    const result = { passed: true, checks: ['Previous tenant records disappear while a new tenant is loading.', 'Navigating away from a delayed tenant request cancels/discards it; late data never replaces the active Richmond page.', 'Resulting cards match Richmond API records with no browser page errors.', 'Keyboard skip link focuses main content; file-like invalid tenant document URLs show Not Found; missing script requests retain HTTP 404.', 'Malformed, overflowing and invalid filter/page/sort values show safe 400 content without browser errors; Reset filters recovers, and position pagination rejects malformed values safely.'] };
    fs.writeFileSync('artifacts/phase5/navigation-stress.json', JSON.stringify(result, null, 2)); console.log(JSON.stringify(result, null, 2));
  } finally { release?.(); await browser.close(); }
})().catch(e => { console.error(e.message.slice(0,600)); process.exitCode = 1; });

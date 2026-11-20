// Infrastructure regression: real Admin sign-in now protects the original connectivity probe.
const { chromium, expect } = require('../.tools/browser/node_modules/@playwright/test');
const fs = require('node:fs');
const assert = require('node:assert/strict');

(async () => {
  const links = JSON.parse(fs.readFileSync('.local/dashboard-links.json', 'utf8'));
  const publicUrl = links.find(x => x.href.startsWith('https://localhost:') && x.href.endsWith('/mems-public'))?.href;
  assert.ok(publicUrl, 'Public gateway URL must be available.');
  const logBytes = fs.readFileSync('.local/apphost-direct.log');
  const log = logBytes.toString(logBytes[0] === 0xff ? 'utf16le' : 'utf8');
  const login = log.match(/https:\/\/localhost:\d+\/login\?t=([^\s]+)/);
  const sqlPassword = JSON.parse(fs.readFileSync('.local/apphost-secrets.json', 'utf8')).Parameters['mems-sql-password'];
  const browserPath = ['C:/Program Files/Google/Chrome/Application/chrome.exe', 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'].find(fs.existsSync);
  const browser = await chromium.launch({ executablePath: browserPath, headless: true });
  const checks = [];
  try {
    const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
    context.setDefaultTimeout(15000);
    const api = await context.request.get('https://localhost:7192/api/foundation');
    assert.equal(api.status(), 200);
    assert.equal((await api.json()).databaseConnected, true);
    for (const url of ['https://localhost:7192/health', 'https://localhost:7192/alive', 'https://localhost:7252/health']) {
      assert.equal((await context.request.get(url)).status(), 200);
    }
    const openapi = await context.request.get('https://localhost:7192/openapi/v1.json');
    assert.equal(openapi.status(), 200);
    assert.ok((await openapi.json()).paths['/api/foundation']);
    checks.push('API/OpenAPI and SQL-backed API/Admin readiness returned HTTP 200');

    const admin = await context.newPage();
    const adminErrors = [];
    admin.on('pageerror', e => adminErrors.push(e.message));
    await admin.goto('https://localhost:7252/admin');
    await admin.getByLabel('Username', { exact: true }).fill('super');
    await admin.getByLabel('Password', { exact: true }).fill(JSON.parse(fs.readFileSync('.local/identity-seed.json', 'utf8')).Parameters['mems-identity-seed-password']);
    await admin.getByRole('button', { name: 'Log in', exact: true }).click();
    await admin.waitForURL('https://localhost:7252/admin');
    await admin.goto('https://localhost:7252/admin/connectivity');
    await expect(admin.getByRole('heading', { name: 'Service connectivity' })).toBeVisible();
    await admin.getByRole('button', { name: 'Check API connection' }).click();
    await expect(admin.getByRole('status')).toHaveText('API connected. SQL Server connected.', { timeout: 30000 });
    assert.deepEqual(adminErrors, []);
    await admin.screenshot({ path: 'artifacts/phase1/admin.png', fullPage: true });
    checks.push('Interactive Server Admin button reached API via Aspire service discovery');

    const page = await context.newPage();
    const errors = [], requests = [], downloadedText = [];
    page.on('pageerror', e => errors.push(e.message));
    page.on('request', r => requests.push(r.url()));
    page.on('response', async r => {
      if (/json|javascript|text\/html/.test(r.headers()['content-type'] || '')) {
        try { downloadedText.push(await r.text()); } catch { /* canceled navigation */ }
      }
    });
    await page.goto(new URL('/_status', publicUrl).href);
    await expect(page.getByRole('heading', { name: 'Service status', exact: true })).toBeVisible({ timeout: 30000 });
    const responsePromise = page.waitForResponse(r => r.url().endsWith('/api/foundation'));
    await page.getByRole('button', { name: 'Check availability' }).click();
    const proxied = await responsePromise;
    assert.equal(proxied.status(), 200);
    assert.equal(new URL(proxied.url()).origin, new URL(publicUrl).origin);
    assert.equal((await proxied.json()).databaseConnected, true);
    await expect(page.getByRole('status')).toHaveText('Election information services are available.');
    assert.deepEqual(errors, []);
    assert.ok(requests.every(url => new URL(url).origin === new URL(publicUrl).origin), 'All public requests must stay on the gateway origin');
    await page.screenshot({ path: 'artifacts/phase1/public.png', fullPage: true });
    await page.reload();
    await expect(page.getByRole('heading', { name: 'Service status', exact: true })).toBeVisible();
    checks.push('WASM loaded, reloaded and reached API through same-origin gateway without CORS');

    const config = await context.request.get(publicUrl + '/_blazor/_configuration');
    assert.equal(config.status(), 200);
    const configText = await config.text();
    JSON.parse(configText);
    const exposed = downloadedText.join('\n') + configText + await api.text();
    assert.ok(!exposed.includes(sqlPassword), 'SQL password must remain server-side');
    if (fs.existsSync('.local/identity-seed.json')) {
      const identityPassword = JSON.parse(fs.readFileSync('.local/identity-seed.json', 'utf8')).Parameters['mems-identity-seed-password'];
      assert.ok(!exposed.includes(identityPassword), 'Identity seed password must remain server-side');
    }
    assert.ok(!exposed.includes(login[1]), 'Dashboard token must remain server-side');
    assert.ok(!/ConnectionStrings|Password=|User ID=|OTEL_EXPORTER_OTLP_HEADERS/i.test(configText));
    const assets = JSON.parse(fs.readFileSync('src/MEMS.Public/obj/project.assets.json', 'utf8'));
    assert.ok(!Object.keys(assets.libraries).some(name => /MEMS\.Data|ServiceDefaults|SqlClient|EntityFrameworkCore/i.test(name)));
    checks.push('Browser configuration contains no SQL credentials/dashboard token; WASM has no Data, EF or server ServiceDefaults dependency');

    const dashboard = await context.newPage();
    await dashboard.goto(login[0]);
    await expect(dashboard.getByText('mems-api', { exact: true }).first()).toBeVisible();
    await dashboard.screenshot({ path: 'artifacts/phase1/dashboard.png', fullPage: true });
    for (const [route, pattern] of [['structuredlogs', /SQL connectivity probe completed/], ['traces', /api\/foundation/]]) {
      await dashboard.goto('https://localhost:17048/' + route);
      // Filter out the WASM runtime asset requests before inspecting the virtualized grid.
      await dashboard.locator('input[type=search]').fill(route === 'structuredlogs' ? 'SQL connectivity probe' : 'foundation');
      await dashboard.locator('input[type=search]').press('Enter');
      await expect(dashboard.locator('body')).toContainText(pattern, { timeout: 30000 });
      await dashboard.screenshot({ path: 'artifacts/phase1/' + route + '.png', fullPage: true });
    }
    checks.push('Authenticated Aspire dashboard shows structured probe logs and API traces');
    const result = { passed: true, adminUrl: 'https://localhost:7252/admin', publicUrl: publicUrl + '/', apiUrl: 'https://localhost:7192/api/foundation', checks };
    fs.writeFileSync('artifacts/phase1/verification.json', JSON.stringify(result, null, 2));
    console.log(JSON.stringify(result, null, 2));
  } finally { await browser.close(); }
})().catch(error => {
  fs.writeFileSync('.local/verification-error.txt', error.message);
  console.error(error.message.slice(0, 1000));
  process.exitCode = 1;
});

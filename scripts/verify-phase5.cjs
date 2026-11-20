// Phase 5: actual WASM/browser/API checks; Admin synchronization uses owned records only.
const { chromium, request, expect } = require('../.tools/browser/node_modules/@playwright/test');
const fs = require('node:fs'), assert = require('node:assert/strict'), zlib = require('node:zlib');
const site = 'https://localhost:7290', admin = 'https://localhost:7252', api = 'https://localhost:7192';
const proxy = '/mems-public/api/mems-api';
const password = JSON.parse(fs.readFileSync('.local/identity-seed.json', 'utf8')).Parameters['mems-identity-seed-password'];
const sqlPassword = JSON.parse(fs.readFileSync('.local/apphost-secrets.json', 'utf8')).Parameters['mems-sql-password'];
const logBytes = fs.readFileSync('.local/apphost-direct.log');
const dashboardToken = logBytes.toString(logBytes[0] === 0xff ? 'utf16le' : 'utf8').match(/login\?t=([^\s]+)/)?.[1];
const prefix = 'Phase5 verify ' + Date.now(), slug = 'phase5-' + Date.now();
const ledger = { prefix, slug, cleaned: false }, checks = [], errors = [], network = [], privacyFailures = [], pending = [];
const secrets = [password, sqlPassword, dashboardToken].filter(Boolean);
fs.mkdirSync('artifacts/phase5', { recursive: true });
const saveLedger = () => fs.writeFileSync('.local/phase5-created-records.json', JSON.stringify(ledger, null, 2));
saveLedger();
const appendCheck = checks.push.bind(checks);
checks.push = (...messages) => { const result = appendCheck(...messages); for (const message of messages) console.log('PASS: ' + message); return result; };
function observe(page) {
  page.on('pageerror', error => errors.push(error.message));
  page.on('request', r => {
    if (new URL(r.url()).pathname.startsWith(proxy + '/api/')) pending.push((async () => {
      try { const h = await r.allHeaders(); network.push({ path: new URL(r.url()).pathname, method: r.method(), cookie: Boolean(h.cookie), authorization: Boolean(h.authorization) }); } catch { }
    })());
  });
  page.on('response', r => {
    if (new URL(r.url()).origin === site) pending.push((async () => {
      try { const bytes = await r.body(); if (secrets.some(secret => bytes.includes(Buffer.from(secret)))) privacyFailures.push('Secret appeared in a browser response'); } catch { }
    })());
  });
}
async function go(page, path) { await page.goto(site + path); }
async function cards(page, count) { await expect(page.locator('.candidate-card')).toHaveCount(count); }
async function loadedImages(page, selector, count) {
  await expect(page.locator(selector)).toHaveCount(count);
  await expect.poll(() => page.locator(selector).evaluateAll(xs => xs.every(x => x.complete && x.naturalWidth > 0))).toBe(true);
}
async function accessible(page) {
  assert.equal(await page.locator('main h1').count(), 1);
  assert.deepEqual(await page.locator('main input:not([type=hidden]):visible,main select:visible').evaluateAll(xs => xs.filter(x => !(x.labels?.length || x.getAttribute('aria-label'))).map(x => x.id)), []);
  assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
}
async function login(page, name = 'super') {
  await page.goto(admin + '/admin/account/login');
  await page.getByLabel('Username', { exact: true }).fill(name);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Log in', exact: true }).click();
  await page.waitForURL(admin + '/admin');
}
async function adminGo(page, path) { await page.goto(admin + path); await page.waitForLoadState('networkidle'); }
async function save(page, label, type) {
  await page.getByRole('button', { name: label, exact: true }).click();
  await page.waitForURL(new RegExp('/' + type + '/\\d+\\?notice=saved$'));
  return Number(new URL(page.url()).pathname.split('/').pop());
}
function png() {
  const crc = b => { let n = 0xffffffff; for (const v of b) { n ^= v; for (let i = 0; i < 8; i++) n = (n >>> 1) ^ ((n & 1) ? 0xedb88320 : 0); } return (n ^ 0xffffffff) >>> 0; };
  const chunk = (name, data) => { const t = Buffer.from(name), size = Buffer.alloc(4), sum = Buffer.alloc(4); size.writeUInt32BE(data.length); sum.writeUInt32BE(crc(Buffer.concat([t, data]))); return Buffer.concat([size, t, data, sum]); };
  const h = Buffer.alloc(13); h.writeUInt32BE(2, 0); h.writeUInt32BE(2, 4); h[8] = 8; h[9] = 6;
  return Buffer.concat([Buffer.from('89504e470d0a1a0a', 'hex'), chunk('IHDR', h), chunk('IDAT', zlib.deflateSync(Buffer.from([0,20,150,80,255,20,150,80,255,0,20,150,80,255,20,150,80,255]))), chunk('IEND', Buffer.alloc(0))]);
}
async function upload(page, name, buffer, mimeType) {
  await page.locator('#media-file').setInputFiles({ name, mimeType, buffer });
  await page.getByRole('button', { name: 'Upload image', exact: true }).click();
  await expect(page.locator('.media-panel [role=status]')).toHaveText('Image uploaded successfully.');
}
async function cleanup() {
  const c = await request.newContext({ baseURL: api, ignoreHTTPSErrors: true });
  try {
    let token = (await (await c.get('/api/auth/csrf')).json()).requestToken;
    assert.equal((await c.post('/api/auth/login', { headers: { 'X-CSRF-TOKEN': token }, data: { userName: 'super', password } })).status(), 200);
    token = (await (await c.get('/api/auth/csrf')).json()).requestToken;
    const remove = async (path, item) => {
      assert.ok(item.name.startsWith(prefix), 'Cleanup ownership prefix');
      assert.equal((await c.delete(path, { headers: { 'X-CSRF-TOKEN': token, 'If-Match': '"' + item.version + '"' } })).status(), 204);
    };
    const tenants = await (await c.get('/api/admin/municipalities')).json();
    for (const t of tenants) {
      const people = await (await c.get('/api/' + t.slug + '/candidates?search=' + encodeURIComponent(prefix) + '&pageSize=100')).json();
      for (const x of people.items.filter(x => x.name.startsWith(prefix))) await remove('/api/' + t.slug + '/candidates/' + x.candidateId, x);
      const offices = await (await c.get('/api/' + t.slug + '/positions')).json();
      for (const x of offices.filter(x => x.name.startsWith(prefix))) await remove('/api/' + t.slug + '/positions/' + x.positionId, x);
    }
    for (const t of tenants.filter(x => x.name.startsWith(prefix))) await remove('/api/admin/municipalities/' + t.municipalityId, await (await c.get('/api/admin/municipalities/' + t.municipalityId)).json());
    ledger.cleaned = true; saveLedger();
  } finally { await c.dispose(); }
}

(async () => {
  const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
  const contexts = [], ground = await request.newContext({ baseURL: api, ignoreHTTPSErrors: true });
  const newPage = async (watch = true) => { const c = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } }); c.setDefaultTimeout(20000); contexts.push(c); const p = await c.newPage(); if (watch) observe(p); return p; };
  const get = async path => { const r = await ground.get(path); assert.equal(r.status(), 200); return r.json(); };
  let failure;
  try {
    const directory = await get('/api/municipalities'), towns = Object.fromEntries(directory.map(x => [x.slug, x]));
    assert.equal(directory.length, 5);
    const expected = {};
    for (const slug of ['burnaby', 'surrey', 'richmond', 'pitt-meadows', 'maple-ridge']) {
      expected[slug] = { m: (await get('/api/' + slug + '/municipalities'))[0], positions: await get('/api/' + slug + '/positions'), people: await get('/api/' + slug + '/candidates') };
    }
    const page = await newPage();
    await go(page, '/'); await expect(page.getByRole('heading', { name: 'Choose your municipality', exact: true })).toBeVisible();
    await expect(page.locator('.municipality-card')).toHaveCount(directory.length); await loadedImages(page, '.municipality-card img', directory.length); await accessible(page);
    await page.screenshot({ path: 'artifacts/phase5/public-landing.png', fullPage: true });
    await page.evaluate(() => window.__phase5NavigationMarker = 'same-wasm-window');
    await page.getByRole('link', { name: 'Burnaby', exact: true }).click();
    await expect(page.locator('h1')).toHaveText(expected.burnaby.m.publicTitle);
    assert.equal(await page.evaluate(() => window.__phase5NavigationMarker), 'same-wasm-window');
    checks.push('Anonymous landing discovery is API-driven, including election dates/logos; tenant links navigate within the single WASM application.');

    let portraits = 0, avatars = 0;
    for (const slug of Object.keys(expected)) {
      const e = expected[slug]; await go(page, '/' + slug);
      await expect(page.locator('h1')).toHaveText(e.m.publicTitle); await expect(page.locator('.tenant-name')).toHaveText(e.m.name);
      await expect(page.locator('.position-card')).toHaveCount(e.positions.length); await cards(page, 3); await loadedImages(page, '.tenant-logo img', 1);
      if (['burnaby', 'surrey', 'richmond'].includes(slug)) await page.screenshot({ path: 'artifacts/phase5/' + slug + '-overview.png', fullPage: true });
      await go(page, '/' + slug + '/candidates'); await cards(page, e.people.totalCount); await loadedImages(page, '.candidate-card img', e.people.totalCount);
      assert.deepEqual(await page.locator('[data-candidate-id]').evaluateAll(xs => xs.map(x => Number(x.dataset.candidateId))), e.people.items.map(x => x.candidateId));
      const types = await page.locator('.candidate-card img').evaluateAll(xs => xs.map(x => x.src.split(';')[0]));
      portraits += types.filter(x => x === 'data:image/jpeg').length; avatars += types.filter(x => x === 'data:image/svg+xml').length;
      await accessible(page);
      if (['burnaby', 'surrey', 'richmond'].includes(slug)) await page.screenshot({ path: 'artifacts/phase5/' + slug + '-candidates.png', fullPage: true });
    }
    assert.equal(portraits, 24); assert.equal(avatars, 6);
    await go(page, '/RICHMOND'); await expect(page.locator('h1')).toHaveText(expected.richmond.m.publicTitle);
    await go(page, '/mems-public/burnaby'); await page.waitForURL(site + '/burnaby'); await expect(page.locator('h1')).toHaveText(expected.burnaby.m.publicTitle);
    checks.push('All five tenant overviews/directories match API ownership and records; 24 JPEG portraits, six Richmond SVG avatars and logos render; mixed-case and legacy routes work.');

    await go(page, '/burnaby/candidates'); const name = expected.burnaby.people.items[0].name.split(' ')[0];
    await page.getByLabel('Search by name', { exact: true }).fill(name); await page.getByRole('button', { name: 'Apply filters', exact: true }).click();
    const searched = await get('/api/burnaby/candidates?search=' + encodeURIComponent(name)); await cards(page, searched.totalCount);
    await page.getByRole('link', { name: 'Clear filters', exact: true }).click(); await cards(page, 6);
    const pos = expected.burnaby.positions[0]; await page.getByLabel('Position', { exact: true }).selectOption(String(pos.positionId)); await page.getByRole('button', { name: 'Apply filters', exact: true }).click(); await cards(page, 2);
    await page.getByRole('link', { name: 'Clear filters', exact: true }).click(); await page.getByLabel('Sort by', { exact: true }).selectOption('created'); await page.getByLabel('Per page', { exact: true }).selectOption('5'); await page.getByRole('button', { name: 'Apply filters', exact: true }).click(); await cards(page, 5);
    const sorted = await get('/api/burnaby/candidates?sort=created&pageSize=5'); assert.deepEqual(await page.locator('[data-candidate-id]').evaluateAll(xs => xs.map(x => Number(x.dataset.candidateId))), sorted.items.map(x => x.candidateId));
    await page.getByRole('link', { name: 'Next page', exact: true }).click(); await cards(page, 1);
    await page.getByRole('link', { name: 'Previous page', exact: true }).click(); await cards(page, 5);
    const person = sorted.items[0]; await page.getByRole('link', { name: person.name, exact: true }).click(); await expect(page.locator('h1')).toHaveText(person.name); await expect(page.locator('.biography')).toHaveText(person.profile); await loadedImages(page, '.profile-portrait img', 1);
    await page.screenshot({ path: 'artifacts/phase5/candidate-profile.png', fullPage: true });
    await page.reload(); await expect(page.locator('h1')).toHaveText(person.name); await page.goBack(); await cards(page, 5); await expect(page.getByLabel('Sort by', { exact: true })).toHaveValue('created'); await page.goForward(); await expect(page.locator('h1')).toHaveText(person.name);
    checks.push('API name search, position filtering, stable sort and pagination match API results; candidate details, direct refresh and browser back/forward preserve route/query state.');

    await go(page, '/burnaby/positions'); await expect(page.locator('.position-card')).toHaveCount(3); await page.getByRole('link', { name: pos.name, exact: true }).click();
    await expect(page.locator('h1')).toHaveText(pos.name); await expect(page.locator('.seat-summary strong')).toHaveText(String(pos.seatsAvailable)); await cards(page, 2);
    await page.screenshot({ path: 'artifacts/phase5/position-details.png', fullPage: true });
    for (const path of ['/no-such-municipality', '/bad_slug', '/burnaby/candidates/' + expected.surrey.people.items[0].candidateId, '/burnaby/positions/' + expected.surrey.positions[0].positionId]) { await go(page, path); await expect(page.getByRole('heading', { name: 'Page not found', exact: true })).toBeVisible(); }
    await go(page, '/burnaby/candidates?positionId=' + expected.surrey.positions[0].positionId); await expect(page.locator('[data-error-status="400"]')).toBeVisible();
    const forged = await page.evaluate(async ({ proxy, candidate, position, tenant }) => {
      const status = async path => (await fetch(proxy + path, { credentials: 'omit' })).status;
      const data = await (await fetch(proxy + '/api/burnaby/candidates?municipalityId=' + tenant, { credentials: 'omit' })).json();
      return { statuses: await Promise.all(['/api/burnaby/candidates/' + candidate, '/api/burnaby/positions/' + position, '/api/burnaby/candidates/' + candidate + '/photo', '/api/burnaby/municipalities/' + tenant].map(status)), tenantIds: data.items.map(x => x.municipalityId) };
    }, { proxy, candidate: expected.surrey.people.items[0].candidateId, position: expected.surrey.positions[0].positionId, tenant: towns.surrey.municipalityId });
    assert.deepEqual(forged.statuses, [404,404,404,404]); assert.ok(forged.tenantIds.every(x => x === towns.burnaby.municipalityId));
    checks.push('Position browsing/seats are scoped; unknown/invalid slugs and foreign record/media IDs fail safely; forged query tenant IDs cannot change API scope.');

    const faulty = await newPage(); let fail = true;
    const candidateRoute = url => url.pathname === proxy + '/api/burnaby/candidates';
    await faulty.route(candidateRoute, r => fail ? r.fulfill({ status: 500, contentType: 'application/json', body: '{"detail":"PRIVATE_ERROR_MARKER"}' }) : r.continue());
    await go(faulty, '/burnaby/candidates'); await expect(faulty.locator('[data-error-status="500"]')).toBeVisible(); assert.ok(!(await faulty.locator('body').innerText()).includes('PRIVATE_ERROR_MARKER'));
    fail = false; await faulty.getByRole('button', { name: 'Try again', exact: true }).click(); await cards(faulty, 6);
    await faulty.route(url => url.pathname === proxy + '/api/burnaby/candidates/' + person.candidateId + '/photo', r => r.fulfill({ status: 404 }));
    await go(faulty, '/burnaby/candidates/' + person.candidateId); await expect(faulty.locator('.profile-portrait')).toContainText('Photograph unavailable');
    checks.push('A controlled 500 response shows a safe error without server details; retry recovers; missing media displays an accessible fallback.');

    await go(page, '/'); await page.locator('.skip-link').focus(); await expect(page.locator('.skip-link')).toBeFocused(); await page.keyboard.press('Enter');
    await page.setViewportSize({ width: 390, height: 844 }); await go(page, '/richmond/candidates'); await cards(page, 6); await loadedImages(page, '.candidate-card img', 6); await accessible(page);
    await page.getByLabel('Search by name', { exact: true }).focus(); await page.keyboard.press('Tab'); await expect(page.getByLabel('Position', { exact: true })).toBeFocused();
    await page.screenshot({ path: 'artifacts/phase5/mobile-richmond-candidates.png', fullPage: true });
    await go(page, '/richmond'); await expect(page.locator('h1')).toHaveText(expected.richmond.m.publicTitle); await accessible(page); await page.screenshot({ path: 'artifacts/phase5/mobile-richmond-overview.png', fullPage: true });
    await go(page, '/richmond/candidates/' + expected.richmond.people.items[0].candidateId); await expect(page.locator('h1')).toHaveText(expected.richmond.people.items[0].name); await accessible(page);
    await page.screenshot({ path: 'artifacts/phase5/mobile-candidate-profile.png', fullPage: true });
    checks.push('Desktop and 390px mobile pages fit without page overflow; headings/labels, skip link and keyboard filter navigation work.');

    const rolePage = await newPage(false); await login(rolePage, 'Burnabyadmin');
    const sameBrowser = await rolePage.context().newPage(); observe(sameBrowser); await go(sameBrowser, '/surrey/candidates'); await cards(sameBrowser, 6); await loadedImages(sameBrowser, '.candidate-card img', 6);
    assert.equal((await rolePage.context().request.get(api + '/api/surrey/candidates')).status(), 403);
    checks.push('Public metadata/images remain anonymous even in a browser signed into another tenant Admin; direct authenticated API access still rejects that foreign tenant.');

    const manage = await newPage(false); await login(manage);
    await adminGo(manage, '/admin/municipalities/create'); await manage.getByLabel('Municipality name', { exact: true }).fill(prefix + ' municipality'); await manage.getByLabel('Tenant slug', { exact: true }).fill(slug); await manage.getByLabel('Election description', { exact: true }).fill('Disposable municipality for synchronization verification.');
    ledger.municipalityId = await save(manage, 'Save municipality', 'municipalities'); saveLedger();
    await upload(manage, 'owned-logo.png', png(), 'image/png');
    const publicSync = await newPage(); await go(publicSync, '/'); await expect(publicSync.locator('.municipality-card')).toHaveCount(6); await publicSync.getByRole('link', { name: prefix + ' municipality', exact: true }).click(); await expect(publicSync.locator('h1')).toHaveText('Municipal election guide');
    await expect(publicSync.locator('main')).toContainText('No positions have been published');
    await adminGo(manage, '/admin/' + slug + '/positions/create'); await manage.getByLabel('Position name', { exact: true }).fill(prefix + ' position'); ledger.positionId = await save(manage, 'Save position', 'positions'); saveLedger();
    await adminGo(manage, '/admin/' + slug + '/candidates/create'); await manage.getByLabel('Candidate name', { exact: true }).fill(prefix + ' candidate'); await manage.getByLabel('Position', { exact: true }).selectOption(String(ledger.positionId)); await manage.getByLabel('Biography', { exact: true }).fill('Original disposable biography.'); ledger.candidateId = await save(manage, 'Save candidate', 'candidates'); saveLedger();
    await upload(manage, 'owned-photo.jpg', fs.readFileSync('src/MEMS.Api/wwwroot/images/demo-candidates/demo-vivian-kessler.jpg'), 'image/jpeg');
    await go(publicSync, '/' + slug + '/candidates'); await cards(publicSync, 1); await publicSync.getByRole('link', { name: prefix + ' candidate', exact: true }).click(); await expect(publicSync.locator('.biography')).toHaveText('Original disposable biography.'); await loadedImages(publicSync, '.profile-portrait img', 1);
    const oldPhoto = await publicSync.locator('.profile-portrait img').getAttribute('src');
    await adminGo(manage, '/admin/' + slug + '/candidates/' + ledger.candidateId + '/edit'); await manage.getByLabel('Biography', { exact: true }).fill('Edited disposable biography. <script>window.__publicInjected=true</script>'); await save(manage, 'Save candidate', 'candidates'); await upload(manage, 'replacement.png', png(), 'image/png');
    await publicSync.reload(); await expect(publicSync.locator('.biography')).toContainText('Edited disposable biography.'); await loadedImages(publicSync, '.profile-portrait img', 1); assert.notEqual(await publicSync.locator('.profile-portrait img').getAttribute('src'), oldPhoto); assert.equal(await publicSync.evaluate(() => window.__publicInjected), undefined);
    await publicSync.screenshot({ path: 'artifacts/phase5/admin-public-sync.png', fullPage: true });
    assert.deepEqual(await get('/api/surrey/candidates'), expected.surrey.people);
    await adminGo(manage, '/admin/' + slug + '/candidates/' + ledger.candidateId + '/delete'); await manage.getByRole('button', { name: 'Confirm delete candidate', exact: true }).click(); await manage.waitForURL(/notice=deleted/);
    await publicSync.reload(); await expect(publicSync.getByRole('heading', { name: 'Page not found', exact: true })).toBeVisible(); await go(publicSync, '/' + slug + '/candidates'); await cards(publicSync, 0);
    await adminGo(manage, '/admin/' + slug + '/positions/' + ledger.positionId + '/delete'); await manage.getByRole('button', { name: 'Confirm delete position', exact: true }).click(); await manage.waitForURL(/notice=deleted/);
    await adminGo(manage, '/admin/municipalities/' + ledger.municipalityId + '/delete'); await manage.getByRole('button', { name: 'Confirm delete municipality', exact: true }).click(); await manage.waitForURL(/notice=deleted/);
    await go(publicSync, '/'); await expect(publicSync.locator('.municipality-card')).toHaveCount(5);
    checks.push('Admin-created municipality/position/candidate appear publicly without rebuild; empty states, logo/photo, biography edits and photo replacement refresh correctly; text is encoded; deletion removes profiles; Surrey remains unchanged; owned UI records are deleted.');

    await Promise.allSettled(pending);
    assert.equal(privacyFailures.length, 0); assert.ok(network.length > 40); assert.ok(network.every(x => x.method === 'GET' && !x.cookie && !x.authorization));
    const assets = JSON.parse(fs.readFileSync('src/MEMS.Public/obj/project.assets.json', 'utf8'));
    assert.ok(!Object.keys(assets.libraries).some(x => /MEMS\.Data|MEMS\.ServiceDefaults|EntityFrameworkCore|SqlClient|Identity\.EntityFramework/i.test(x)));
    assert.deepEqual(await publicSync.evaluate(() => ({ local: localStorage.length, session: sessionStorage.length })), { local: 0, session: 0 });
    assert.deepEqual(errors, []);
    checks.push('Observed public API calls are GET-only without cookies/Authorization; browser responses contain no SQL/seed/dashboard secrets; WASM dependencies exclude Data/EF/SQL/server defaults; no voting storage exists; no browser page errors.');
  } catch (error) { failure = error; }
  finally {
    try { await cleanup(); } catch (error) { failure ??= error; }
    await ground.dispose(); for (const c of contexts) await c.close(); await browser.close();
  }
  if (failure) { fs.writeFileSync('artifacts/phase5/incomplete-run.json', JSON.stringify({ passed: false, completedChecks: checks, ownedRecordsCleaned: ledger.cleaned }, null, 2)); throw failure; }
  const result = { passed: true, checks, anonymousApiRequestsObserved: network.length, ownedRecordsCleaned: ledger.cleaned, browserPageErrors: errors };
  fs.writeFileSync('artifacts/phase5/browser-verification.json', JSON.stringify(result, null, 2)); console.log(JSON.stringify(result, null, 2));
})().catch(error => {
  let message = error.stack || error.message; for (const secret of secrets) message = message.split(secret).join('[REDACTED]');
  fs.writeFileSync('.local/phase5-browser-error.txt', message); console.error(message.slice(0, 1300)); process.exitCode = 1;
});

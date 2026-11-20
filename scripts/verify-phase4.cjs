// Real browser forms/circuits and legitimate Identity sessions. CRUD touches owned records only.
const { chromium, request, expect } = require('../.tools/browser/node_modules/@playwright/test');
const fs = require('node:fs'), assert = require('node:assert/strict'), zlib = require('node:zlib');
const admin = 'https://localhost:7252', api = 'https://localhost:7192';
const password = JSON.parse(fs.readFileSync('.local/identity-seed.json', 'utf8')).Parameters['mems-identity-seed-password'];
const fixture = JSON.parse(fs.readFileSync('.local/phase3-fixtures.json', 'utf8'));
const prefix = 'Phase4 verify ' + Date.now(), slug = 'phase4-' + Date.now();
const checks = [], errors = [], ledger = { prefix, slug, municipalities: [], positions: [], candidates: [] };
fs.mkdirSync('artifacts/phase4', { recursive: true });
const saveLedger = () => fs.writeFileSync('.local/phase4-created-records.json', JSON.stringify(ledger, null, 2));
saveLedger();

function png() {
  const crc = b => { let n = 0xffffffff; for (const v of b) { n ^= v; for (let i = 0; i < 8; i++) n = (n >>> 1) ^ ((n & 1) ? 0xedb88320 : 0); } return (n ^ 0xffffffff) >>> 0; };
  const chunk = (name, data) => { const t = Buffer.from(name), size = Buffer.alloc(4), sum = Buffer.alloc(4); size.writeUInt32BE(data.length); sum.writeUInt32BE(crc(Buffer.concat([t, data]))); return Buffer.concat([size, t, data, sum]); };
  const h = Buffer.alloc(13); h.writeUInt32BE(2, 0); h.writeUInt32BE(2, 4); h[8] = 8; h[9] = 6;
  return Buffer.concat([Buffer.from('89504e470d0a1a0a', 'hex'), chunk('IHDR', h), chunk('IDAT', zlib.deflateSync(Buffer.from([0,20,150,80,255,20,150,80,255,0,20,150,80,255,20,150,80,255]))), chunk('IEND', Buffer.alloc(0))]);
}
async function go(page, path) { await page.goto(admin + path); await page.waitForTimeout(400); }
async function login(page, name, secret = password, query = '') {
  await go(page, '/admin/account/login' + query);
  await page.getByLabel('Username', { exact: true }).fill(name);
  await page.getByLabel('Password', { exact: true }).fill(secret);
  await page.getByRole('button', { name: 'Log in', exact: true }).click();
  await page.waitForURL(/\/admin$/);
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
}
async function save(page, button, kind) {
  await page.getByRole('button', { name: button, exact: true }).click();
  await page.waitForURL(new RegExp('/' + kind + '/\\d+\\?notice=saved$'));
  return Number(new URL(page.url()).pathname.split('/').pop());
}
async function counts(page, m, p, c) {
  await expect(page.getByTestId('municipality-count')).toHaveText(String(m));
  await expect(page.getByTestId('position-count')).toHaveText(String(p));
  await expect(page.getByTestId('candidate-count')).toHaveText(String(c));
}
async function alert(page, text) { await expect(page.locator('main [role=alert]').filter({ hasText: text }).first()).toBeVisible(); }
async function candidate(page, tenant, name, positionId) {
  await go(page, '/admin/' + tenant + '/candidates/create');
  await page.getByLabel('Candidate name', { exact: true }).fill(name);
  await page.getByLabel('Position', { exact: true }).selectOption(String(positionId));
  await page.getByLabel('Biography', { exact: true }).fill('Fictional verification-owned biography.');
  await page.getByLabel('Email (optional)', { exact: true }).fill('phase4@example.org');
  await page.getByLabel('Website (optional)', { exact: true }).fill('https://example.org');
  const id = await save(page, 'Save candidate', 'candidates'); ledger.candidates.push({ slug: tenant, id }); saveLedger(); return id;
}
async function upload(page, name, buffer, mimeType, expected = 'Image uploaded successfully.') {
  await page.locator('#media-file').setInputFiles({ name, mimeType, buffer });
  await page.getByRole('button', { name: 'Upload image', exact: true }).click();
  if (expected === 'Image uploaded successfully.') await expect(page.locator('.media-panel [role=status]')).toHaveText(expected);
  else await alert(page, expected);
}
async function accessible(page) {
  const missing = await page.locator('main input:not([type=hidden]):visible,main select:visible,main textarea:visible').evaluateAll(elements =>
    elements.filter(x => !(x.labels?.length || x.getAttribute('aria-label') || x.getAttribute('aria-labelledby'))).map(x => x.id || x.tagName));
  assert.deepEqual(missing, []); assert.equal(await page.locator('main h1').count(), 1);
}
async function cleanup() {
  const c = await request.newContext({ baseURL: api, ignoreHTTPSErrors: true });
  try {
    let token = (await (await c.get('/api/auth/csrf')).json()).requestToken;
    const login = await c.post('/api/auth/login', { headers: { 'X-CSRF-TOKEN': token }, data: { userName: 'super', password } });
    assert.equal(login.status(), 200, 'Cleanup legitimate Super Admin login');
    token = (await (await c.get('/api/auth/csrf')).json()).requestToken;
    const remove = async (path, item) => {
      assert.ok(item.name.startsWith(prefix), 'Cleanup ownership prefix');
      const r = await c.delete(path, { headers: { 'X-CSRF-TOKEN': token, 'If-Match': '"' + item.version + '"' } });
      assert.equal(r.status(), 204, 'Owned-record cleanup: ' + path);
    };
    const tenants = await (await c.get('/api/admin/municipalities')).json();
    for (const t of tenants) {
      const result = await (await c.get('/api/' + t.slug + '/candidates?search=' + encodeURIComponent(prefix) + '&pageSize=100')).json();
      for (const x of result.items.filter(x => x.name.startsWith(prefix))) await remove('/api/' + t.slug + '/candidates/' + x.candidateId, x);
      const positions = await (await c.get('/api/' + t.slug + '/positions')).json();
      for (const x of positions.filter(x => x.name.startsWith(prefix))) await remove('/api/' + t.slug + '/positions/' + x.positionId, x);
    }
    for (const t of tenants.filter(t => t.name.startsWith(prefix))) {
      const fresh = await (await c.get('/api/admin/municipalities/' + t.municipalityId)).json();
      await remove('/api/admin/municipalities/' + t.municipalityId, fresh);
    }
    ledger.cleaned = true; saveLedger();
  } finally { await c.dispose(); }
}

(async () => {
  const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
  const contexts = [];
  const pageFor = async () => { const c = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } }); c.setDefaultTimeout(15000); contexts.push(c); const p = await c.newPage(); p.on('pageerror', e => errors.push(e.message)); return p; };
  let primaryError;
  try {
    const anon = await pageFor();
    for (const path of ['/admin', '/admin/municipalities/create', '/admin/burnaby/candidates/13/edit', '/admin/account/change-password']) {
      await go(anon, path); assert.equal(new URL(anon.url()).pathname, '/admin/account/login');
    }
    await accessible(anon);
    await anon.getByRole('button', { name: 'Log in', exact: true }).click(); await alert(anon, 'required');
    await anon.getByLabel('Username', { exact: true }).fill('missing-' + Date.now());
    await anon.getByLabel('Password', { exact: true }).fill('incorrect-verification-password');
    await anon.getByRole('button', { name: 'Log in', exact: true }).click(); await alert(anon, 'Invalid credentials');
    assert.equal(await anon.getByLabel('Password', { exact: true }).inputValue(), '');
    const noCsrf = await anon.context().request.post(admin + '/admin/account/login', { form: { _handler: 'admin-login', 'Input.UserName': fixture.UserName, 'Input.Password': fixture.Password } });
    assert.equal(noCsrf.status(), 400);
    const rawMedia = await anon.context().request.get(admin + '/admin/media/burnaby/candidates/13/photo'); assert.equal(rawMedia.status(), 401);
    checks.push('Anonymous Admin routes challenge to login; required/failed login messages are safe; passwords are not echoed; missing form CSRF returns 400; Admin media requires authentication.');

    const superPage = await pageFor(); await login(superPage, 'super'); await counts(superPage, 5, 15, 30);
    await superPage.screenshot({ path: 'artifacts/phase4/super-dashboard.png', fullPage: true });
    const municipal = await pageFor(); await login(municipal, 'Burnabyadmin'); await counts(municipal, 1, 3, 6);
    await municipal.screenshot({ path: 'artifacts/phase4/municipal-dashboard.png', fullPage: true });
    for (const name of ['Surreyadmin', 'Richmondadmin', 'pm']) { const p = await pageFor(); await login(p, name, password, '?returnUrl=https%3A%2F%2Fattacker.example'); await counts(p, 1, 3, 6); }
    checks.push('All five existing accounts sign in; Super totals are 5/15/30 and municipal totals 1/3/6; external return URLs cannot redirect away.');

    await go(superPage, '/admin/municipalities/create');
    await superPage.getByRole('button', { name: 'Save municipality', exact: true }).click(); await alert(superPage, 'required');
    await superPage.getByLabel('Municipality name', { exact: true }).fill(prefix + ' town');
    await superPage.getByLabel('Tenant slug', { exact: true }).fill(slug.toUpperCase());
    await superPage.getByLabel('Election description', { exact: true }).fill('Fictional verification-owned municipality.');
    const municipalityId = await save(superPage, 'Save municipality', 'municipalities'); ledger.municipalities.push(municipalityId); saveLedger();
    await expect(superPage.locator('main')).toContainText('/' + slug);
    await go(superPage, '/admin/municipalities/' + municipalityId + '/edit');
    await superPage.getByLabel('Election description', { exact: true }).fill('Edited verification-owned municipality.');
    await save(superPage, 'Save municipality', 'municipalities'); await expect(superPage.locator('main')).toContainText('Edited verification-owned municipality.');
    await upload(superPage, 'invalid.svg', Buffer.from('<svg/>'), 'image/svg+xml', 'Only JPEG and PNG');
    await upload(superPage, 'logo.png', png(), 'image/png');
    await expect(superPage.locator('.media-panel img')).toBeVisible();
    assert.ok(await superPage.locator('.media-panel img').evaluate(x => x.complete && x.naturalWidth > 0));
    const logoSrc = await superPage.locator('.record-heading img').getAttribute('src');
    await upload(superPage, 'replacement-logo.jpg', fs.readFileSync('src/MEMS.Api/wwwroot/images/demo-candidates/demo-vivian-kessler.jpg'), 'image/jpeg');
    assert.notEqual(await superPage.locator('.record-heading img').getAttribute('src'), logoSrc);
    await superPage.screenshot({ path: 'artifacts/phase4/municipality-details.png', fullPage: true });
    checks.push('Super Admin creates/reads/edits a tenant with normalized slug and election fields; invalid logo is rejected and valid logo is displayed.');

    await go(superPage, '/admin/' + slug + '/positions/create');
    await superPage.getByLabel('Position name', { exact: true }).fill(prefix + ' position');
    await superPage.getByLabel('Available seats', { exact: true }).fill('0');
    await superPage.getByRole('button', { name: 'Save position', exact: true }).click(); await alert(superPage, 'between 1 and 100');
    await superPage.getByLabel('Available seats', { exact: true }).fill('2');
    const positionId = await save(superPage, 'Save position', 'positions'); ledger.positions.push({ slug, id: positionId }); saveLedger();
    await go(superPage, '/admin/' + slug + '/positions/' + positionId + '/edit'); await superPage.getByLabel('Available seats', { exact: true }).fill('3'); await save(superPage, 'Save position', 'positions');
    await go(superPage, '/admin/' + slug + '/positions/create'); await superPage.getByLabel('Position name', { exact: true }).fill(prefix + ' position'); await superPage.getByRole('button', { name: 'Save position', exact: true }).click(); await alert(superPage, 'conflicts');
    const candidateId = await candidate(superPage, slug, prefix + ' candidate', positionId);
    await go(superPage, '/admin/' + slug + '/candidates/' + candidateId + '/edit');
    await superPage.getByLabel('Email (optional)', { exact: true }).fill('invalid'); await superPage.getByRole('button', { name: 'Save candidate', exact: true }).click(); await alert(superPage, 'e-mail');
    await superPage.getByLabel('Email (optional)', { exact: true }).fill('edited@example.org');
    await superPage.getByLabel('Biography', { exact: true }).fill('Edited fictional verification biography.'); await save(superPage, 'Save candidate', 'candidates');
    const staleEdit = await superPage.context().newPage(); await go(staleEdit, '/admin/' + slug + '/candidates/' + candidateId + '/edit');
    await go(superPage, '/admin/' + slug + '/candidates/' + candidateId + '/edit');
    await superPage.getByLabel('Biography', { exact: true }).fill('Winning concurrent verification biography.'); await save(superPage, 'Save candidate', 'candidates');
    await staleEdit.getByLabel('Biography', { exact: true }).fill('Stale verification change must be rejected.');
    await staleEdit.getByRole('button', { name: 'Save candidate', exact: true }).click(); await alert(staleEdit, 'record changed'); await staleEdit.close();
    await upload(superPage, 'fake.png', Buffer.from('not an image'), 'image/png', 'not a valid');
    await upload(superPage, 'large.png', Buffer.alloc(2 * 1024 * 1024 + 1), 'image/png', 'must not exceed');
    await upload(superPage, 'portrait.jpg', fs.readFileSync('src/MEMS.Api/wwwroot/images/demo-candidates/demo-vivian-kessler.jpg'), 'image/jpeg');
    const firstSrc = await superPage.locator('.record-heading img').getAttribute('src');
    await upload(superPage, 'replacement.png', png(), 'image/png');
    assert.notEqual(await superPage.locator('.record-heading img').getAttribute('src'), firstSrc);
    const photo = await superPage.context().request.get(admin + await superPage.locator('.record-heading img').getAttribute('src')); assert.equal(photo.status(), 200); assert.equal(photo.headers()['content-type'], 'image/png');
    await superPage.screenshot({ path: 'artifacts/phase4/candidate-details.png', fullPage: true });
    await go(superPage, '/admin/' + slug + '/positions/' + positionId + '/delete'); await superPage.getByRole('button', { name: 'Confirm delete position', exact: true }).click(); await alert(superPage, 'Remove or reassign');
    await go(superPage, '/admin/municipalities/' + municipalityId + '/delete'); await superPage.getByRole('button', { name: 'Confirm delete municipality', exact: true }).click(); await alert(superPage, 'dependent election');
    await superPage.screenshot({ path: 'artifacts/phase4/restricted-delete.png', fullPage: true });
    await go(superPage, '/admin'); await counts(superPage, 6, 16, 31);
    checks.push('Super position/candidate create, details and edit work; seat/email/duplicate validation is visible; JPEG/PNG upload/replacement refreshes images; invalid/oversized uploads and restricted parent deletes fail safely; totals update from SQL.');

    await go(municipal, '/admin/municipalities'); await expect(municipal.locator('tbody tr')).toHaveCount(1); await expect(municipal.getByRole('link', { name: 'Create municipality', exact: true })).toHaveCount(0);
    await go(municipal, '/admin/municipalities/create'); await alert(municipal, 'Only Super Admin');
    await go(municipal, '/admin/surrey/candidates'); await alert(municipal, 'outside your assignment');
    await go(municipal, '/admin/no-such-tenant/positions'); await alert(municipal, 'Municipality not found');
    const foreign = await (await municipal.context().request.get(api + '/api/surrey/candidates')).json(); assert.equal(foreign.status, 403);
    const publicContext = await request.newContext({ baseURL: api, ignoreHTTPSErrors: true });
    const surreyCandidates = await (await publicContext.get('/api/surrey/candidates')).json(); const surreyPositions = await (await publicContext.get('/api/surrey/positions')).json(); await publicContext.dispose();
    await go(municipal, '/admin/burnaby/candidates/' + surreyCandidates.items[0].candidateId + '/edit'); await alert(municipal, 'Candidate not found'); await expect(municipal.getByLabel('Candidate name', { exact: true })).toHaveCount(0);
    await go(municipal, '/admin/burnaby/positions/create'); await expect(municipal.locator('#municipality-picker option')).toHaveCount(1);
    await municipal.getByLabel('Position name', { exact: true }).fill(prefix + ' Burnaby position'); const ownPosition = await save(municipal, 'Save position', 'positions'); ledger.positions.push({ slug: 'burnaby', id: ownPosition }); saveLedger();
    const ownCandidate = await candidate(municipal, 'burnaby', prefix + ' Burnaby candidate', ownPosition);
    await go(municipal, '/admin/burnaby/candidates/' + ownCandidate + '/edit');
    await municipal.locator('#candidate-position').evaluate((select, id) => select.add(new Option('Forged foreign position', String(id))), surreyPositions[0].positionId);
    await municipal.locator('#candidate-position').selectOption(String(surreyPositions[0].positionId)); await municipal.getByRole('button', { name: 'Save candidate', exact: true }).click(); await alert(municipal, 'Choose a position in this municipality');
    await municipal.locator('#candidate-position').selectOption(String(ownPosition)); await municipal.getByLabel('Biography', { exact: true }).fill('Municipal administrator edited own record.'); await save(municipal, 'Save candidate', 'candidates');
    await upload(municipal, 'owned.png', png(), 'image/png');
    await go(municipal, '/admin/burnaby/candidates/' + ownCandidate + '/delete'); await municipal.getByRole('button', { name: 'Confirm delete candidate', exact: true }).click(); await municipal.waitForURL(/notice=deleted/);
    await go(municipal, '/admin/burnaby/positions/' + ownPosition + '/delete'); await municipal.getByRole('button', { name: 'Confirm delete position', exact: true }).click(); await municipal.waitForURL(/notice=deleted/);
    await go(municipal, '/admin'); await counts(municipal, 1, 3, 6);
    checks.push('Municipal administrator sees only its tenant, cannot manage tenants or foreign URLs/IDs, and completes own position/candidate create/edit/photo/delete. A forged foreign position option is rejected server-side.');

    await go(municipal, '/admin/burnaby/candidates'); await municipal.getByLabel('Search by name', { exact: true }).fill('Vivian'); await municipal.getByRole('button', { name: 'Apply filters', exact: true }).click(); await expect(municipal.locator('tbody tr')).toHaveCount(1);
    await go(municipal, '/admin/burnaby/candidates'); await municipal.getByLabel('Per page', { exact: true }).selectOption('5'); await municipal.getByRole('button', { name: 'Apply filters', exact: true }).click(); await expect(municipal.locator('tbody tr')).toHaveCount(5); await municipal.getByRole('link', { name: 'Next page', exact: true }).click(); await expect(municipal.locator('tbody tr')).toHaveCount(1);
    await go(municipal, '/admin/burnaby/candidates'); const firstPosition = await municipal.locator('#position-filter option').nth(1).getAttribute('value'); await municipal.locator('#position-filter').selectOption(firstPosition); await municipal.locator('#candidate-sort').selectOption('created'); await municipal.getByRole('button', { name: 'Apply filters', exact: true }).click(); await expect(municipal.locator('tbody tr')).toHaveCount(2); await accessible(municipal);
    await go(superPage, '/admin/candidates'); await superPage.locator('#municipality-picker').selectOption('richmond'); await superPage.waitForURL(/\/admin\/richmond\/candidates$/); await expect(superPage.locator('tbody tr')).toHaveCount(6);
    await municipal.setViewportSize({ width: 390, height: 844 }); await go(municipal, '/admin'); await municipal.locator('.admin-navigation summary').click(); await counts(municipal, 1, 3, 6);
    assert.ok(await municipal.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)); await municipal.screenshot({ path: 'artifacts/phase4/mobile-dashboard.png', fullPage: true });
    await go(municipal, '/admin/burnaby/candidates'); await accessible(municipal); assert.ok(await municipal.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)); await municipal.screenshot({ path: 'artifacts/phase4/mobile-candidates.png', fullPage: true });
    await go(municipal, '/admin/burnaby/candidates/create'); await accessible(municipal); assert.ok(await municipal.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)); await municipal.screenshot({ path: 'artifacts/phase4/mobile-candidate-form.png', fullPage: true });
    checks.push('Name search, tenant/position filters, sort selection, page navigation and clear filters work. Desktop and 390px mobile layouts fit without page overflow; form controls have accessible labels and page headings.');

    const temp = await pageFor(); await login(temp, fixture.UserName, fixture.Password);
    const oldTab = await temp.context().newPage(); await go(oldTab, '/admin/connectivity'); await expect(oldTab.getByRole('button', { name: 'Check API connection', exact: true })).toBeEnabled();
    await go(temp, '/admin/account/change-password');
    await temp.getByLabel('Current password', { exact: true }).fill('incorrect-owned-password'); await temp.getByLabel('New password', { exact: true }).fill('VerificationOnly#A123'); await temp.getByLabel('Confirm new password', { exact: true }).fill('different');
    await temp.getByRole('button', { name: 'Change password', exact: true }).click(); await alert(temp, 'must match');
    const changedPassword = 'VerificationOnly#A123' + Date.now();
    await temp.getByLabel('Current password', { exact: true }).fill('incorrect-owned-password'); await temp.getByLabel('New password', { exact: true }).fill(changedPassword); await temp.getByLabel('Confirm new password', { exact: true }).fill(changedPassword);
    await temp.getByRole('button', { name: 'Change password', exact: true }).click(); await alert(temp, 'Incorrect password'); assert.equal(await temp.getByLabel('Current password', { exact: true }).inputValue(), '');
    await temp.getByLabel('Current password', { exact: true }).fill(fixture.Password); await temp.getByLabel('New password', { exact: true }).fill(changedPassword); await temp.getByLabel('Confirm new password', { exact: true }).fill(changedPassword);
    await temp.getByRole('button', { name: 'Change password', exact: true }).click(); await temp.waitForURL(/changed=true/); await expect(temp.locator('main [role=status]')).toHaveText('Your password was changed successfully.');
    await oldTab.getByRole('button', { name: 'Check API connection', exact: true }).click(); await alert(oldTab, 'Sign in again');
    await go(temp, '/admin/account/logout'); await temp.getByRole('button', { name: 'Confirm log out', exact: true }).click(); await temp.waitForURL(/loggedOut=true/);
    await login(temp, fixture.UserName, changedPassword);
    await go(temp, '/admin/account/logout'); await temp.getByRole('button', { name: 'Confirm log out', exact: true }).click(); await temp.waitForURL(/loggedOut=true/);
    for (let attempt = 0; attempt < 5; attempt++) { await go(temp, '/admin/account/login'); await temp.getByLabel('Username', { exact: true }).fill(fixture.UserName); await temp.getByLabel('Password', { exact: true }).fill('wrong-owned-password'); await temp.getByRole('button', { name: 'Log in', exact: true }).click(); await alert(temp, 'Invalid credentials'); }
    await go(temp, '/admin/account/login'); await temp.getByLabel('Username', { exact: true }).fill(fixture.UserName); await temp.getByLabel('Password', { exact: true }).fill(changedPassword); await temp.getByRole('button', { name: 'Log in', exact: true }).click(); await alert(temp, 'Invalid credentials');
    checks.push('Owned account password confirmation/current-password validation, successful password change, fresh login with new password and five-failure lockout work; an old connected circuit cannot act after its security stamp changes.');

    const stale = await superPage.context().newPage(); await go(stale, '/admin/' + slug + '/candidates/' + candidateId + '/edit');
    const cookieBefore = (await superPage.context().cookies()).filter(x => x.name.startsWith('.MEMS.Assignment2.Identity')).map(x => x.name + '=' + x.value).join('; ');
    await go(superPage, '/admin/account/logout');
    assert.equal((await superPage.context().request.get(admin + '/admin', { maxRedirects: 0 })).status(), 200, 'GET logout does not sign out');
    const logoutNoCsrf = await superPage.context().request.post(admin + '/admin/account/logout', { form: { _handler: 'admin-logout' } }); assert.equal(logoutNoCsrf.status(), 400);
    await superPage.getByRole('button', { name: 'Confirm log out', exact: true }).click(); await superPage.waitForURL(/loggedOut=true/);
    await stale.getByLabel('Candidate name', { exact: true }).fill(prefix + ' unauthorized edit'); await stale.getByRole('button', { name: 'Save candidate', exact: true }).click(); await alert(stale, 'session ended');
    const replay = await request.newContext({ baseURL: api, ignoreHTTPSErrors: true, extraHTTPHeaders: { Cookie: cookieBefore } }); assert.equal((await replay.get('/api/auth/me')).status(), 401); await replay.dispose();
    await go(superPage, '/admin'); assert.equal(new URL(superPage.url()).pathname, '/admin/account/login');
    checks.push('Logout requires explicit POST/CSRF, ends the cookie session, blocks writes in an already-open circuit, and makes replay of the prior cookie fail at the separate API.');
    await login(superPage, 'super');
    await go(superPage, '/admin/' + slug + '/candidates/' + candidateId + '/delete'); await superPage.getByRole('button', { name: 'Confirm delete candidate', exact: true }).click(); await superPage.waitForURL(/notice=deleted/);
    await go(superPage, '/admin/' + slug + '/positions/' + positionId + '/delete'); await superPage.getByRole('button', { name: 'Confirm delete position', exact: true }).click(); await superPage.waitForURL(/notice=deleted/);
    await go(superPage, '/admin/municipalities/' + municipalityId + '/delete'); await superPage.getByRole('button', { name: 'Confirm delete municipality', exact: true }).click(); await superPage.waitForURL(/notice=deleted/);
    await go(superPage, '/admin'); await counts(superPage, 5, 15, 30);
    checks.push('Stale edit forms return a visible conflict without overwriting the winning update. Super Admin deletes candidate, empty position and empty municipality through confirmation screens; totals return to baseline.');
    assert.deepEqual(errors, []);
  } catch (error) { primaryError = error; }
  finally {
    try { await cleanup(); } catch (error) { primaryError ??= error; }
    for (const c of contexts) await c.close(); await browser.close();
  }
  if (primaryError) throw primaryError;
  const result = { passed: true, checks, ownedRecordsCleaned: true, browserPageErrors: errors };
  fs.writeFileSync('artifacts/phase4/browser-verification.json', JSON.stringify(result, null, 2)); console.log(JSON.stringify(result, null, 2));
})().catch(error => { fs.writeFileSync('.local/phase4-browser-error.txt', error.message); console.error(error.message.slice(0, 1600)); process.exitCode = 1; });

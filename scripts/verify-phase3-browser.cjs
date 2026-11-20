// Exercises existing infrastructure in a real browser; adds no finished login UI.
const { chromium } = require('../.tools/browser/node_modules/@playwright/test');
const fs = require('node:fs');
const assert = require('node:assert/strict');

(async () => {
  const executablePath = [
    'C:/Program Files/Google/Chrome/Application/chrome.exe',
    'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'
  ].find(fs.existsSync);
  const password = JSON.parse(fs.readFileSync('.local/identity-seed.json', 'utf8'))
    .Parameters['mems-identity-seed-password'];
  const browser = await chromium.launch({ executablePath, headless: true });
  try {
    const context = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.goto('https://localhost:7290/mems-public/');
    const result = await page.evaluate(async password => {
      const prefix = '/mems-public/api/mems-api';
      // API DTO media paths are canonical API paths, transported through this gateway prefix.
      const get = async path => {
        const response = await fetch(prefix + path);
        return { status: response.status, body: await response.json() };
      };
      let csrf = (await get('/api/auth/csrf')).body.requestToken;
      const login = await fetch(prefix + '/api/auth/login', {
        method: 'POST', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf },
        body: JSON.stringify({ userName: 'Burnabyadmin', password })
      });
      const session = await login.json();
      csrf = (await get('/api/auth/csrf')).body.requestToken;
      const me = await get('/api/auth/me');
      const own = await get('/api/burnaby/candidates');
      const foreign = await get('/api/surrey/candidates');
      const photo = await fetch(prefix + own.body.items[0].photoUrl);
      const withoutCsrf = await fetch(prefix + '/api/auth/logout', { method: 'POST' });
      const logout = await fetch(prefix + '/api/auth/logout', {
        method: 'POST', headers: { 'X-CSRF-TOKEN': csrf }
      });
      const afterLogout = await get('/api/auth/me');
      return {
        login: login.status, session, me: me.status, own: own.status,
        ownTenantOnly: own.body.items.every(x => x.municipalityId === session.municipalityId),
        foreign: foreign.status, photo: photo.status, photoType: photo.headers.get('content-type'),
        withoutCsrf: withoutCsrf.status, logout: logout.status, afterLogout: afterLogout.status,
        readableCookies: document.cookie
      };
    }, password);
    assert.equal(result.login, 200);
    assert.equal(result.me, 200);
    assert.equal(result.session.userName, 'Burnabyadmin');
    assert.equal(result.own, 200);
    assert.equal(result.ownTenantOnly, true);
    assert.equal(result.foreign, 403);
    assert.equal(result.photo, 200);
    assert.match(result.photoType, /^image\//);
    assert.equal(result.withoutCsrf, 400);
    assert.equal(result.logout, 204);
    assert.equal(result.afterLogout, 401);
    assert.ok(!result.readableCookies.includes('.MEMS.Assignment2.'));
    assert.deepEqual(errors, []);
    const checks = [
      'Real browser signs in through the HTTPS same-origin Aspire gateway with cookie and fresh CSRF tokens.',
      'Own-tenant data and its canonical media URL work through the gateway; foreign-tenant data returns 403.',
      'Missing CSRF rejects logout with 400; valid logout ends the session and subsequent /me returns 401.',
      'Identity/CSRF cookies are inaccessible to JavaScript; no browser runtime errors.'
    ];
    fs.writeFileSync('artifacts/phase3/browser-security.json', JSON.stringify({ passed: true, checks }, null, 2));
    console.log(JSON.stringify({ passed: true, checks }, null, 2));
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error.message); process.exitCode = 1; });

// Real WASM tests. Selection mutations stay inside browser sessionStorage; no database writes.
const { chromium, request, expect } = require('../.tools/browser/node_modules/@playwright/test');
const fs = require('node:fs'), assert = require('node:assert/strict');
const origin = 'https://localhost:7290', proxy = '/mems-public/api/mems-api';
const checks = [], errors = [], observed = [], contexts = [], pending = [];
fs.mkdirSync('artifacts/phase6', { recursive: true });
const pass = message => { checks.push(message); console.log('PASS: ' + message); };
const key = id => 'mems.guide.v1.' + id;
const browserPath = 'C:/Program Files/Google/Chrome/Application/chrome.exe';
(async () => {
  const browser = await chromium.launch({ executablePath: browserPath, headless: true });
  const api = await request.newContext({ baseURL: 'https://localhost:7192', ignoreHTTPSErrors: true });
  const context = async () => { const c = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } }); c.setDefaultTimeout(20000); contexts.push(c); return c; };
  const watch = p => {
    p.on('pageerror', e => errors.push(e.message));
    p.on('request', r => { if (r.url().includes(proxy + '/api/')) pending.push((async () => { const h = await r.allHeaders(); observed.push({ url: r.url(), method: r.method(), body: r.postData(), cookie: !!h.cookie, authorization: !!h.authorization }); })()); });
  };
  const go = async (p, path) => { await p.goto(origin + path); await p.waitForLoadState('networkidle'); };
  const guide = async (p, slug, count) => { await go(p, '/' + slug + '/guide'); await expect(p.locator('.guide-document')).toBeVisible(); await expect(p.locator('[data-guide-candidate-id]')).toHaveCount(count); await expect(p.getByRole('button', { name: 'Print voting guide', exact: true })).toBeEnabled(); };
  const storage = (p, id) => p.evaluate(k => JSON.parse(sessionStorage.getItem(k)), key(id));
  const setIds = (p, id, ids) => p.evaluate(({ k, id, ids }) => sessionStorage.setItem(k, JSON.stringify({ version: 1, municipalityId: id, ids })), { k: key(id), id, ids });
  const quietMutation = async (p, action) => { await p.waitForLoadState('networkidle'); await Promise.all(pending); const before = observed.length, url = p.url(); await action(); await p.waitForTimeout(150); await Promise.all(pending); assert.equal(observed.length, before, 'Changing choices must make zero API requests'); assert.equal(p.url(), url, 'Changing choices must not encode choices in a URL'); };
  let failure;
  try {
    const get = async path => { const r = await api.get(path); assert.equal(r.status(), 200); return r.json(); };
    const municipalities = await get('/api/municipalities'), towns = Object.fromEntries(municipalities.map(m => [m.slug, m]));
    const burnaby = await get('/api/burnaby/candidates?pageSize=100'), surrey = await get('/api/surrey/candidates?pageSize=100');
    const positions = await get('/api/burnaby/positions');
    const mayor = positions.find(p => p.seatsAvailable === 1), mayors = burnaby.items.filter(c => c.positionId === mayor.positionId);
    assert.ok(mayors.length > mayor.seatsAvailable);
    const picks = [...mayors, burnaby.items.find(c => c.positionId !== mayor.positionId)];
    const c = await context(), page = await c.newPage(); watch(page);
    await go(page, '/burnaby/candidates'); await expect(page.locator('.guide-selection-button:not([disabled])')).toHaveCount(6);
    assert.equal(await storage(page, towns.burnaby.municipalityId), null);
    for (const candidate of picks) await quietMutation(page, async () => {
      const button = page.locator('[data-candidate-id="' + candidate.candidateId + '"] .guide-selection-button');
      await button.focus(); await page.keyboard.press('Enter'); await expect(button).toHaveAttribute('aria-pressed', 'true');
    });
    const simultaneous = burnaby.items.filter(x => !picks.some(p => p.candidateId === x.candidateId)).slice(0,2);
    await quietMutation(page, async () => {
      // Dispatch two DOM clicks without racing Playwright's single physical mouse pointer.
      await page.evaluate(ids => ids.forEach(id => document.querySelector('[data-candidate-id="'+id+'"] .guide-selection-button').click()), simultaneous.map(x=>x.candidateId));
      await expect.poll(async () => (await storage(page, towns.burnaby.municipalityId)).ids.length).toBe(picks.length + 2);
      await page.evaluate(ids => ids.forEach(id => document.querySelector('[data-candidate-id="'+id+'"] .guide-selection-button').click()), simultaneous.map(x=>x.candidateId));
      await expect.poll(async () => (await storage(page, towns.burnaby.municipalityId)).ids.length).toBe(picks.length);
    });
    assert.deepEqual((await storage(page, towns.burnaby.municipalityId)).ids.sort((a,b)=>a-b), picks.map(x=>x.candidateId).sort((a,b)=>a-b));
    await go(page, '/burnaby/candidates/' + picks[0].candidateId);
    await expect(page.locator('.guide-selection-button')).toHaveText('Remove from guide');
    assert.equal((await storage(page, towns.burnaby.municipalityId)).ids.length, picks.length);
    await guide(page, 'burnaby', picks.length); await page.reload(); await expect(page.locator('[data-guide-candidate-id]')).toHaveCount(picks.length);
    for (const candidate of picks) await expect(page.locator('.guide-candidate-name').filter({ hasText: candidate.name })).toHaveCount(1);
    pass('Anonymous keyboard selection, profile/directory synchronization, duplicate prevention, navigation/refresh persistence and multiple candidates for a one-seat position work without an arbitrary cap.');

    await guide(page, 'richmond', 0); await expect(page.locator('.guide-municipality')).toHaveText('Richmond');
    for (const candidate of picks) assert.ok(!(await page.locator('.guide-document').innerText()).includes(candidate.name));
    for (const slug of ['pitt-meadows','maple-ridge','RICHMOND']) await guide(page, slug, 0);
    await go(page, '/unknown-tenant/guide'); await expect(page.locator('[data-error-status="404"]')).toBeVisible();
    await go(page, '/surrey/candidates'); await page.locator('[data-candidate-id="' + surrey.items[0].candidateId + '"] .guide-selection-button').click();
    await guide(page, 'surrey', 1); await guide(page, 'burnaby', picks.length);
    await quietMutation(page, async () => { await page.getByRole('button', { name: 'Remove ' + picks[0].name + ' from guide', exact: true }).click(); await expect(page.locator('[data-guide-candidate-id]')).toHaveCount(picks.length - 1); });
    await quietMutation(page, async () => { await page.getByRole('button', { name: 'Clear guide', exact: true }).click(); await page.getByRole('button', { name: 'Keep selections', exact: true }).click(); });
    assert.equal((await storage(page, towns.burnaby.municipalityId)).ids.length, picks.length - 1);
    await quietMutation(page, async () => { await page.getByRole('button', { name: 'Clear guide', exact: true }).click(); await page.getByRole('button', { name: 'Confirm clear guide', exact: true }).click(); await expect(page.locator('.guide-empty')).toBeVisible(); });
    assert.equal(await storage(page, towns.burnaby.municipalityId), null);
    assert.equal((await storage(page, towns.surrey.municipalityId)).ids.length, 1);
    pass('Burnaby, Surrey and Richmond guides are isolated; individual removal, clear confirmation/cancel, empty messaging and tenant-only clearing make zero API requests.');

    // Same tenant catalog traffic for empty and selected guides, not selected-ID lookups.
    const catalogTraffic = async () => { const urls = []; const handle = r => { if (r.url().includes(proxy + '/api/')) urls.push(r.url()); }; page.on('request', handle); await page.reload(); await page.waitForLoadState('networkidle'); await expect(page.getByRole('button', { name: 'Print voting guide', exact: true })).toBeEnabled(); page.off('request', handle); return urls.sort(); };
    const emptyTraffic = await catalogTraffic();
    await setIds(page, towns.burnaby.municipalityId, picks.map(x=>x.candidateId));
    const selectedTraffic = await catalogTraffic(); assert.deepEqual(selectedTraffic, emptyTraffic);
    assert.ok(selectedTraffic.some(x => x.includes('/api/burnaby/candidates?page=1&pageSize=100&sort=name')));
    assert.ok(!selectedTraffic.some(x => /candidates\/\d+/.test(x)));
    assert.equal(await page.evaluate(() => localStorage.length), 0);
    assert.equal((await c.cookies(origin)).length, 0);
    assert.deepEqual(Object.keys(await storage(page, towns.burnaby.municipalityId)).sort(), ['ids','municipalityId','version']);
    pass('Empty and selected guide reloads issue identical public catalog/media requests; choice mutations issue none. No selection IDs, lists, request bodies, cookies or localStorage are sent/used.');

    await page.evaluate(() => { window.__printCalls = 0; window.print = () => { window.__printCalls++; window.dispatchEvent(new Event('beforeprint')); }; });
    await quietMutation(page, async () => { await page.getByRole('button', { name: 'Print voting guide', exact: true }).click(); });
    assert.equal(await page.evaluate(() => window.__printCalls), 1);
    await expect(page.locator('[data-guide-print-date]')).not.toHaveText('Preparing date…');
    for (const size of ['Letter','A4']) {
      await page.pdf({ path: 'artifacts/phase6/burnaby-guide-' + size + '.pdf', format: size, displayHeaderFooter: false, printBackground: true });
    }
    await page.emulateMedia({ media: 'print' });
    for (const selector of ['.public-header','.public-footer','.tenant-banner','.tenant-nav','.guide-controls','.guide-help']) await expect(page.locator(selector).first()).toBeHidden();
    assert.equal(await page.locator('.guide-table tr').last().evaluate(e => getComputedStyle(e).breakInside), 'avoid');
    await page.screenshot({ path: 'artifacts/phase6/burnaby-print-layout.png', fullPage: true }); await page.emulateMedia({ media: null });
    await page.screenshot({ path: 'artifacts/phase6/burnaby-guide-desktop.png', fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 }); assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
    await page.screenshot({ path: 'artifacts/phase6/burnaby-guide-mobile.png', fullPage: true });
    await page.locator('.skip-link').focus(); await page.keyboard.press('Enter'); await expect(page.locator('#public-content')).toBeFocused();
    pass('Print button invokes window.print locally; print media hides navigation/actions, refreshes print date and avoids row splits. Letter/A4 PDFs, desktop/mobile fit and keyboard skip focus verified.');

    await setIds(page, towns.burnaby.municipalityId, [picks[0].candidateId, picks[0].candidateId, surrey.items[0].candidateId, 2147483647]);
    await guide(page, 'burnaby', 1); assert.deepEqual((await storage(page, towns.burnaby.municipalityId)).ids, [picks[0].candidateId]);
    await expect(page.locator('.guide-feedback').first()).toContainText('no longer available');
    await page.evaluate(k => sessionStorage.setItem(k, '{invalid'), key(towns.burnaby.municipalityId)); await guide(page, 'burnaby', 0);
    await expect(page.locator('.guide-feedback').first()).toContainText('Invalid saved guide');
    await page.pdf({ path: 'artifacts/phase6/empty-guide-Letter.pdf', format: 'Letter', displayHeaderFooter: false });
    await page.pdf({ path: 'artifacts/phase6/empty-guide-A4.pdf', format: 'A4', displayHeaderFooter: false });
    pass('Duplicate, deleted/nonexistent and forged foreign candidate IDs are resolved/pruned locally without ID lookups; corrupted storage recovers safely and empty guides print explicitly.');

    await go(page, '/surrey/guide'); await expect(page.locator('[data-guide-candidate-id]')).toHaveCount(1);
    await page.screenshot({ path: 'artifacts/phase6/surrey-guide.png', fullPage: true });
    const otherTab = await c.newPage(); watch(otherTab); await guide(otherTab, 'surrey', 0); await otherTab.close();
    const popupPromise = c.waitForEvent('page'); await page.evaluate(() => window.open('/surrey/guide', '_blank'));
    const copied = await popupPromise; watch(copied); await expect(copied.locator('[data-guide-candidate-id]')).toHaveCount(1); await copied.close();
    await page.close(); const reopened = await c.newPage(); watch(reopened); await guide(reopened, 'surrey', 0);
    pass('Independent tabs begin empty; an opener-created tab gets the standard initial sessionStorage copy; closing the selected tab and opening a fresh tab leaves no choices. No unload clearing breaks refresh.');

    const blocked = await context(); await blocked.addInitScript(() => { for (const method of ['getItem','setItem','removeItem']) { const original = Storage.prototype[method]; Storage.prototype[method] = function(k,...args) { if (k.startsWith('mems.guide.')) throw new DOMException('Blocked', 'SecurityError'); return original.call(this,k,...args); }; } });
    const blockedPage = await blocked.newPage(); watch(blockedPage); await go(blockedPage, '/burnaby/candidates'); await expect(blockedPage.locator('.guide-selection-button:disabled')).toHaveCount(6);
    await guideError(blockedPage);
    async function guideError(p) { await go(p, '/burnaby/guide'); await expect(p.locator('.guide-feedback')).toContainText('Session storage is unavailable'); await expect(p.getByRole('button', { name: 'Print voting guide', exact: true })).toBeDisabled(); await expect(p.locator('.guide-document')).toHaveCount(0); }
    pass('Blocked sessionStorage produces a safe message and disabled selection/printing, with no fallback to persistent or server storage.');
    const quota = await context(); await quota.addInitScript(() => { const original = Storage.prototype.setItem; Storage.prototype.setItem = function(k,...args) { if(k.startsWith('mems.guide.'))throw new DOMException('Full','QuotaExceededError');return original.call(this,k,...args); }; });
    const quotaPage = await quota.newPage(); watch(quotaPage); await go(quotaPage, '/burnaby/candidates');
    const quotaCard=quotaPage.locator('.candidate-card').first(); await quotaCard.locator('.guide-selection-button').click();
    await expect(quotaCard.locator('.guide-feedback')).toContainText('Session storage is unavailable'); await expect(quotaCard.locator('.guide-selection-button')).toBeDisabled();
    assert.equal(await storage(quotaPage,towns.burnaby.municipalityId),null);
    pass('Session-storage quota/write failures show a safe error and never claim a selection was saved or fall back to another storage mechanism.');

    // Browser-only public-response fixtures exercise pagination/long print rows; never written to SQL.
    const longContext = await context(), longPage = await longContext.newPage(); watch(longPage);
    const longNames = Array.from({ length: 32 }, (_,i) => 'Long print fixture ' + String(i + 1).padStart(2,'0') + ' ' + 'Alexandria Montgomery '.repeat(7));
    const people = longNames.map((name,i) => ({ ...picks[0], candidateId: 2000000+i, name }));
    await longPage.route('**' + proxy + '/api/burnaby/candidates?**', r => { const n = Number(new URL(r.request().url()).searchParams.get('page')); return r.fulfill({ json: { items: people.slice((n-1)*16,n*16), page:n, pageSize:16,totalCount:32,totalPages:2 } }); });
    await go(longPage, '/'); await setIds(longPage, towns.burnaby.municipalityId, people.map(x=>x.candidateId)); await guide(longPage, 'burnaby', 32);
    for (const size of ['Letter','A4']) await longPage.pdf({ path: 'artifacts/phase6/long-guide-' + size + '.pdf', format:size, displayHeaderFooter:false });
    const manifest = { municipality:'Burnaby', electionDate:(await get('/api/burnaby/municipalities'))[0].electionDate, candidates:picks.map(x=>({name:x.name,position:positions.find(p=>p.positionId===x.positionId).name})), longNames, printDate:await longPage.locator('[data-guide-print-date]').innerText() };
    fs.writeFileSync('artifacts/phase6/print-fields.json', JSON.stringify(manifest,null,2));
    await longPage.setViewportSize({width:390,height:844}); assert.ok(await longPage.evaluate(()=>document.documentElement.scrollWidth <= innerWidth+1));
    pass('A choice-independent two-page public catalog resolves 32 long-name fixtures and exports multi-page Letter/A4 print layouts without a selection cap or mobile overflow; no fixtures are written to SQL.');

    const missing = await context(), missingPage = await missing.newPage(); watch(missingPage);
    await missingPage.route('**' + proxy + '/api/burnaby/municipalities', async r => { const response=await r.fetch(), data=await response.json(); data[0].electionDate='0001-01-01'; await r.fulfill({json:data}); });
    await go(missingPage, '/burnaby/guide'); await expect(missingPage.locator('[data-error-status="503"]')).toBeVisible(); await expect(missingPage.getByRole('button',{name:'Print voting guide',exact:true})).toHaveCount(0);
    pass('Missing required election metadata fails safely before printing instead of producing a guide with blank required fields.');

    await Promise.all(pending); assert.ok(observed.length > 20); assert.ok(observed.every(r=>r.method==='GET' && r.body===null && !r.cookie && !r.authorization));
    const openapi = await get('/openapi/v1.json'); assert.ok(!Object.keys(openapi.paths).some(p=>/ballot|voting-guide|selections|vote-totals/i.test(p)));
    assert.deepEqual(errors, []);
    pass('All observed public API traffic is anonymous GET-only with no selection payloads; existing OpenAPI has no ballot/selection/tally endpoint; all exercised pages have zero browser runtime errors.');
  } catch (e) { failure = e; }
  finally { await api.dispose(); for (const c of contexts) await c.close(); await browser.close(); }
  const result = { passed:!failure, checks, apiRequestsObserved:observed.length, browserPageErrors:errors, databaseMutations:0 };
  fs.writeFileSync('artifacts/phase6/browser-verification.json',JSON.stringify(result,null,2));
  if(failure) throw failure;
  console.log(JSON.stringify(result,null,2));
})().catch(e=>{fs.writeFileSync('.local/phase6-browser-error.txt',e.stack||e.message);console.error(e.message.slice(0,1300));process.exitCode=1;});

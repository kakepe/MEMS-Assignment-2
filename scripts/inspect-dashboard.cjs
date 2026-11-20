const { chromium } = require('../.tools/browser/node_modules/@playwright/test');
const fs = require('node:fs');
const path = require('node:path');

(async () => {
  const bytes = fs.readFileSync('.local/apphost-direct.log');
  const log = bytes.toString(bytes[0] === 0xff ? 'utf16le' : 'utf8');
  const login = log.match(/https:\/\/localhost:\d+\/login\?t=[^\s]+/);
  if (!login) throw new Error('Dashboard login URL not yet available.');
  const browserPath = ['C:/Program Files/Google/Chrome/Application/chrome.exe', 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'].find(fs.existsSync);
  const browser = await chromium.launch({ executablePath: browserPath, headless: true });
  try {
    const context = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await context.newPage();
    await page.goto(login[0]);
    await page.waitForTimeout(2500);
    const body = await page.locator('body').innerText();
    fs.writeFileSync('.local/dashboard-text.txt', body);
    const links = await page.locator('a[href]').evaluateAll(elements => elements.map(e => ({ text: e.innerText, href: e.href })).filter(e => e.href.startsWith('http') && !e.href.includes('?')));
    fs.writeFileSync('.local/dashboard-links.json', JSON.stringify(links, null, 2));
    console.log(JSON.stringify(links.filter(e => e.href.includes('localhost')), null, 2));
    fs.mkdirSync('artifacts/phase1', { recursive: true });
    await page.screenshot({ path: path.resolve('artifacts/phase1/dashboard.png'), fullPage: true });
    console.log('Dashboard opened and screenshot captured.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error.message); process.exitCode = 1; });

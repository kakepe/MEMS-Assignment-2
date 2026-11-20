const {chromium,expect}=require('../.tools/browser/node_modules/@playwright/test');
const fs=require('node:fs'),assert=require('node:assert/strict');
(async()=>{
  const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
  try{
    const context=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width:1440,height:1000}});context.setDefaultTimeout(15000);
    const page=await context.newPage();await page.goto('https://localhost:7252/admin/account/login');
    await page.getByLabel('Username',{exact:true}).focus();await page.keyboard.press('Tab');await expect(page.getByLabel('Password',{exact:true})).toBeFocused();
    await page.keyboard.press('Tab');await expect(page.getByRole('button',{name:'Log in',exact:true})).toBeFocused();
    await page.getByLabel('Username',{exact:true}).fill('super');
    await page.getByLabel('Password',{exact:true}).fill(JSON.parse(fs.readFileSync('.local/identity-seed.json','utf8')).Parameters['mems-identity-seed-password']);
    await page.getByRole('button',{name:'Log in',exact:true}).click();await page.waitForURL('https://localhost:7252/admin');
    await expect(page.getByTestId('candidate-count')).toHaveText('30');
    await page.setViewportSize({width:390,height:844});await page.locator('.admin-navigation summary').focus();await page.keyboard.press('Enter');
    assert.equal(await page.locator('.admin-navigation').getAttribute('open'),null);await page.keyboard.press('Enter');
    assert.notEqual(await page.locator('.admin-navigation').getAttribute('open'),null);
    const bytes=fs.readFileSync('.local/apphost-direct.log');const log=bytes.toString(bytes[0]===0xff?'utf16le':'utf8');
    const login=log.match(/https:\/\/localhost:\d+\/login\?t=[^\s]+/);assert.ok(login);
    const dashboard=await context.newPage();await dashboard.setViewportSize({width:1440,height:1000});await dashboard.goto(login[0]);
    await expect(dashboard.getByText('mems-admin',{exact:true}).first()).toBeVisible();
    await dashboard.screenshot({path:'artifacts/phase4/aspire-dashboard.png',fullPage:true});
    for(const [route,term,file] of [['structuredlogs','Admin data operation completed','admin-structuredlogs.png'],['traces','GET /admin','admin-traces.png']]){
      await dashboard.goto('https://localhost:17048/'+route);
      await dashboard.locator('input[type=search]').fill(term);
      await dashboard.locator('input[type=search]').press('Enter');
      // Match actual row content, not the Resource filter's static "mems-admin" option.
      await expect(dashboard.locator('body')).toContainText(route==='traces'?/GET\s+\/admin/:term,{timeout:30000});
      await dashboard.screenshot({path:'artifacts/phase4/'+file,fullPage:true});
    }
    const result={passed:true,checks:['Login fields/buttons support keyboard tab order.','Responsive navigation expands/collapses with keyboard Enter.','Authenticated Aspire dashboard opens and shows service resources.','Structured Admin operation logs and Admin request traces are visible in Aspire.']};
    fs.writeFileSync('artifacts/phase4/accessibility-observability.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
  }finally{await browser.close();}
})().catch(e=>{fs.writeFileSync('.local/phase4-observability-error.txt',e.message);console.error(e.message.slice(0,600));process.exitCode=1});

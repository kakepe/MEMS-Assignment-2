// Keep one real browser tab alive while the operator restarts the same SQL container and Aspire.
const {chromium,expect}=require('../.tools/browser/node_modules/@playwright/test');
const fs=require('node:fs'),crypto=require('node:crypto'),assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 try{
  const context=await browser.newContext({ignoreHTTPSErrors:true}),page=await context.newPage();
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto('https://localhost:7290/burnaby/candidates');const first=page.locator('.candidate-card').first();
  const id=Number(await first.getAttribute('data-candidate-id'));await first.locator('.guide-selection-button').click();
  await page.getByRole('link',{name:'My voting guide',exact:true}).click();await expect(page.locator('[data-guide-candidate-id]')).toHaveCount(1);
  const token=crypto.randomUUID();fs.writeFileSync('.local/phase6-restart-ready.json',JSON.stringify({ready:true,token}));console.log('Ready: live browser tab holds a selection locally for the Aspire/SQL restart check.');
  const deadline=Date.now()+300000;
  while(true){
   if(Date.now()>deadline)throw Error('Restart handshake timed out; browser closed without altering server records.');
   try{if(JSON.parse(fs.readFileSync('.local/phase6-restart-resume.json','utf8')).token===token)break;}catch{}
   await new Promise(r=>setTimeout(r,500));
  }
  await page.reload();await expect(page.locator('[data-guide-candidate-id]')).toHaveCount(1);assert.equal(Number(await page.locator('[data-guide-candidate-id]').getAttribute('data-guide-candidate-id')),id);
  assert.deepEqual(errors,[]);const result={passed:true,selectionSurvivedSameTabAcrossSqlAndAspireRestart:true,browserPageErrors:errors};
  fs.writeFileSync('artifacts/phase6/session-restart.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result));
 }finally{await browser.close();}
})().catch(e=>{console.error(e.message.slice(0,900));process.exitCode=1;});

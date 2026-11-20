// Only the separately authenticated fixture driver writes one owned candidate. WASM choices never leave the tab.
const {chromium,request,expect}=require('../.tools/browser/node_modules/@playwright/test');
const fs=require('node:fs'),assert=require('node:assert/strict');
const origin='https://localhost:7290', base='https://localhost:7192';
const prefix='Phase6 verify '+Date.now(), state={prefix,candidateId:null,cleaned:false};
const ledger=()=>fs.writeFileSync('.local/phase6-owned-records.json',JSON.stringify(state,null,2));
(async()=>{
  const admin=await request.newContext({baseURL:base,ignoreHTTPSErrors:true});
  const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
  const context=await browser.newContext({ignoreHTTPSErrors:true});
  let token, page, failure; const browserErrors=[], traffic=[];
  try{
    token=(await(await admin.get('/api/auth/csrf')).json()).requestToken;
    const secret=JSON.parse(fs.readFileSync('.local/identity-seed.json','utf8')).Parameters['mems-identity-seed-password'];
    assert.equal((await admin.post('/api/auth/login',{headers:{'X-CSRF-TOKEN':token},data:{userName:'super',password:secret}})).status(),200);
    token=(await(await admin.get('/api/auth/csrf')).json()).requestToken;
    const positions=await(await admin.get('/api/burnaby/positions')).json();
    const payload={name:prefix,positionId:positions[0].positionId,profile:'Owned Phase 6 synchronization fixture.',email:null,website:null};
    const create=await admin.post('/api/burnaby/candidates',{headers:{'X-CSRF-TOKEN':token},data:payload});assert.equal(create.status(),201);
    const candidate=await create.json(); state.candidateId=candidate.candidateId;ledger();
    page=await context.newPage();page.on('pageerror',e=>browserErrors.push(e.message));page.on('request',r=>{if(r.url().includes('/mems-public/api/'))traffic.push({url:r.url(),method:r.method(),body:r.postData()});});
    await page.goto(origin+'/burnaby/candidates/'+candidate.candidateId);
    await page.getByRole('button',{name:'Add '+prefix+' to guide',exact:true}).click();
    await page.getByRole('link',{name:'My voting guide',exact:true}).click();await expect(page.locator('[data-guide-candidate-id]')).toHaveCount(1);
    const update={...payload,name:(prefix+' Alexandria Montgomery ').padEnd(100,'x'),positionId:positions[1].positionId};
    const changed=await admin.put('/api/burnaby/candidates/'+candidate.candidateId,{headers:{'X-CSRF-TOKEN':token,'If-Match':'"'+candidate.version+'"'},data:update});assert.equal(changed.status(),200);
    await page.reload();await expect(page.locator('.guide-candidate-name')).toHaveText(update.name);await expect(page.locator('.guide-position-name')).toHaveText(positions[1].name);
    await expect(page.getByRole('button',{name:'Print voting guide',exact:true})).toBeEnabled();
    await page.pdf({path:'artifacts/phase6/live-edited-guide-Letter.pdf',format:'Letter',displayHeaderFooter:false});
    const updated=await changed.json();assert.equal((await admin.delete('/api/burnaby/candidates/'+candidate.candidateId,{headers:{'X-CSRF-TOKEN':token,'If-Match':'"'+updated.version+'"'}})).status(),204);
    state.cleaned=true;ledger();
    await page.reload();await expect(page.locator('.guide-empty')).toBeVisible();await expect(page.locator('.guide-feedback')).toContainText('no longer available');
    assert.equal(await page.evaluate(k=>sessionStorage.getItem(k),'mems.guide.v1.'+candidate.municipalityId),null);
    const adminPage=await context.newPage();await adminPage.goto('https://localhost:7252/admin');assert.equal(await adminPage.evaluate(()=>sessionStorage.length),0);
    assert.ok(traffic.every(r=>r.method==='GET'&&r.body===null));assert.deepEqual(browserErrors,[]);
    const result={passed:true,checks:['An owned API-created candidate can be selected anonymously without writing choices to the API.','A 100-character candidate-name edit and position change appear in the existing guide after refresh.','Deleting the owned candidate removes its stale choice locally; storage key is cleared.','The Admin origin has no public selection storage; public traffic remains GET-only.'],ownedCandidateCleaned:true};
    fs.writeFileSync('artifacts/phase6/admin-sync.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
  }catch(e){failure=e;}
  finally{
    if(state.candidateId&&!state.cleaned){const r=await admin.get('/api/burnaby/candidates/'+state.candidateId);if(r.status()===200){const own=await r.json();assert.ok(own.name.startsWith(prefix));assert.equal((await admin.delete('/api/burnaby/candidates/'+state.candidateId,{headers:{'X-CSRF-TOKEN':token,'If-Match':'"'+own.version+'"'}})).status(),204);}state.cleaned=true;ledger();}
    await context.close();await browser.close();await admin.dispose();
  }
  if(failure)throw failure;
})().catch(e=>{console.error(e.message.slice(0,1200));process.exitCode=1;});

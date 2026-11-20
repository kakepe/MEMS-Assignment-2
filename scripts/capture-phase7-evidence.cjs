// Real application screenshots and sanitized request/query exports; no mocked responses or new app endpoints.
const {chromium,request,expect}=require('../.tools/browser/node_modules/@playwright/test');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {pathToFileURL}=require('node:url');
const dest='docs/final/evidence',apiBase='https://localhost:7192',publicBase='https://localhost:7290';
fs.mkdirSync(dest,{recursive:true});
const html=s=>s.replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const style='body{font:16px system-ui;margin:36px;color:#183447;background:#f6f8fa}h1{font-size:30px}h2{font-size:21px}table{border-collapse:collapse;width:100%;background:white}td,th{text-align:left;padding:10px;border:1px solid #ccd6df}pre{background:white;border:1px solid #ccd6df;padding:18px;white-space:pre-wrap;overflow-wrap:anywhere;font:14px Consolas,monospace}.note{color:#40576b}';
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 const contexts=[],api=await request.newContext({baseURL:apiBase,ignoreHTTPSErrors:true});
 const secret=JSON.parse(fs.readFileSync('.local/identity-seed.json','utf8')).Parameters['mems-identity-seed-password'];
 const context=async()=>{const c=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width:1440,height:1000}});contexts.push(c);return c;};
 const go=async(p,url)=>{await p.goto(url);await p.waitForLoadState('networkidle');};
 const photo=async(p,name)=>p.screenshot({path:dest+'/'+name+'.png',fullPage:true});
 const results=[];const shotTimes={};
 try{
  const spec=await(await api.get('/openapi/v1.json')).json();
  for(const resource of ['positions','candidates']) for(const [method,suffix] of [['get',''],['post',''],['get','/{id}'],['put','/{id}'],['delete','/{id}']]) assert.ok(spec.paths['/api/{slug}/'+resource+suffix]?.[method],method+' '+resource+suffix);
  for(const [method,suffix] of [['get',''],['post',''],['get','/{id}'],['put','/{id}'],['delete','/{id}']]) assert.ok(spec.paths['/api/admin/municipalities'+suffix]?.[method]);
  assert.ok(spec.components.schemas.CandidateRequest);assert.ok(!Object.keys(spec.paths).some(x=>/ballot|vote-totals|selections/.test(x)));
  fs.writeFileSync(dest+'/openapi.json',JSON.stringify(spec,null,2));
  const probe=async(method,url,expected,data,client=api)=>{
    const response=await client.fetch(url,{method,...(data?{data}:{})});assert.equal(response.status(),expected);
    const body=await response.json();results.push({method,url:apiBase+url,status:response.status(),body});return body;
  };
  const people=await probe('GET','/api/burnaby/candidates?page=1&pageSize=3&sort=name',200);
  const surreyPositions=await(await api.get('/api/surrey/positions')).json();
  await probe('GET','/api/burnaby/candidates?positionId='+surreyPositions[0].positionId,400);
  await probe('GET','/api/unknown-tenant/candidates',404);
  const positions=await(await api.get('/api/burnaby/positions')).json();
  await probe('POST','/api/burnaby/candidates',401,{name:'Phase7 unauthorized probe',positionId:positions[0].positionId,profile:'Must not be persisted.'});
  const municipal=await request.newContext({baseURL:apiBase,ignoreHTTPSErrors:true});
  try{
    let token=(await(await municipal.get('/api/auth/csrf')).json()).requestToken;
    assert.equal((await municipal.post('/api/auth/login',{headers:{'X-CSRF-TOKEN':token},data:{userName:'Burnabyadmin',password:secret}})).status(),200);
    await probe('GET','/api/surrey/candidates',403,undefined,municipal);
  }finally{await municipal.dispose();}
  fs.writeFileSync(dest+'/api-results.json',JSON.stringify({capturedAt:new Date().toISOString(),provenance:'Actual HTTPS requests; 401 probe did not create a record. No auth tokens/cookies exported.',results},null,2));
  const c=await context(),p=await c.newPage();p.on('pageerror',e=>{throw e;});
  const logBytes=fs.readFileSync('.local/apphost-direct.log'),log=logBytes.toString(logBytes[0]===255?'utf16le':'utf8');const dashboard=log.match(/https:\/\/localhost:\d+\/login\?t=[^\s]+/);assert.ok(dashboard);
  await go(p,dashboard[0]);await expect(p.getByText('mems-api',{exact:true}).first()).toBeVisible();await photo(p,'aspire-dashboard');shotTimes.aspire=new Date().toISOString();
  await go(p,'https://localhost:7252/admin');await p.getByLabel('Username',{exact:true}).fill('super');await p.getByLabel('Password',{exact:true}).fill(secret);await p.getByRole('button',{name:'Log in',exact:true}).click();await p.waitForURL('https://localhost:7252/admin');
  await expect(p.getByTestId('candidate-count')).toHaveText('30');await photo(p,'admin-dashboard');
  await go(p,'https://localhost:7252/admin/candidates');await photo(p,'admin-candidates');
  const pub=await(await context()).newPage();
  for(const slug of ['burnaby','surrey','richmond']){await go(pub,publicBase+'/'+slug+'/candidates');await expect(pub.locator('.candidate-card')).toHaveCount(6);await expect.poll(()=>pub.locator('.candidate-card img').evaluateAll(xs=>xs.every(x=>x.complete&&x.naturalWidth>0))).toBe(true);await photo(pub,'public-'+slug);}
  await go(pub,publicBase+'/burnaby/candidates/'+people.items[0].candidateId);await pub.locator('.guide-selection-button').click();await pub.getByRole('link',{name:'My voting guide',exact:true}).click();await expect(pub.locator('[data-guide-candidate-id]')).toHaveCount(1);await expect(pub.getByRole('button',{name:'Print voting guide',exact:true})).toBeEnabled();await photo(pub,'public-guide');
  await pub.setViewportSize({width:390,height:844});await photo(pub,'public-guide-mobile');
  await go(p,apiBase+'/api/docs');await photo(p,'api-documentation');await go(p,apiBase+'/api/burnaby/candidates?page=1&pageSize=3&sort=name');await photo(p,'api-live-response');
  const apiHtml='<!doctype html><html lang="en"><meta charset="utf-8"><title>Actual API verification export</title><style>'+style+'</style><h1>Web API  -  actual HTTPS verification</h1><p class="note">Captured '+new Date().toISOString()+'. Real local requests; no mock data. Authentication cookies/tokens omitted.</p><table><tr><th>Method</th><th>Request</th><th>Actual status</th></tr>'+results.map(r=>'<tr><td>'+r.method+'</td><td>'+html(r.url)+'</td><td>'+r.status+'</td></tr>').join('')+'</table><h2>Actual public response (first result)</h2><pre>'+html(JSON.stringify(results[0].body,null,2))+'</pre></html>';
  fs.writeFileSync(dest+'/api-results.html',apiHtml);await go(p,pathToFileURL(path.resolve(dest+'/api-results.html')).href);await photo(p,'api-testing');
  const sql=JSON.parse(fs.readFileSync(dest+'/sql-snapshot.json','utf8').replace(/^\uFEFF/,''));
  const sqlHtml='<!doctype html><html lang="en"><meta charset="utf-8"><title>SQL Server live query export</title><style>'+style+'</style><h1>SQL Server  -  read-only live query export</h1><p class="note">'+html(sql.database)+'  |  '+html(sql.capturedAt)+'  |  actual query results rendered for legibility (not an SSMS screenshot). No Identity row values exported.</p><pre>SELECT name FROM sys.tables WHERE is_ms_shipped = 0;\nSELECT MigrationId, ProductVersion FROM __EFMigrationsHistory;\nSELECT (SELECT COUNT(*) FROM Municipalities) AS municipalities, (SELECT COUNT(*) FROM Positions) AS positions, (SELECT COUNT(*) FROM Candidates) AS candidates, (SELECT COUNT(*) FROM AspNetUsers) AS users, (SELECT COUNT(*) FROM AspNetRoles) AS roles;</pre><h2>Actual counts</h2><table><tr>'+Object.keys(sql.counts).map(x=>'<th>'+html(x)+'</th>').join('')+'</tr><tr>'+Object.values(sql.counts).map(x=>'<td>'+x+'</td>').join('')+'</tr></table><h2>Actual SQL tables ('+sql.tables.length+')</h2><table>'+sql.tables.map(t=>'<tr><td>'+html(t)+'</td><td>'+sql.columns.filter(c=>c.tableName===t).length+' columns</td></tr>').join('')+'</table><h2>Applied migrations</h2><pre>'+html(JSON.stringify(sql.migrations,null,2))+'</pre></html>';
  fs.writeFileSync(dest+'/sql-query-export.html',sqlHtml);await go(p,pathToFileURL(path.resolve(dest+'/sql-query-export.html')).href);await photo(p,'sql-server');
  fs.writeFileSync('artifacts/phase7/evidence-verification.json',JSON.stringify({passed:true,capturedAt:new Date().toISOString(),allFiveRequiredCategories:true,apiStatuses:results.map(r=>r.status),openApiOperations:Object.values(spec.paths).reduce((n,x)=>n+Object.keys(x).filter(k=>['get','post','put','delete','patch'].includes(k)).length,0),sqlTables:sql.tables.length,mockedData:false,sqlScreenshotIsReadOnlyExport:true},null,2));
  console.log('Captured all five real-evidence categories and verified live OpenAPI/HTTP statuses; SQL screenshot is labeled as an actual read-only query export.');
 }finally{await api.dispose();for(const c of contexts)await c.close();await browser.close();}
})().catch(e=>{console.error(e.message.slice(0,1200));process.exitCode=1;});


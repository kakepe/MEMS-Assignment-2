// Real Identity cookie sessions and HTTP requests only; no fake principal/header authentication.
const { request } = require('../.tools/browser/node_modules/@playwright/test');
const fs = require('node:fs'), path = require('node:path'), zlib = require('node:zlib');
const assert = require('node:assert/strict');
const base = 'https://localhost:7192';
const password = JSON.parse(fs.readFileSync('.local/identity-seed.json','utf8')).Parameters['mems-identity-seed-password'];
const fixture = JSON.parse(fs.readFileSync('.local/phase3-fixtures.json','utf8'));
const checks=[], examples={}, ledger={candidates:[],positions:[],municipalities:[]};
const contexts=[];
const prefix='Phase3 verification '+Date.now(), slug='phase3-'+Date.now();
function saveLedger(){fs.writeFileSync('.local/phase3-created-records.json',JSON.stringify(ledger,null,2));}
async function client(){const ctx=await request.newContext({baseURL:base,ignoreHTTPSErrors:true,timeout:30000});contexts.push(ctx);return {ctx,token:null};}
async function csrf(c){const r=await c.ctx.get('/api/auth/csrf');assert.equal(r.status(),200,'CSRF issuance');c.token=(await r.json()).requestToken;return c.token;}
async function call(c,method,url,status,data,version,extra={}){
  const headers={...extra};if(!['GET','HEAD','OPTIONS'].includes(method)&&c.token)headers['X-CSRF-TOKEN']=c.token;
  if(version)headers['If-Match']='"'+version+'"';
  const r=await c.ctx.fetch(url,{method,headers,...(data===undefined?{}:{data})});
  assert.equal(r.status(),status,method+' '+url+' status');
  if(status>=400){const p=await r.json();assert.equal(p.status,status,'Problem Details status');assert.ok(!/SqlException|C:\\Users|PasswordHash|connectionString|stackTrace/.test(JSON.stringify(p)),'Safe errors');}
  return r;
}
async function login(name,secret=password){const c=await client();await csrf(c);const anonymousToken=c.token;const r=await call(c,'POST','/api/auth/login',200,{userName:name,password:secret});await csrf(c);return {...c,anonymousToken,session:await r.json()};}
async function get(c,url){return (await call(c,'GET',url,200)).json();}
async function upload(c,url,version,name,buffer,mimeType='image/png',status=200){
  const r=await c.ctx.post(url,{headers:{'X-CSRF-TOKEN':c.token,'If-Match':'"'+version+'"'},multipart:{file:{name,mimeType,buffer}}});
  assert.equal(r.status(),status,'Upload '+name+' status');if(status>=400)assert.equal((await r.json()).status,status);return r;
}
function png(width=2,height=2){
  const crc=b=>{let n=0xffffffff;for(const v of b){n^=v;for(let i=0;i<8;i++)n=(n>>>1)^((n&1)?0xedb88320:0);}return (n^0xffffffff)>>>0;};
  const chunk=(type,data)=>{const t=Buffer.from(type),size=Buffer.alloc(4),sum=Buffer.alloc(4);size.writeUInt32BE(data.length);sum.writeUInt32BE(crc(Buffer.concat([t,data])));return Buffer.concat([size,t,data,sum]);};
  const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(width,0);ihdr.writeUInt32BE(height,4);ihdr[8]=8;ihdr[9]=6;
  const rows=Buffer.alloc((width*4+1)*height,0);for(let y=0;y<height;y++)for(let x=0;x<width;x++){const at=y*(width*4+1)+1+x*4;rows[at]=40;rows[at+1]=120;rows[at+2]=180;rows[at+3]=255;}
  return Buffer.concat([Buffer.from('89504e470d0a1a0a','hex'),chunk('IHDR',ihdr),chunk('IDAT',zlib.deflateSync(rows)),chunk('IEND',Buffer.alloc(0))]);
}
function dtoSafe(value){
  if(Array.isArray(value)){value.forEach(dtoSafe);return;}
  if(value&&typeof value==='object')for(const [key,item] of Object.entries(value)){
    assert.ok(!/^(passwordHash|securityStamp|concurrencyStamp|photoFileName|logoFileName|demoSeedKey|userRoles|users|roles|connectionString)$/i.test(key),'DTO excludes '+key);dtoSafe(item);
  }
}

(async()=>{
  const anon=await client();let superAdmin;
  try{
    const directory=await get(anon,'/api/municipalities');assert.equal(directory.length,5);dtoSafe(directory);
    const bySlug=Object.fromEntries(directory.map(x=>[x.slug,x]));
    const bp=await get(anon,'/api/burnaby/positions'),sp=await get(anon,'/api/surrey/positions');
    const bc=await get(anon,'/api/burnaby/candidates'),sc=await get(anon,'/api/surrey/candidates');
    assert.equal(bc.totalCount,6);assert.equal(sc.totalCount,6);assert.ok(bc.items.every(x=>x.municipalityId===bySlug.burnaby.municipalityId));dtoSafe(bc);
    await call(anon,'GET','/api/no-such-tenant/candidates',404);
    await call(anon,'GET','/api/BURNABY/candidates',404);
    await call(anon,'GET','/api/burnaby/candidates/'+sc.items[0].candidateId,404);
    await call(anon,'GET','/api/burnaby/positions/'+sp[0].positionId,404);
    await call(anon,'GET','/api/burnaby/municipalities/'+bySlug.surrey.municipalityId,404);
    await call(anon,'GET','/api/admin/municipalities',401);
    for(const [method,url,data] of [['POST','/api/burnaby/positions',{name:prefix,seatsAvailable:1}],['PUT','/api/burnaby/candidates/'+bc.items[0].candidateId,{}],['DELETE','/api/burnaby/candidates/'+bc.items[0].candidateId],['POST','/api/admin/municipalities',{}]])await call(anon,method,url,401,data);
    await call(anon,'POST','/api/auth/login',400,{userName:'super',password});
    checks.push('Anonymous tenant discovery/reads work; unknown/foreign IDs fail; all anonymous administrative writes return 401; login CSRF is enforced.');

    superAdmin=await login('super');const burnaby=await login('Burnabyadmin'),surrey=await login('Surreyadmin');
    for(const name of ['pm','Richmondadmin']){const c=await login(name);assert.equal((await get(c,'/api/auth/me')).userName,name);}
    assert.equal((await get(superAdmin,'/api/admin/municipalities')).length,5);
    assert.equal((await get(burnaby,'/api/municipalities')).length,1);
    assert.equal((await get(burnaby,'/api/auth/me')).municipalityId,bySlug.burnaby.municipalityId);
    assert.ok(superAdmin.session.isSuperAdmin);
    await call(burnaby,'GET','/api/admin/municipalities',403);
    await call(burnaby,'POST','/api/admin/municipalities',403,{});
    await call(burnaby,'GET','/api/surrey/candidates',403);
    await call(surrey,'GET','/api/burnaby/positions',403);
    await call(burnaby,'GET','/api/surrey/candidates/'+sc.items[0].candidateId,403,undefined,undefined,{'X-Tenant-Id':String(bySlug.surrey.municipalityId),'X-Role':'Super Admin'});
    await call(burnaby,'POST','/api/burnaby/positions',400,{name:prefix,seatsAvailable:1,municipalityId:bySlug.surrey.municipalityId});
    await call(burnaby,'POST','/api/burnaby/positions',403,{name:prefix,seatsAvailable:1},undefined,{Origin:'https://attacker.example'});
    const noToken=await client();const missing=await noToken.ctx.post('/api/burnaby/positions',{data:{name:prefix,seatsAvailable:1}});assert.equal(missing.status(),401);
    const badCsrf=await burnaby.ctx.post('/api/burnaby/positions',{headers:{'X-CSRF-TOKEN':burnaby.anonymousToken},data:{name:prefix,seatsAvailable:1}});assert.equal(badCsrf.status(),400);
    const crossToken=await burnaby.ctx.post('/api/burnaby/positions',{headers:{'X-CSRF-TOKEN':superAdmin.token},data:{name:prefix,seatsAvailable:1}});assert.equal(crossToken.status(),400);
    checks.push('All five required accounts authenticate legitimately; current stored role/tenant assignments, forged headers/body IDs, origin checks and principal-bound CSRF tokens are enforced.');

    let mun=await (await call(superAdmin,'POST','/api/admin/municipalities',201,{slug:slug.toUpperCase(),name:prefix,province:'British Columbia',electionDate:'2026-10-17',description:'Verification-owned tenant.',brandColor:'#245678',publicTitle:'Verification guide'})).json();
    ledger.municipalities.push(mun.municipalityId);saveLedger();assert.equal(mun.slug,slug);dtoSafe(mun);
    examples.municipality=mun;
    const malformedVersion=await superAdmin.ctx.delete('/api/admin/municipalities/'+mun.municipalityId,{headers:{'X-CSRF-TOKEN':superAdmin.token,'If-Match':'"'}});
    assert.equal(malformedVersion.status(),400);assert.equal((await malformedVersion.json()).status,400);
    assert.equal((await get(anon,'/api/'+mun.slug+'/municipalities')).length,1);
    let previous=mun.version;
    const munBody={slug:slug+'-edited',name:prefix+' edited',province:mun.province,electionDate:mun.electionDate,description:'Updated verification-owned tenant.',brandColor:'#345678',publicTitle:'Updated guide'};
    mun=await (await call(superAdmin,'PUT','/api/admin/municipalities/'+mun.municipalityId,200,munBody,previous)).json();
    await call(superAdmin,'PUT','/api/admin/municipalities/'+mun.municipalityId,409,munBody,previous);
    await call(superAdmin,'PUT','/api/admin/municipalities/'+mun.municipalityId,409,{...munBody,slug:'burnaby'},mun.version);
    await call(anon,'GET','/api/'+slug+'/municipalities',404);
    await call(burnaby,'GET','/api/'+mun.slug+'/municipalities',403);
    await call(superAdmin,'POST','/api/admin/municipalities',400,{...munBody,slug:'admin',name:prefix+' reserved'});
    await call(superAdmin,'POST','/api/admin/municipalities',400,{...munBody,slug:slug+'-date',electionDate:'0001-01-01'});
    // A role-name collision must roll back municipality creation atomically.
    await call(superAdmin,'POST','/api/admin/municipalities',409,{...munBody,slug:slug+'-atomic',name:'Super'});
    await call(anon,'GET','/api/'+slug+'-atomic/municipalities',404);
    checks.push('Super Admin municipality POST/GET/PUT, slug normalization/change, duplicate/reserved/date validation, stale ETag handling and atomic role creation passed.');

    let position=await (await call(burnaby,'POST','/api/burnaby/positions',201,{name:prefix,seatsAvailable:2})).json();
    ledger.positions.push({slug:'burnaby',id:position.positionId});saveLedger();assert.equal(position.municipalityId,bySlug.burnaby.municipalityId);
    position=await (await call(burnaby,'PUT','/api/burnaby/positions/'+position.positionId,200,{name:prefix+' office',seatsAvailable:3},position.version)).json();
    await call(burnaby,'POST','/api/burnaby/positions',409,{name:position.name,seatsAvailable:1});
    for(const seats of [0,101])await call(burnaby,'POST','/api/burnaby/positions',400,{name:prefix,seatsAvailable:seats});
    await call(burnaby,'POST','/api/burnaby/positions',400,{name:' ',seatsAvailable:1});
    await call(burnaby,'POST','/api/burnaby/positions',400,{name:'x'.repeat(101),seatsAvailable:1});
    await call(burnaby,'PUT','/api/burnaby/positions/'+sp[0].positionId,404,{name:prefix,seatsAvailable:1},sp[0].version);
    await call(burnaby,'DELETE','/api/burnaby/positions/'+sp[0].positionId,404,undefined,sp[0].version);
    await call(burnaby,'POST','/api/surrey/positions',403,{name:prefix,seatsAvailable:1});
    let superPosition=await (await call(superAdmin,'POST','/api/surrey/positions',201,{name:prefix,seatsAvailable:1})).json();ledger.positions.push({slug:'surrey',id:superPosition.positionId});saveLedger();
    checks.push('Position CRUD permissions, Super Admin access across tenants, scoped foreign IDs, duplicate offices and seat/name validation passed.');

    const candidateBody={positionId:position.positionId,name:prefix+' candidate',profile:'Fictional verification-owned biography.',email:'phase3@example.org',website:'https://example.org'};
    let candidate=await (await call(burnaby,'POST','/api/burnaby/candidates',201,candidateBody)).json();
    ledger.candidates.push({slug:'burnaby',id:candidate.candidateId});saveLedger();dtoSafe(candidate);assert.equal(candidate.municipalityId,bySlug.burnaby.municipalityId);examples.candidate=candidate;
    for(const invalid of [{...candidateBody,positionId:sp[0].positionId},{...candidateBody,email:'invalid'},{...candidateBody,website:'javascript:alert(1)'},{...candidateBody,profile:'x'.repeat(4001)},{...candidateBody,name:' '},{...candidateBody,dateCreated:'2000-01-01'},{...candidateBody,photoFileName:'../secret'},{...candidateBody,demoSeedKey:'claim-seed'},{...candidateBody,municipalityId:bySlug.surrey.municipalityId}])await call(burnaby,'POST','/api/burnaby/candidates',400,invalid);
    await call(burnaby,'PUT','/api/burnaby/candidates/'+sc.items[0].candidateId,404,candidateBody,sc.items[0].version);
    await call(burnaby,'DELETE','/api/burnaby/candidates/'+sc.items[0].candidateId,404,undefined,sc.items[0].version);
    await call(burnaby,'PUT','/api/burnaby/candidates/'+candidate.candidateId,400,{...candidateBody,positionId:sp[0].positionId},candidate.version);
    await call(burnaby,'PUT','/api/burnaby/candidates/'+candidate.candidateId,400,candidateBody);
    previous=candidate.version;
    candidate=await (await call(burnaby,'PUT','/api/burnaby/candidates/'+candidate.candidateId,200,{...candidateBody,profile:'Edited verification-owned biography.'},candidate.version)).json();
    await call(burnaby,'PUT','/api/burnaby/candidates/'+candidate.candidateId,409,candidateBody,previous);
    await call(burnaby,'DELETE','/api/burnaby/positions/'+position.positionId,409,undefined,position.version);
    const twin=await (await call(burnaby,'POST','/api/burnaby/candidates',201,candidateBody)).json();ledger.candidates.push({slug:'burnaby',id:twin.candidateId});saveLedger();
    const query='/api/burnaby/candidates?search='+encodeURIComponent(prefix)+'&positionId='+position.positionId+'&pageSize=1';
    const first=await get(anon,query+'&page=1'),second=await get(anon,query+'&page=2');assert.equal(first.totalCount,2);assert.ok(first.items[0].candidateId<second.items[0].candidateId);
    await call(anon,'GET','/api/burnaby/candidates?pageSize=101',400);await call(anon,'GET','/api/burnaby/candidates?page=0',400);await call(anon,'GET','/api/burnaby/candidates?sort=unsafe',400);
    await call(anon,'GET','/api/burnaby/candidates?positionId='+sp[0].positionId,400);
    const forgedQuery=await get(anon,'/api/burnaby/candidates?municipalityId='+bySlug.surrey.municipalityId);assert.ok(forgedQuery.items.every(x=>x.municipalityId===bySlug.burnaby.municipalityId));
    checks.push('Candidate CRUD, parent ownership, overposting protection, safe DTOs, stale/missing versions, dependent-delete conflict, name search, filters and stable pagination passed.');

    const photo='/api/burnaby/candidates/'+candidate.candidateId+'/photo', validPng=png();
    await call(anon,'GET',photo,404);
    for(const [name,buffer,mime,status] of [['bad.svg',Buffer.from('<svg/>'),'image/svg+xml',400],['fake.png',Buffer.from('<html>not an image</html>'),'image/png',400],['wrong.jpg',validPng,'image/jpeg',400],['../escape.png',validPng,'image/png',400],['big.png',Buffer.alloc(2*1024*1024+1),'image/png',413],['wide.png',png(6001,1),'image/png',400],['empty.png',Buffer.alloc(0),'image/png',400]])await upload(burnaby,photo,candidate.version,name,buffer,mime,status);
    await upload(surrey,photo,candidate.version,'valid.png',validPng,'image/png',403);
    await upload(burnaby,'/api/burnaby/candidates/'+sc.items[0].candidateId+'/photo',sc.items[0].version,'valid.png',validPng,'image/png',404);
    candidate=await (await upload(burnaby,photo,candidate.version,'photo.JPEG',fs.readFileSync('src/MEMS.Api/wwwroot/images/demo-candidates/demo-vivian-kessler.jpg'),'application/octet-stream')).json();
    const image=await call(anon,'GET',candidate.photoUrl,200);assert.equal(image.headers()['content-type'],'image/png');assert.ok((await image.body()).subarray(0,8).equals(Buffer.from('89504e470d0a1a0a','hex')));
    candidate=await (await upload(burnaby,photo,candidate.version,'valid.png',Buffer.concat([validPng,Buffer.from('PRIVATE_TRAILING_MARKER')]))).json();
    const sanitized=await (await call(anon,'GET',candidate.photoUrl,200)).body();assert.ok(!sanitized.includes(Buffer.from('PRIVATE_TRAILING_MARKER')));
    await call(burnaby,'DELETE','/api/surrey/candidates/'+sc.items[0].candidateId+'/photo',403,undefined,sc.items[0].version);
    candidate=await (await call(burnaby,'DELETE',photo,200,undefined,candidate.version)).json();assert.equal(candidate.photoUrl,null);await call(anon,'GET',photo,404);
    const logo='/api/'+mun.slug+'/municipalities/'+mun.municipalityId+'/logo';
    await upload(burnaby,'/api/burnaby/municipalities/'+bySlug.burnaby.municipalityId+'/logo',bySlug.burnaby.version||bc.items[0].version,'valid.png',validPng,'image/png',403);
    mun=await (await upload(superAdmin,logo,mun.version,'logo.png',validPng)).json();await call(anon,'GET',mun.logoUrl,200);
    mun=await (await call(superAdmin,'DELETE',logo,200,undefined,mun.version)).json();assert.equal(mun.logoUrl,null);
    await call(anon,'GET',bc.items[0].photoUrl,200);
    await call(anon,'GET','/api/burnaby/candidates/'+sc.items[0].candidateId+'/photo',404);
    await call(anon,'GET','/images/demo-candidates/demo-vivian-kessler.jpg',404);
    await call(anon,'GET','/.local/identity-seed.json',404);
    checks.push('Media rejects unsupported, spoofed, mismatched, empty, traversal, oversized and excessive-dimension files; JPEG/PNG decoding/re-encoding, tenant checks, replace/delete and seed-photo preservation passed.');

    const docs=await call(anon,'GET','/openapi/v1.json',200);const spec=await docs.json();
    for(const route of ['/api/admin/municipalities','/api/{slug}/positions','/api/{slug}/candidates','/api/auth/login'])assert.ok(spec.paths[route]);
    assert.ok(spec.components.securitySchemes.IdentityCookie);assert.ok(spec.components.securitySchemes.CsrfHeader);
    assert.ok(spec.paths['/api/{slug}/candidates'].post.security.length);await call(anon,'GET','/api/docs',200);
    const cors=await anon.ctx.fetch('/api/burnaby/candidates',{method:'OPTIONS',headers:{Origin:'https://attacker.example','Access-Control-Request-Method':'POST'}});assert.equal(cors.headers()['access-control-allow-origin'],undefined);
    assert.equal(image.headers()['x-content-type-options'],'nosniff');
    checks.push('OpenAPI describes models, cookie/CSRF requirements and routes; readable docs work; external CORS is not enabled; nosniff and HTTPS media serving verified.');

    // Explicitly prove that the assigned municipal administrator can complete DELETE too.
    await call(burnaby,'DELETE','/api/burnaby/candidates/'+twin.candidateId,204,undefined,twin.version);
    await call(burnaby,'DELETE','/api/burnaby/candidates/'+candidate.candidateId,204,undefined,candidate.version);
    await call(burnaby,'DELETE','/api/burnaby/positions/'+position.positionId,204,undefined,position.version);
    await call(anon,'GET','/api/burnaby/candidates/'+candidate.candidateId,404);
    checks.push('The assigned municipal administrator successfully deleted its own candidates and empty position; foreign records remained outside its scope.');

    const temp=await login(fixture.UserName,fixture.Password);await call(temp,'POST','/api/auth/logout',204);await call(temp,'GET','/api/auth/me',401);await csrf(temp);
    for(let i=0;i<5;i++)await call(temp,'POST','/api/auth/login',401,{userName:fixture.UserName,password:'incorrect-verification-password'});
    await call(temp,'POST','/api/auth/login',401,{userName:fixture.UserName,password:fixture.Password});
    const cookies=await superAdmin.ctx.storageState();const authCookie=cookies.cookies.find(x=>x.name==='.MEMS.Assignment2.Identity');assert.ok(authCookie.secure&&authCookie.httpOnly);
    checks.push('Logout ends the cookie session; the owned account locks after five failed passwords; secure HttpOnly Identity cookies are used. No seeded password is reset.');
  }finally{
    if(superAdmin){
      for(const item of [...ledger.candidates].reverse()){const r=await superAdmin.ctx.get('/api/'+item.slug+'/candidates/'+item.id);if(r.status()===200)await call(superAdmin,'DELETE','/api/'+item.slug+'/candidates/'+item.id,204,undefined,(await r.json()).version);}
      for(const item of [...ledger.positions].reverse()){const r=await superAdmin.ctx.get('/api/'+item.slug+'/positions/'+item.id);if(r.status()===200)await call(superAdmin,'DELETE','/api/'+item.slug+'/positions/'+item.id,204,undefined,(await r.json()).version);}
      for(const id of [...ledger.municipalities].reverse()){const r=await superAdmin.ctx.get('/api/admin/municipalities/'+id);if(r.status()===200)await call(superAdmin,'DELETE','/api/admin/municipalities/'+id,204,undefined,(await r.json()).version);}
    }
    for(const ctx of contexts)await ctx.dispose();
  }
  fs.mkdirSync('artifacts/phase3',{recursive:true});fs.writeFileSync('artifacts/phase3/api-verification.json',JSON.stringify({passed:true,checks},null,2));
  fs.writeFileSync('artifacts/phase3/examples.json',JSON.stringify(examples,null,2));
  console.log(JSON.stringify({passed:true,checks},null,2));
})().catch(e=>{fs.writeFileSync('.local/phase3-api-error.txt',e.message);console.error(e.message.slice(0,1200));process.exitCode=1;});

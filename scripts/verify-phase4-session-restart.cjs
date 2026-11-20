// Legitimate API sign-in/logout; private revoked-cookie replay checks only, no database writes.
const {request}=require('../.tools/browser/node_modules/@playwright/test');
const fs=require('node:fs'),assert=require('node:assert/strict');
(async()=>{
  const prepare=process.argv[2]==='prepare';
  const state='.local/phase4-revoked-session.json';
  const cookie=prepare?undefined:JSON.parse(fs.readFileSync(state,'utf8')).cookie;
  const c=await request.newContext({baseURL:'https://localhost:7192',ignoreHTTPSErrors:true,...(cookie?{extraHTTPHeaders:{Cookie:cookie}}:{})});
  try{
    if(prepare){
      let token=(await(await c.get('/api/auth/csrf')).json()).requestToken;
      const password=JSON.parse(fs.readFileSync('.local/identity-seed.json','utf8')).Parameters['mems-identity-seed-password'];
      assert.equal((await c.post('/api/auth/login',{headers:{'X-CSRF-TOKEN':token},data:{userName:'super',password}})).status(),200);
      token=(await(await c.get('/api/auth/csrf')).json()).requestToken;
      const cookies=(await c.storageState()).cookies.filter(x=>x.name.startsWith('.MEMS.Assignment2.Identity')).map(x=>x.name+'='+x.value).join('; ');
      assert.equal((await c.post('/api/auth/logout',{headers:{'X-CSRF-TOKEN':token}})).status(),204);
      fs.writeFileSync(state,JSON.stringify({cookie:cookies}));
      console.log('Prepared a legitimately revoked session; private replay state stays in ignored local storage.');
    }else{
      assert.equal((await c.get('/api/auth/me')).status(),401);
      fs.writeFileSync('artifacts/phase4/session-restart.json',JSON.stringify({passed:true,loggedOutCookieStillRejectedAfterAspireRestart:true},null,2));
      console.log('PASS: logged-out cookie remains rejected after Aspire restart; session revocation persisted.');
    }
  }finally{await c.dispose();}
})().catch(e=>{console.error(e.message.slice(0,400));process.exitCode=1});

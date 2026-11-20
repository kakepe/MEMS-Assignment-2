const {chromium}=require('../.tools/browser/node_modules/@playwright/test');
const {pathToFileURL}=require('node:url');const path=require('node:path'),fs=require('node:fs'),assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1400,height:1000}}),external=[];
  page.on('request',r=>{if(!/^(file:|data:)/.test(r.url()))external.push(r.url());});
  await page.goto(pathToFileURL(path.resolve('docs/final/report.html')).href);await page.evaluate(()=>document.fonts.ready);
  assert.ok(await page.locator('img').evaluateAll(xs=>xs.length>=13&&xs.every(x=>x.complete&&x.naturalWidth>0)),'Every diagram/screenshot must load');
  assert.deepEqual(external,[],'Report must render offline');
  await page.pdf({path:'docs/final/MEMS-Assignment-2-Final-Report.pdf',preferCSSPageSize:true,printBackground:true,displayHeaderFooter:true,headerTemplate:'<span></span>',footerTemplate:'<div style="font:8px Arial;color:#536775;width:100%;text-align:center">MEMS · COMP4976 · Abdullah Rehan / Alex Fisher · <span class="pageNumber"></span> / <span class="totalPages"></span></div>'});
  for(const [name,file] of [['architecture','architecture.svg'],['domain-erd','erd-domain.svg'],['identity-erd','erd-identity.svg']]){
    await page.setViewportSize({width:1300,height:950});
    await page.setContent('<!doctype html><html><body style="margin:0;background:white"><img style="display:block;width:1250px" src="'+pathToFileURL(path.resolve('docs/final/diagrams/'+file)).href+'"></body></html>');
    await page.locator('img').evaluate(e=>e.decode());await page.locator('img').screenshot({path:'artifacts/phase7/'+name+'.png',animations:'disabled'});
  }
  fs.writeFileSync('artifacts/phase7/report-render.json',JSON.stringify({passed:true,allImagesLoaded:true,externalRequests:0,pdfCreated:true},null,2));console.log('Final PDF rendered offline with all diagrams/screenshots.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e.message.slice(0,1000));process.exitCode=1;});

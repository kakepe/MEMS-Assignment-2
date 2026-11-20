// Opens native Chrome preview and cancels it. Never clicks Print or writes to a physical printer.
const {chromium,expect}=require('../.tools/browser/node_modules/@playwright/test');
const fs=require('node:fs');
(async()=>{
 const context=await chromium.launchPersistentContext('.local/phase6-preview-profile',{executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:false,ignoreHTTPSErrors:true,viewport:{width:1440,height:1000}});
 const browser=context.browser();
 const targets=[];
 try{
   const page=await context.newPage();await page.goto('https://localhost:7290/burnaby/candidates');
   await page.locator('.guide-selection-button').first().click();await page.getByRole('link',{name:'My voting guide',exact:true}).click();
   await expect(page.getByRole('button',{name:'Print voting guide',exact:true})).toBeEnabled();
   const clicking=page.getByRole('button',{name:'Print voting guide',exact:true}).click().catch(()=>{});
   const cdp=await browser.newBrowserCDPSession();
   let preview;
   for(let i=0;i<30;i++){
     const t=(await cdp.send('Target.getTargets')).targetInfos;
     targets.splice(0,targets.length,...t.map(x=>({type:x.type,url:x.url})));
     preview=t.find(p=>p.url==='chrome://print/');
     if(preview)break;
     await new Promise(r=>setTimeout(r,300));
   }
   fs.writeFileSync('artifacts/phase6/preview-targets.json',JSON.stringify(targets,null,2));
   if(!preview)throw Error('Native print preview target could not be inspected.');
   // Native preview belongs to Chrome's default context, outside Playwright's incognito page list.
   const {sessionId}=await cdp.send('Target.attachToTarget',{targetId:preview.targetId,flatten:false});
   let next=0;
   const command=(method,params={})=>new Promise((resolve,reject)=>{
     const id=++next,timeout=setTimeout(()=>{cdp.off('Target.receivedMessageFromTarget',onMessage);reject(Error('Preview command timed out'));},15000);
     function onMessage(e){if(e.sessionId!==sessionId)return;const message=JSON.parse(e.message);if(message.id!==id)return;clearTimeout(timeout);cdp.off('Target.receivedMessageFromTarget',onMessage);message.error?reject(Error(message.error.message)):resolve(message.result);}
     cdp.on('Target.receivedMessageFromTarget',onMessage);
     cdp.send('Target.sendMessageToTarget',{sessionId,message:JSON.stringify({id,method,params})}).catch(reject);
   });
   const evaluate=async expression=>(await command('Runtime.evaluate',{expression,returnByValue:true})).result.value;
   const walk="function all(root=document){return [...root.querySelectorAll('*')].flatMap(e=>[e,...(e.shadowRoot?all(e.shadowRoot):[])]);}";
   await new Promise(r=>setTimeout(r,2200));
   await evaluate(`(()=>{${walk}const more=all().find(e=>e.tagName==='CR-EXPAND-BUTTON');if(more&&!more.expanded)more.click();return true;})()`);
   await new Promise(r=>setTimeout(r,600));
   let papers=[];
   for(let i=0;i<25;i++){
     papers=await evaluate(`(()=>{${walk}const s=all().find(e=>e.tagName==='SELECT'&&[...e.options].some(o=>/A4|Letter/i.test(o.textContent)));return s?[...s.options].map(o=>o.textContent.trim()):[];})()`);
     if(papers.length)break;
     await new Promise(r=>setTimeout(r,500));
   }
   fs.writeFileSync('.local/phase6-preview-papers.json',JSON.stringify(papers));
   fs.writeFileSync('.local/phase6-preview-controls.json',JSON.stringify(await evaluate(`(()=>{${walk}return all().filter(e=>e.tagName.startsWith('PRINT-')||e.tagName==='SELECT'||e.tagName==='CR-BUTTON').map(e=>({tag:e.tagName,id:e.id,text:e.textContent.trim().slice(0,150)}));})()`),null,2));
   const initial=await command('Page.captureScreenshot',{format:'png',captureBeyondViewport:false});fs.writeFileSync('artifacts/phase6/native-print-preview.png',Buffer.from(initial.data,'base64'));
   const verified=[];
   for(const paper of ['Letter','A4']){
     const selected=await evaluate(`(()=>{${walk}const s=all().find(e=>e.tagName==='SELECT'&&[...e.options].some(o=>/A4|Letter/i.test(o.textContent)));const o=s&&[...s.options].find(o=>o.textContent.trim().includes(${JSON.stringify(paper)}));if(!o)return false;s.value=o.value;s.dispatchEvent(new Event('change',{bubbles:true}));return true;})()`);
     if(!selected)throw Error('Native preview paper option unavailable: '+paper);
     await new Promise(r=>setTimeout(r,1300));
     const capture=await command('Page.captureScreenshot',{format:'png',captureBeyondViewport:false});fs.writeFileSync('artifacts/phase6/native-print-preview-'+paper+'.png',Buffer.from(capture.data,'base64'));verified.push(paper);
   }
   const result={passed:true,nativePreviewOpenedByGuidePrintButton:true,paperOptionsVerified:verified,physicalPrintingAttempted:false};
   fs.writeFileSync('artifacts/phase6/native-print-preview.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result));
   await evaluate(`(()=>{${walk}all().find(e=>e.tagName==='CR-BUTTON'&&e.textContent.trim()==='Cancel')?.click();return true;})()`);await clicking;
 }finally{await browser.close();}
})().catch(e=>{console.error(e.message.slice(0,1000));process.exitCode=1;});

// Local preview harness for our own review fragment, not app/browser automation.
const fs=require('node:fs');
const path=require('node:path');
const {chromium}=require('C:/Users/huxel/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
(async()=>{
  const browser=await chromium.launch({headless:true,channel:'msedge'});
  const page=await browser.newPage({viewport:{width:736,height:900}});
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  const fragment=fs.readFileSync('C:/Users/huxel/.codex/visualizations/2026/09/30/01a0f224-0b97-7b20-80a9-aa020b217db1/barn-exterior-review.html','utf8');
  if(fragment.includes('<html')||fragment.includes('<!DOCTYPE')||Buffer.byteLength(fragment)>1e6)throw Error('Invalid fragment');
  // Test-only approximation of host utility styles and state API.
  await page.setContent(`<style>:root{--foreground:#273431;--background:#fff;--muted:#ebefed;--muted-foreground:#64716a}body{margin:0;font:14px Arial}h3{font-size:16px;font-weight:500}.viz-controls{display:flex;flex-wrap:wrap;gap:12px}.form-label{display:flex;flex-direction:column;gap:4px}select{font:inherit;padding:6px}.text-small{font-size:12px}</style><script>window.openai={widgetState:null,setWidgetState:async s=>window.savedState=s}</script>`+fragment);
  const cases=[];
  for(const width of [736,320]){
    await page.setViewportSize({width,height:900});
    for(const level of [1,3,5]){
      await page.locator('#barn-level').selectOption(String(level));
      for(const view of ['front','right','rear','left','top']){
        await page.locator('#barn-view').selectOption(view);
        await page.waitForTimeout(50);
        const check=await page.evaluate(()=>({overflow:document.documentElement.scrollWidth>innerWidth,
          summary:document.querySelector('#barn-detail').textContent,
          saved:window.savedState,rect:document.querySelector('svg').getBoundingClientRect().toJSON(),
          clippedText:[...document.querySelectorAll('svg text')].filter(t=>{const r=t.getBoundingClientRect(),s=t.ownerSVGElement.getBoundingClientRect();return r.left<s.left-.5||r.right>s.right+.5||r.top<s.top-.5||r.bottom>s.bottom+.5}).map(t=>t.textContent)}));
        if(check.overflow||check.clippedText.length||check.saved.modelContent.level!==level||check.saved.modelContent.view!==view)throw Error(JSON.stringify({width,level,view,...check}));
        cases.push({width,level,view,summary:check.summary});
      }
    }
    await page.locator('#barn-view').selectOption('front');
    await page.screenshot({path:path.join(__dirname,`interactive-qa-${width}.png`)});
  }
  await page.evaluate(()=>window.dispatchEvent(new CustomEvent('openai:set_globals',{detail:{globals:{widgetState:{modelContent:{proposal:'barn-exterior-A',level:2,view:'left'}}}}})));
  if(await page.locator('#barn-level').inputValue()!=='2'||await page.locator('#barn-view').inputValue()!=='left')throw Error('State restore failed');
  if(errors.length)throw Error(errors.join('\n'));
  fs.writeFileSync(path.join(__dirname,'interactive-validation.json'),JSON.stringify({cases:cases.length,widths:[736,320],levels:[1,3,5],views:5,scriptErrors:errors,stateRestore:'PASS',textBounds:'PASS',horizontalOverflow:false},null,2)+'\n');
  console.log(`PASS: ${cases.length} level/view/width cases; no script errors, text clipping or horizontal overflow; state restore passed.`);
  await browser.close();
})().catch(e=>{console.error(e);process.exitCode=1;});

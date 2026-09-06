const { chromium } = require('playwright');
(async () => { const b = await chromium.launch({ executablePath: '/opt/pw-browsers/chromium' });
  for (const s of ['DemoScene_Forest','DemoScene_Autumn','DemoScene_Desert','DemoScene_DeepForest']) {
    const p = await b.newPage({ viewport: { width: 1600, height: 900 } });
    await p.goto('file://' + process.argv[2] + '/' + s + '.html'); await p.waitForTimeout(500);
    await p.screenshot({ path: process.argv[2] + '/' + s + '.png' }); await p.close(); }
  await b.close(); })();

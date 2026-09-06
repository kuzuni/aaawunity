// render_our_map.py 가 만든 our_*.html 을 PNG 로 (Playwright · PNG 는 커밋 금지 · T19 비교용)
const { chromium } = require('playwright'); const fs = require('fs'); const path = require('path');
(async () => { const b = await chromium.launch({ executablePath: '/opt/pw-browsers/chromium' });
  const dir = process.argv[2];
  for (const f of fs.readdirSync(dir).filter(f => f.startsWith('our_') && f.endsWith('.html'))) {
    const p = await b.newPage({ viewport: { width: 540, height: 1140 } });
    await p.goto('file://' + path.resolve(dir, f)); await p.waitForTimeout(300);
    await p.screenshot({ path: path.resolve(dir, f.replace('.html', '.png')) }); await p.close(); }
  await b.close(); })();

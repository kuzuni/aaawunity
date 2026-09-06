// WebGL 배포 스모크 판정기 (T59·T60 · 주인 상시 지시 2026-09-06 «배포·push 전 에러 확인 · 게임 들어가 확인»). 셸 래퍼 = tools/webgl_smoke.sh
// 사용: node tools/webgl_smoke.js <URL> [--battle] [--require-marker] [--strict-audio] [--no-fps] [--timeout SEC] [--shot out.png] [--log out.txt]
// 판정(종료 코드 0 = 초록):
//   ⓐ pageerror · console.error 0 — 유니티 로더의 «Invoking error handler»(RangeError · 예외) · 빨간 Debug.LogError 전부.
//      단 오디오 매체 에러(NotSupportedError «no supported source» · EncodingError «Unable to decode audio data» · «Loading FSB failed»)는
//      **T64(WebGL 오디오)** 이 닫힐 때까지 ⚠ 경고로만 센다(--strict-audio 면 에러) — 결정 110(PROGRESS).
//   ⓑ 로딩 완료 = #unity-loading-bar 가 사라짐(index.html 템플릿의 then) 또는 window.unityInstance.
//   ⓒ «[KkomaKnight] ready lobby»(App.BuildUi) — --require-marker 면 필수, 아니면 없을 때 ⚠(마커 이전 빌드).
//   ⓓ --battle: SendMessage("App","DebugGo","battle") 뒤 «ready battle» + 10초 동안 에러 0.
//   ⓔ 로딩 뒤 10초 FPS 한 줄(T72 4항 «질감 트윈이 프레임을 갉지 않는가» · 판정에는 안 쓴다 · --no-fps 로 끔).
// playwright 는 전역(npm i -g playwright@1.56.1 · npx playwright install --with-deps chromium) — 없으면 exit 3.
'use strict';
const fs = require('fs');

const args = process.argv.slice(2);
const url = args.find(a => !a.startsWith('--') && !/^\d+$/.test(a) && !a.endsWith('.png') && !a.endsWith('.txt'));
const flag = n => args.includes('--' + n);
const opt = (n, d) => { const i = args.indexOf('--' + n); return i >= 0 && args[i + 1] ? args[i + 1] : d; };
if (!url) { console.error('usage: node tools/webgl_smoke.js <URL> [--battle] [--require-marker] [--strict-audio] [--no-fps] [--timeout SEC] [--shot out.png] [--log out.txt]'); process.exit(2); }
const timeoutSec = parseInt(opt('timeout', '180'), 10);
const wantBattle = flag('battle'), requireMarker = flag('require-marker'), strictAudio = flag('strict-audio');
const shotPath = opt('shot', ''), logPath = opt('log', '');
const AUDIO_RE = /no supported source was found|Unable to decode audio data|Loading FSB failed|playSoundClip error/;

let chromium;
try { ({ chromium } = require('playwright')); }
catch (e) {
  try { ({ chromium } = require(require('child_process').execSync('npm root -g').toString().trim() + '/playwright')); }
  catch (e2) { console.error('[smoke] playwright 없음 — `npm i -g playwright@1.56.1 && npx playwright install --with-deps chromium` 또는 CI 결과로 대신: ' + e2.message); process.exit(3); }
}

const lines = [];
const log = (tag, msg) => { const l = `[${new Date().toISOString().substr(11, 12)}] ${tag} ${String(msg).replace(/\n+$/, '')}`; lines.push(l); if (tag !== 'log') console.log(l.slice(0, 600)); };

(async () => {
  const browser = await chromium.launch({
    headless: true,
    args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--no-sandbox', '--disable-dev-shm-usage', '--autoplay-policy=no-user-gesture-required'],
  });
  const page = await browser.newPage({ viewport: { width: 540, height: 1170 } });
  const errors = [], audioWarn = [];
  let readyLobby = false, readyBattle = false, loaded = false;
  const noteError = (text) => { if (!strictAudio && AUDIO_RE.test(text)) { audioWarn.push(text); log('AUDIO⚠', text); } else { errors.push(text); log('ERROR', text); } };
  page.on('pageerror', e => noteError('pageerror: ' + (e.stack || e.message)));
  page.on('console', m => {
    const t = m.type(), text = m.text();
    if (t === 'error') noteError('console.error: ' + text);
    else if (t === 'warning') log('warn', text.slice(0, 300));
    else log('log', text.slice(0, 300));
    if (text.includes('[KkomaKnight] ready lobby')) readyLobby = true;
    if (text.includes('[KkomaKnight] ready battle')) readyBattle = true;
    if (text.includes('Invoking error handler due to') && !AUDIO_RE.test(text)) noteError('unity error handler: ' + text.slice(0, 500));
  });
  page.on('requestfailed', r => log('reqfail', r.url() + ' ' + (r.failure() || {}).errorText));
  page.on('response', r => { if (r.status() >= 400) noteError(`http ${r.status()} ${r.url()}`); });

  // 유니티 기본 템플릿은 unityInstance 를 then 의 지역 변수로만 둔다(로더의 함수 선언은 window setter 도 우회) → 문서 응답을 가로채
  // `createUnityInstance(canvas, config,` 호출부만 감싸 인스턴스를 window.unityInstance 에 둔다(레포의 템플릿은 손대지 않는다 · 패턴이 없으면 그대로 두고 경고).
  await page.route('**/*', async route => {
    if (route.request().resourceType() !== 'document') return route.fallback();
    const res = await route.fetch();
    let body = await res.text();
    const pat = /\bcreateUnityInstance\s*\(\s*canvas\s*,\s*config\s*,/;
    if (pat.test(body)) {
      body = body.replace(pat, 'window.__kkSmokeCUI(canvas, config,')
        .replace(/<head[^>]*>/i, m => m + '<script>window.__kkSmokeCUI=(...a)=>createUnityInstance(...a).then(i=>{window.unityInstance=i;return i;});</script>');
    } else log('warn', 'index.html 에 createUnityInstance(canvas, config, 패턴이 없어 SendMessage 훅을 못 건다');
    await route.fulfill({ response: res, body, headers: { ...res.headers(), 'content-length': String(Buffer.byteLength(body)) } });
  });
  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60000 });
  const isLoaded = () => page.evaluate(() => { const b = document.querySelector('#unity-loading-bar'); return !!window.unityInstance || (b ? b.style.display === 'none' : false); }).catch(() => false);
  const deadline = Date.now() + timeoutSec * 1000;
  while (Date.now() < deadline) {
    loaded = await isLoaded();
    if (loaded && readyLobby) break;
    if (loaded && !requireMarker) { await page.waitForTimeout(5000); break; }   // 마커 없는 빌드(구 빌드) · 늦게 오는 마커 5초만 더
    if (errors.some(e => /Maximum call stack|RangeError|abort\(|Uncaught|unity error handler/.test(e))) break;
    await page.waitForTimeout(500);
  }
  log('state', `loaded=${loaded} readyLobby=${readyLobby} errors=${errors.length} audioWarn=${audioWarn.length}`);

  // T72 4항 — 질감 트윈(패턴 uvRect 흐름 · 아이콘 뒤 빛살 회전)이 프레임을 갉지 않는지 «배포된 화면에서 10초» 재서 한 줄 남긴다.
  // 판정에는 안 쓴다(headless SwiftShader 는 폰 GPU 가 아니다 · 회차 사이 비교용 수치) — --no-fps 로 끌 수 있다.
  if (loaded && !flag('no-fps')) {
    const fps = await page.evaluate(() => new Promise(res => {
      const t0 = performance.now(); let n = 0, prev = t0, worst = Infinity;
      const step = t => {
        n++; const dt = t - prev; prev = t;
        if (n > 1 && dt > 0) worst = Math.min(worst, 1000 / dt);
        if (t - t0 < 10000) requestAnimationFrame(step); else res({ avg: n / ((t - t0) / 1000), min: worst === Infinity ? 0 : worst });
      };
      requestAnimationFrame(step);
    })).catch(e => { log('fps-fail', e.message); return null; });
    if (fps) log('fps', `로비 10초 · 평균 ${fps.avg.toFixed(1)} fps · 최저 ${fps.min.toFixed(1)} fps (T72 질감 트윈 · headless SwiftShader 기준)`);
  }
  if (loaded && wantBattle) {
    // 템플릿은 unityInstance 를 지역 변수로만 두므로 SendMessage 는 Module(unityFramework) 경유가 안 되면 실패 — App 이 없으면 에러 1건
    const sent = await page.evaluate(() => {
      const inst = window.unityInstance; if (inst && inst.SendMessage) { inst.SendMessage('App', 'DebugGo', 'battle'); return 'unityInstance'; }
      // 템플릿 호환: createUnityInstance 의 then 이 window 에 안 놓았을 때 Module 로
      const M = window.Module || (window.unityFramework && window.unityFramework.Module); if (M && M.SendMessage) { M.SendMessage('App', 'DebugGo', 'battle'); return 'Module'; }
      return null;
    }).catch(e => { noteError('SendMessage 실패: ' + e.message); return null; });
    if (!sent) noteError('SendMessage 경로 없음(window.unityInstance / Module) — index.html 템플릿이 unityInstance 를 window 에 두어야 한다');
    else log('send', 'DebugGo battle via ' + sent);
    const d2 = Date.now() + 20000;
    while (Date.now() < d2 && !readyBattle) await page.waitForTimeout(250);
    await page.waitForTimeout(10000);   // 전투 10초 동안 에러 0
    log('state', `readyBattle=${readyBattle} errors=${errors.length} audioWarn=${audioWarn.length}`);
  }
  if (shotPath) { try { fs.mkdirSync(require('path').dirname(shotPath), { recursive: true }); await page.screenshot({ path: shotPath }); log('shot', shotPath); } catch (e) { log('shot-fail', e.message); } }
  await browser.close();
  if (logPath) { try { fs.mkdirSync(require('path').dirname(logPath), { recursive: true }); fs.writeFileSync(logPath, lines.join('\n') + '\n'); } catch (e) { console.log('[smoke] 로그 저장 실패 ' + e.message); } }

  const markerOk = readyLobby || !requireMarker;
  const ok = errors.length === 0 && loaded && markerOk && (!wantBattle || readyBattle);
  console.log(ok ? `[smoke] ✅ 초록: 콘솔 에러 0 · 로딩 완료 · ${readyLobby ? '로비 도달' : '로비 마커 없음(구 빌드 · ⚠)'}${wantBattle ? ' · 전투 진입' : ''}${audioWarn.length ? ` · 오디오 경고 ${audioWarn.length}(T64)` : ''}`
                 : `[smoke] ❌ 빨강: errors=${errors.length} loaded=${loaded} readyLobby=${readyLobby}${wantBattle ? ` readyBattle=${readyBattle}` : ''} audioWarn=${audioWarn.length}`);
  for (const e of errors.slice(0, 20)) console.log('   - ' + e.split('\n').slice(0, 8).join('\n     '));
  process.exit(ok ? 0 : 1);
})().catch(e => { console.error('[smoke] 실행 실패: ' + (e.stack || e.message)); process.exit(4); });

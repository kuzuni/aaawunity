// WebGL 배포 스모크 판정기 (T59·T60 · 주인 상시 지시 2026-09-06 «배포·push 전 에러 확인 · 게임 들어가 확인»). 셸 래퍼 = tools/webgl_smoke.sh
// 사용: node tools/webgl_smoke.js <URL> [--battle] [--require-marker] [--strict-audio] [--strict-net] [--no-fps] [--timeout SEC] [--shot out.png] [--log out.txt]
// 판정(종료 코드 0 = 초록):
//   ⓐ pageerror · console.error 0 — 유니티 로더의 «Invoking error handler»(RangeError · 예외) · 빨간 Debug.LogError 전부.
//      단 **오디오 문구는 빨강 사유가 아니다 — 주인 종결 지시(2026-09-07 04:3X «webgl 오디오 잘 들리는데 뭐 자꾸 안 된다 카냐»)**.
//      «no supported source» · «The element has no supported sources»(T134) · «Unable to decode audio data» ·
//      «Loading FSB failed» · «playSoundClip error» ·
//      «Streaming of 'ogg' on this platform is not supported» 는 `AUDIO⚠` 로 로그에만 남기고 종료 코드에 안 넣는다
//      (--strict-audio 를 **명시**해야 에러 · 결정 110 → 219). 판정 정본은 주인 실기다 — 주인 폰·데스크톱 Chrome 에서는 소리가 난다.
//      headless 가 유독 못 읽는 까닭도 실측됐다(결정 218): playwright 의 chromium 은 독점 코덱이 없어
//      `canPlayType('audio/mp4; codecs="mp4a.40.2"')` · `audio/aac` 에 «no» 를 준다(ogg·mpeg 는 «probably»).
//      유니티 WebGL 은 MP3·WAV 가 아닌 클립을 전부 audio/mp4 로 넘기므로(framework.js `jsAudioGetMimeTypeFromType`)
//      이 브라우저에서만 «못 읽는다» 가 난다 — 게임의 결함이 아니라 실행 환경이다(T83 의 «망» 과 같은 갈래).
//      또 **게임 밖 호스트로 나가는 요청이 «망» 때문에 막힌 것**(프록시·DNS·오프라인 · net::ERR_TUNNEL_CONNECTION_FAILED 류)은
//      게임 결함이 아니라 실행 환경이라 ⚠ 경고로만 센다(--strict-net 이면 에러) — T83 · 주인 IAP/Unity Services 커밋 뒤
//      WebGL 이 Unity Analytics(config.uca.cloud.unity3d.com · cdp.cloud.unity3d.com)로 나가는데 워커 컨테이너의 프록시가 그걸 막는다.
//      **같은 출처(빌드가 서비스되는 origin)의 파일이 못 읽히면 그건 그대로 에러다** — 게임 파일 누락을 놓치지 않는다.
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
if (!url && !flag('self-test')) { console.error('usage: node tools/webgl_smoke.js <URL> [--battle] [--require-marker] [--strict-audio] [--strict-net] [--no-fps] [--timeout SEC] [--shot out.png] [--log out.txt] | --self-test'); process.exit(2); }
const timeoutSec = parseInt(opt('timeout', '180'), 10);
const wantBattle = flag('battle'), requireMarker = flag('require-marker'), strictAudio = flag('strict-audio'), strictNet = flag('strict-net');
const shotPath = opt('shot', ''), logPath = opt('log', '');
// 오디오 문구(주인 종결 지시 2026-09-07 · 결정 219) — 「Streaming of 'ogg' … not supported」 는 유니티 WebGL 네이티브가
// UnityWebRequestMultimedia 의 ogg 스트리밍을 거부하며 찍는 줄이라 위 넷과 같은 갈래로 본다.
// 「The element has no supported sources.」(T134) — 유니티 WebGL 은 131072바이트가 넘는 클립을 `<audio>` 요소로 넘기는데
// (`_JS_Sound_Load` · `check_audio_webgl.py` 의 실측 절), 헤드리스 chromium 은 AAC/MP4 코덱이 없어 그 요소가
// `NotSupportedError` 를 던진다. 「no supported source was found」 와 **같은 일의 다른 문구**다(작은 클립은 decodeAudioData
// 로 가서 앞 문구, 큰 클립은 `<audio>` 로 가서 이 문구) — 그래서 같은 갈래로 본다. 이 문구는 미디어 요소만 낼 수 있어
// 다른 «자원 없음» 을 덮지 않는다. 오디오가 진짜 나는지는 헤드리스가 판정 못 하고 주인 실기가 정본이다(결정 300).
const AUDIO_RE = /no supported source was found|The element has no supported sources|Unable to decode audio data|Loading FSB failed|playSoundClip error|Streaming of '[^']*' on this platform is not supported/;
// 브라우저 없이 도는 자가 점검(T134) — 「이 문구가 오디오인가」 를 실제 CI 로그에서 오려 온 줄로 못 박는다.
// 배포 스모크는 25분짜리 WebGL 빌드 뒤에야 도는 자라 분류가 틀린 것을 «빌드 한 판» 쓰고 나서야 안다
// (T134 가 그랬다: #252·#255 두 런이 통째로 버려졌다). 셸 래퍼가 브라우저를 켜기 전에 이걸 먼저 돌린다.
if (flag('self-test')) {
  const cases = [
    // [문구, 오디오로 봐야 하는가] — 참인 것들은 CI 로그 원문에서 그대로 옮겼다
    ['pageerror: The element has no supported sources.', true],                    // #252·#255 가 빨개진 줄
    ['unity error handler: Invoking error handler due to\nNotSupportedError: The element has no supported sources.', true],
    ['pageerror: Failed to load because no supported source was found.', true],
    ['console.error: Loading FSB failed for audio clip "hit".', true],
    ['pageerror: Unable to decode audio data', true],
    // 오디오가 아닌 것을 삼키면 «게임 파일 누락» 을 놓친다 — 반대쪽도 못 박는다
    ['console.error: Failed to load resource: the server responded with a status of 404', false],
    ['pageerror: RangeError: Maximum call stack size exceeded', false],
    ['console.error: 화면 없음: lobby', false],
  ];
  let bad = 0;
  for (const [text, want] of cases) {
    const got = AUDIO_RE.test(text);
    if (got !== want) { bad++; console.error(`  ✗ «${text.split('\n')[0]}» → 오디오=${got} (기대 ${want})`); }
  }
  console.log(bad ? `[smoke] ❌ 자가 점검 ${bad}/${cases.length} 어긋남` : `[smoke] ✅ 자가 점검 ${cases.length}/${cases.length} — 오디오 문구 분류 그대로`);
  process.exit(bad ? 1 : 0);
}
// 망 때문에 못 간 요청(프록시·DNS·오프라인) — 서버가 준 4xx/5xx 는 여기 없다(그건 아래 response 훅이 에러로 센다)
const NET_RE = /Failed to load resource: net::(ERR_TUNNEL_CONNECTION_FAILED|ERR_PROXY_CONNECTION_FAILED|ERR_NAME_NOT_RESOLVED|ERR_INTERNET_DISCONNECTED|ERR_CONNECTION_(REFUSED|TIMED_OUT|RESET|CLOSED)|ERR_ADDRESS_UNREACHABLE|ERR_CERT_[A-Z_]+)/;
// 무엇을 쟀는지 한 낱말로(perf 줄용 · T129) — 로컬 서버면 셸이 넣어 준 SMOKE_TARGET, 아니면 URL 의 호스트
const mode = () => process.env.SMOKE_TARGET || (() => { try { return new URL(url).hostname; } catch (e) { return 'unknown'; } })();
const pageOrigin = (() => { try { return new URL(url).origin; } catch (e) { return null; } })();
const isOffOrigin = (u) => { if (!u) return false; try { return new URL(u).origin !== pageOrigin; } catch (e) { return false; } };

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
  const errors = [], audioWarn = [], netWarn = [];
  let readyLobby = false, readyBattle = false, loaded = false, tweens = null;
  // where = 그 console 메시지가 가리키는 자원 URL(«Failed to load resource» 는 막힌 그 파일을 가리킨다)
  const noteError = (text, where) => {
    if (!strictAudio && AUDIO_RE.test(text)) { audioWarn.push(text); log('AUDIO⚠', text); return; }
    if (!strictNet && NET_RE.test(text) && isOffOrigin(where)) { netWarn.push(where + ' · ' + text); log('NET⚠', where + ' · ' + text); return; }
    errors.push(text); log('ERROR', text);
  };
  page.on('pageerror', e => noteError('pageerror: ' + (e.stack || e.message)));
  page.on('console', m => {
    const t = m.type(), text = m.text();
    if (t === 'error') noteError('console.error: ' + text, (m.location() || {}).url);
    else if (t === 'warning') log('warn', text.slice(0, 300));
    else log('log', text.slice(0, 300));
    if (text.includes('[KkomaKnight] ready lobby')) readyLobby = true;
    if (text.includes('[KkomaKnight] ready battle')) readyBattle = true;
    { const p = text.match(/\[KkomaKnight\] perf tweens=(\d+)/); if (p) tweens = Number(p[1]); }   // T129 ⓑ — DebugGo perf 의 답(Assets/Scripts/Game/App.cs)
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
  log('state', `loaded=${loaded} readyLobby=${readyLobby} errors=${errors.length} audioWarn=${audioWarn.length} netWarn=${netWarn.length}`);

  // T72 4항 — 질감 트윈(패턴 uvRect 흐름 · 아이콘 뒤 빛살 회전)이 프레임을 갉지 않는지 «배포된 화면에서 10초» 재서 한 줄 남긴다.
  // 판정에는 안 쓴다(headless SwiftShader 는 폰 GPU 가 아니다 · 회차 사이 비교용 수치) — --no-fps 로 끌 수 있다.
  if (loaded && !flag('no-fps')) {
    // T129 ⓑ — «왜 느려지나» 를 세려면 «몇 개가 도나» 부터다. fps 를 재기 직전에 한 번 묻는다(화면은 안 바뀐다).
    // 이 case 가 없는 옛 빌드는 «모르는 목적지» 를 로그 한 줄로 남기고 끝난다(에러 0) → tweens 는 null 로 남고 perf 줄에서 «?» 가 된다.
    await page.evaluate(() => {
      const inst = window.unityInstance; if (inst && inst.SendMessage) return inst.SendMessage('App', 'DebugGo', 'perf');
      const M = window.Module || (window.unityFramework && window.unityFramework.Module); if (M && M.SendMessage) M.SendMessage('App', 'DebugGo', 'perf');
    }).catch(e => log('perf-fail', e.message));
    await page.waitForTimeout(500);   // 콘솔 답이 넘어올 틈
    const fps = await page.evaluate(() => new Promise(res => {
      const t0 = performance.now(); let n = 0, prev = t0, worst = Infinity;
      const step = t => {
        n++; const dt = t - prev; prev = t;
        if (n > 1 && dt > 0) worst = Math.min(worst, 1000 / dt);
        if (t - t0 < 10000) requestAnimationFrame(step); else res({ avg: n / ((t - t0) / 1000), min: worst === Infinity ? 0 : worst });
      };
      requestAnimationFrame(step);
    })).catch(e => { log('fps-fail', e.message); return null; });
    if (fps) {
      log('fps', `로비 10초 · 평균 ${fps.avg.toFixed(1)} fps · 최저 ${fps.min.toFixed(1)} fps (T72 질감 트윈 · headless SwiftShader 기준)`);
      // T129 ⓐ — «추세를 보려면 회차마다 로그를 눈으로 읽어야» 했다. 한 줄을 기계가 읽을 꼴로 같이 남긴다:
      // 지난 CI 로그·아티팩트에서 `grep -o 'perf fps=.*'` 한 번이면 표가 복원된다(build 는 어느 빌드를 잰 것인지).
      // build 는 셸(--gh-pages 가 배포 커밋을 넣어 준다) → GitHub Actions 의 GITHUB_SHA → 빈 값 순.
      const build = (process.env.SMOKE_BUILD || process.env.GITHUB_SHA || '').slice(0, 8);
      log('perf', `fps=${fps.avg.toFixed(1)} min=${fps.min.toFixed(1)} tweens=${tweens === null ? '?' : tweens} build=${build || '?'} target=${mode()}`);
    }
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
    log('state', `readyBattle=${readyBattle} errors=${errors.length} audioWarn=${audioWarn.length} netWarn=${netWarn.length}`);
  }
  if (shotPath) { try { fs.mkdirSync(require('path').dirname(shotPath), { recursive: true }); await page.screenshot({ path: shotPath }); log('shot', shotPath); } catch (e) { log('shot-fail', e.message); } }
  await browser.close();
  if (logPath) { try { fs.mkdirSync(require('path').dirname(logPath), { recursive: true }); fs.writeFileSync(logPath, lines.join('\n') + '\n'); } catch (e) { console.log('[smoke] 로그 저장 실패 ' + e.message); } }

  const markerOk = readyLobby || !requireMarker;
  const ok = errors.length === 0 && loaded && markerOk && (!wantBattle || readyBattle);
  const netTail = netWarn.length ? ` · 망 경고 ${netWarn.length}(게임 밖 호스트 · T83)` : '';
  console.log(ok ? `[smoke] ✅ 초록: 콘솔 에러 0 · 로딩 완료 · ${readyLobby ? '로비 도달' : '로비 마커 없음(구 빌드 · ⚠)'}${wantBattle ? ' · 전투 진입' : ''}${audioWarn.length ? ` · 오디오 경고 ${audioWarn.length}(판정 밖 · 주인 실기가 정본)` : ''}${netTail}`
                 : `[smoke] ❌ 빨강: errors=${errors.length} loaded=${loaded} readyLobby=${readyLobby}${wantBattle ? ` readyBattle=${readyBattle}` : ''} audioWarn=${audioWarn.length}${netTail}`);
  // 오디오 경고는 같은 문구가 수십 줄 반복되므로 «문구 ×N» 으로 묶어 찍는다(주인 지시 2026-09-07 · 결정 219).
  if (audioWarn.length) {
    const tally = new Map();
    for (const w of audioWarn) { const k = w.split('\n')[0].slice(0, 160); tally.set(k, (tally.get(k) || 0) + 1); }
    for (const [k, n] of [...tally.entries()].sort((a, b) => b[1] - a[1]).slice(0, 10)) console.log(`   ⚠ 오디오 ${k} ×${n}`);
  }
  for (const w of netWarn.slice(0, 10)) console.log('   ⚠ 망 ' + w.slice(0, 200));
  for (const e of errors.slice(0, 20)) console.log('   - ' + e.split('\n').slice(0, 8).join('\n     '));
  process.exit(ok ? 0 : 1);
})().catch(e => { console.error('[smoke] 실행 실패: ' + (e.stack || e.message)); process.exit(4); });

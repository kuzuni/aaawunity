#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""업적 표의 줄이 «실제로 오르는가» — 부르는 자리를 **코드에서** 세어 표와 맞댄다 (T287 · 검수 Q).

무엇이 비어 있었나
  T258 2회차가 EditMode 자 하나를 세웠다(`AchievementTests.표의_이름과_훅이_대는_이름이_같다`).
  그 자가 하는 일은 **표의 counter 가 `hooked` · `notYet` 목록 안에 있는가** 와
  **`hooked` 의 이름이 표에 있는가** 둘뿐이다. 두 목록은 **사람이 손으로 적은 글자**다.
  그래서 자의 주석이 스스로 경고한 바로 그 사고 —
      «한 글자만 달라도 그 줄은 **영원히 0** 인 채로 화면에 뜬다»
  가 **이 자를 그냥 통과한다**: `Quests.Ach(app, Quests.AchGiftClaim)` 한 줄을 지워도,
  상수의 값을 `"giftClam"` 으로 오타 내도, 목록과 표는 그대로라 초록이다.
  자는 «Game 어셈블리를 못 본다» 는 까닭으로 이름을 글자로 적었고(테스트는 Core 만 참조),
  그 제약은 참이다 — 그러니 **그 자리를 C# 밖에서 메운다.**

무엇을 재는가 (Assets/Scripts 만 · 테스트는 안 본다)
  ⓐ `const string` 들을 모아 **이름 → 글자** 표를 만든다.
  ⓑ 부르는 자리를 찾는다 — `Quests.Ach` · `Quests.AchOncePerDay` · `Quests.Bump` ·
     `Achievement.Add` · `Achievement.AddOncePerDay`.
     **둘째 인자가 글자로 풀리는 것만** 센다(`Ach(app, counter)` 처럼 변수를 받는 자리는
     그 자체가 배관이지 훅이 아니다 — 세면 «Quests.cs 가 전부 걸어 두었다» 는 거짓이 나온다).
  ⓒ `Quests.AchName` 의 **몸통을 읽어** 퀘스트 이름 → 업적 이름 잇기를 세운다(글자로 안 박는다).
     `Bump` 로 오른 퀘스트 이름은 그 잇기를 통과한 것만 업적에 닿는다.
  ⓓ `achievement.json` 의 줄과 맞댄다.

판정
  ✗ 표에 있는데 **부르는 자리가 없다**(«아직» 으로 적어 둔 것 제외) — 그 줄은 영원히 0 이다.
  ✗ 부르는 자리는 있는데 **표에 그 줄이 없다** — 세는 값을 아무도 안 읽는다.
  ✗ 자의 `notYet` 에 있는 이름에 **부르는 자리가 생겼다** — 옛 계약을 갈아 끼운다(T184).
  ✗ 자의 `hooked` 에 있는데 부르는 자리가 없다(아래 <see cref="KNOWN_ZERO"/> 밖의 것).

이 자는 막지 않는다 (늘 exit 0 · 결정 493)
  «아직 안 건 훅» 은 빌드 결함이 아니라 **순서**다(펫 시스템·남의 lock). 마지막 줄만 판정으로 남긴다(T281).

쓰는 법
    python3 tools/check_ach_hooks.py
    python3 tools/check_ach_hooks.py --self-test
"""
import json
import os
import re
import sys

SCRIPTS = "Assets/Scripts"
TABLE = "Assets/KkomaKnight/achievement.json"
TEST = "Assets/Tests/EditMode/AchievementTests.cs"

# «부르는 자리가 아직 없는 것이 옳다» 고 코드가 스스로 밝힌 줄 — 자의 `notYet` 에는 없는데 `hooked` 에 있는 것.
#   `petUpgrade` 는 퀘스트·업적 양쪽에 줄이 있는데 **양쪽 다 부를 자리가 없다**(펫 시스템 미구현 · T273).
#   그래도 자가 «훅» 쪽에 둔 까닭은 `AchName` 이 이미 이름을 이어 두어, T273 이 `Bump` 한 줄을 놓는 순간
#   업적까지 같이 살아나기 때문이다(자의 꼬리 주석). **T273 이 닫히면 이 줄을 지운다** — 그러면 이 자가
#   «부르는 자리가 생겼다» 가 아니라 아무 말도 안 하게 되고, 그것이 옳다.
# 이제 비었다 — T293 ⓘ 가 «펫 강화» 버튼을 세우며 `petUpgrade` 를 걸었다(2026-09-09 22:5X · 워커 K).
#   표 17줄이 전부 부르는 자리를 갖는다. 새 줄을 표에 적고 훅을 안 걸면 여기서 바로 드러난다.
KNOWN_ZERO = {}

# 부르는 자리로 세는 이름들. 둘째 인자가 counter 다.
CALL = re.compile(
    r"\b(?:Quests\.(?:Ach|AchOncePerDay|Bump)|Achievement\.(?:Add|AddOncePerDay))\s*\(\s*"
    r"[^,()]+,\s*([A-Za-z_][\w.]*|\"[^\"]*\")\s*[,)]"
)
CONST = re.compile(r"\bconst\s+string\s+(?P<body>[^;]+);", re.S)
PAIR = re.compile(r"(\w+)\s*=\s*\"([^\"]*)\"")
# 한 파일 안에서 «상수를 담아 두었다가 넘기는» 지역 변수 (T287 뒷맛 · 결정 825)
#   `string achBox = boxKey == "rare" ? Quests.AchChestRare : … : null;  if (achBox != null) Quests.Ach(App, achBox, n);`
#   부르는 자리가 **버젓이 있는데** 자가 «훅이 없다» 고 울었다(ShopScreen.cs:762 · 실측 2026-09-09 06:1X).
LOCAL = re.compile(r"\bstring\s+(?P<name>[A-Za-z_]\w*)\s*=\s*(?P<rhs>[^;{}]+);")
TOKEN = re.compile(r"\"[^\"]*\"|[A-Za-z_][\w.]*")


def read(path):
    with open(path, encoding="utf-8") as f:
        return f.read()


def cs_files(root):
    out = []
    for base, _, names in os.walk(root):
        out += [os.path.join(base, n) for n in names if n.endswith(".cs")]
    return sorted(out)


def consts(sources):
    """`const string A = "a", B = "b";` 를 모두 모아 이름 → 글자. 짧은 이름과 `Class.Ident` 둘 다 담는다."""
    m = {}
    for path, src in sources:
        cls = os.path.splitext(os.path.basename(path))[0]
        for blk in CONST.finditer(src):
            for name, lit in PAIR.findall(blk.group("body")):
                m[name] = lit
                m[cls + "." + name] = lit
    return m


def lit_of(tok, cm):
    """인자 하나를 글자로 푼다 — 못 풀면 None(변수를 받는 배관 자리다)."""
    if tok.startswith('"'):
        return tok[1:-1]
    if tok in cm:
        return cm[tok]
    return cm.get(tok.split(".")[-1]) if "." in tok else None


def locals_of(src, cm):
    """
    한 파일 안의 «상수를 담아 두는» 지역 변수 → 담길 수 있는 글자들 (T287 뒷맛).

    ⚠ <b>왜 필요한가</b> — 이름을 «고르는» 자리는 갈래를 타는 것이 자연스럽다:
        `string achBox = boxKey == "rare" ? Quests.AchChestRare : … : null;` 뒤에 `Quests.Ach(App, achBox, n)`.
    첫 판의 자는 «둘째 인자가 글자로 풀리는 것만» 셌으므로 <b>부르는 자리가 버젓이 있는데 «훅이 없다» 고 울었다</b>
    (`ShopScreen.cs:762` · 실측 2026-09-09 06:1X · 세운 지 한 시간 만이다). 자가 헛울면 다음 사람은 그 자를 안 본다.

    ⚠ <b>한 홉만 간다</b> — 오른쪽에서 <b>상수로 풀리는 낱말만</b> 거둔다. 변수에서 변수로 타고 가지 않는다:
    `string c = row.Counter;` 같은 자리는 여전히 <b>안 풀리고</b>, 그것이 옳다(그 자리는 배관이지 훅이 아니다).
    이 한 겹이 이 레포의 실제 꼴을 다 덮는다 — 더 따라가면 «자가 코드를 흉내 내기» 시작하고, 그때부터 자가 거짓말을 한다.
    """
    out = {}
    for m in LOCAL.finditer(src):
        rhs = m.group("rhs")
        # 갈래를 고르는 오른쪽(`boxKey == "rare" ? …`)에서는 **비교에 쓰인 글자**를 거두지 않는다 —
        # 그것은 «담길 이름» 이 아니라 «어느 갈래인가» 다. 안 걸러 두면 `counter == "adWatch"` 한 줄이
        # 지워진 훅을 살아 있는 것처럼 덮어 버린다(자를 눈멀게 하는 쪽으로 틀린다).
        toks = [t for t in TOKEN.findall(rhs) if not (t.startswith('"') and ("==" in rhs or "!=" in rhs))]
        lits = {lit for t in toks if (lit := lit_of(t, cm)) is not None}
        if lits:
            out.setdefault(m.group("name"), set()).update(lits)
    return out


def ach_name_map(quests_src, cm):
    """`AchName` 의 몸통을 읽어 «퀘스트 이름 → 업적 이름» 을 세운다 — 글자로 안 박는다(잇기가 바뀌면 이 자가 따라간다)."""
    i = quests_src.find("string AchName(")
    if i < 0:
        return None
    body = quests_src[i:quests_src.find("\n        }", i)]
    out = {}
    for cond, ret in re.findall(r"if\s*\((.*?)\)\s*return\s+(\w+)\s*;", body, re.S):
        names = [lit_of(x, cm) for x in re.findall(r"counter\s*==\s*([\w.]+)", cond)]
        for n in names:
            if n is None:
                continue
            if ret == "counter":
                out[n] = n
            elif ret != "null":
                t = lit_of(ret, cm)
                if t:
                    out[n] = t
    return out


def test_lists(src):
    """자가 손으로 적어 둔 두 목록(`hooked` · `notYet`)을 그대로 읽어 온다 — 자의 «주장» 이 이 자의 «실측» 과 맞는가를 보려고."""
    out = {}
    for key in ("hooked", "notYet"):
        m = re.search(key + r"\s*=\s*new\s+System\.Collections\.Generic\.HashSet<string>\s*\{(.*?)\};", src, re.S)
        out[key] = set(re.findall(r"\"([^\"]+)\"", m.group(1))) if m else set()
    return out


def measure(sources, table, tl):
    """돌려주는 것: (닿는 이름, 표에 없는 이름, 잇기, 부르는 자리 수) — 재는 일만 하고 판정은 안 한다(자기 검사가 이 함수를 부른다)."""
    cm = consts(sources)
    quests = next((s for p, s in sources if p.endswith("Quests.cs")), "")
    amap = ach_name_map(quests, cm) or {}
    rows = set(table)

    direct, bumped, sites = set(), set(), 0
    for _, src in sources:
        loc = locals_of(src, cm)                 # 그 파일 안에서 «상수를 담아 두는» 지역 변수(T287 뒷맛)
        for tok in CALL.findall(src):
            lit = lit_of(tok, cm)
            lits = {lit} if lit is not None else loc.get(tok, set())
            if not lits:         # 끝내 안 풀리는 자리 = 배관(Ach·Bump 의 몸통) — 훅이 아니다
                continue
            sites += 1
            for x in lits:
                (direct if x in rows else bumped).add(x)

    reached = set(direct)
    orphan = set()
    for name in bumped:                      # 표에 없는 글자 = 퀘스트 이름이거나, 아무도 안 읽는 이름
        t = amap.get(name)
        if t in rows:
            reached.add(t)
        elif t is not None:
            orphan.add(t)                    # 잇기는 있는데 표에 그 줄이 없다
    zero = rows - reached
    return reached, zero, orphan, amap, sites


def main():
    if "--self-test" in sys.argv[1:]:
        return self_test()

    sources = [(p, read(p)) for p in cs_files(SCRIPTS)]
    table = [r["counter"] for r in json.loads(read(TABLE))["list"]]
    tl = test_lists(read(TEST)) if os.path.exists(TEST) else {"hooked": set(), "notYet": set()}

    reached, zero, orphan, amap, sites = measure(sources, table, tl)

    print(f"· 업적 표 {len(table)}줄 · 부르는 자리 {sites}곳 · 실제로 오르는 줄 {len(reached)} · 영원히 0 인 줄 {len(zero)}")
    print(f"· `AchName` 잇기 {len(amap)}개(퀘스트 이름 → 업적 이름) — 자가 몸통을 읽어 세운 것이라 잇기가 바뀌면 따라간다")

    bad = []
    unex = sorted(zero - set(tl["notYet"]) - set(KNOWN_ZERO))
    if unex:
        bad.append("표에 있는데 부르는 자리가 없다: " + " · ".join(unex) + " — 그 줄은 영원히 0 이다")
    if orphan:
        bad.append("부르는 자리는 있는데 표에 그 줄이 없다: " + " · ".join(sorted(orphan)))
    stale = sorted(set(tl["notYet"]) & reached)
    if stale:
        bad.append("자의 `notYet` 에 적힌 " + " · ".join(stale) + " 에 부르는 자리가 생겼다 — 목록을 갈아 끼운다(T184)")
    lying = sorted(set(tl["hooked"]) - reached - set(KNOWN_ZERO))
    if lying:
        bad.append("자는 «걸었다» 는데 부르는 자리가 없다: " + " · ".join(lying) +
                   " — 훅이 지워졌거나 상수의 글자가 틀렸다(자의 목록은 손으로 적은 것이라 이것을 못 본다)")

    for k, why in sorted(KNOWN_ZERO.items()):
        if k in reached:
            bad.append(f"«{k}» 에 부르는 자리가 생겼다 — `KNOWN_ZERO` 에서 그 줄을 지운다({why})")
        else:
            print(f"· «{k}» 는 아직 0 이다 — {why}(알고 있는 것이라 판정을 안 바꾼다)")
    if zero & set(tl["notYet"]):
        print("· 자가 «아직» 으로 적어 둔 줄 " + " · ".join(sorted(zero & set(tl['notYet']))) + " 은 그대로 0 이다(제자리)")

    if bad:
        print("✗ check_ach_hooks: " + " · ".join(bad) + " — 보고만(막지 않는다 · 결정 493)")
    else:
        print(f"✓ check_ach_hooks: 표 {len(table)}줄 중 {len(reached)}줄이 실제로 오르고, 나머지는 전부 «아직» 으로 적힌 것뿐이다 (보고만 · T287)")
    return 0


def self_test():
    """자가 정말 무는가 — 손으로 만든 작은 소스로 갈래를 하나씩 부러뜨려 본다(T249)."""
    quests = '''
        public const string Kill = "kill", ChestOpen = "chestOpen", PetUpgrade = "petUpgrade";
        public const string AchAdWatch = "adWatch", AchArenaTry = "arenaTry", AchExpeditionQuick = "expeditionQuick";
        public const string ExpeditionFastClaim = "expeditionFastClaim";
        public const string AchChestRare = "chestOpenRare", AchChestEpic = "chestOpenEpic";
        static string AchName(string counter)
        {
            if (counter == Kill || counter == PetUpgrade) return counter;
            if (counter == ExpeditionFastClaim) return AchExpeditionQuick;
            return null;
        }
        public static void Ach(App app, string counter, int n = 1) { Achievement.Add(app.Save, counter, n); }
        }
'''
    ok_game = '''
        Quests.Ach(app, Quests.AchAdWatch);
        Quests.Ach(this, Quests.AchArenaTry);
        Quests.Bump(App, Quests.Kill, add);
        Quests.Bump(App, Quests.ChestOpen, n);
        Quests.Bump(app, Quests.ExpeditionFastClaim);
        string achBox = boxKey == "rare" ? Quests.AchChestRare : boxKey == "legend" ? Quests.AchChestEpic : null;
        if (achBox != null) Quests.Ach(App, achBox, n);
'''
    table = ["kill", "adWatch", "arenaTry", "expeditionQuick", "petUpgrade", "chestOpenRare", "chestOpenEpic"]
    tl = {"hooked": {"kill", "adWatch", "arenaTry", "expeditionQuick", "petUpgrade", "chestOpenRare", "chestOpenEpic"},
          "notYet": set()}

    def run(game_src, tbl=table):
        return measure([("Quests.cs", quests), ("Game.cs", game_src)], tbl, tl)

    bad, n = 0, 0

    def check(name, got, want):
        nonlocal bad, n
        n += 1
        okk = got == want
        bad += 0 if okk else 1
        print(f"  {'✔' if okk else '✘'} {name} — 잰 것 {sorted(got)} / 기대 {sorted(want)}")

    reached, zero, orphan, amap, sites = run(ok_game)
    check("성한 판: 여섯이 오르고 petUpgrade 만 0", reached,
          {"kill", "adWatch", "arenaTry", "expeditionQuick", "chestOpenRare", "chestOpenEpic"})
    check("성한 판: 표에 없는 이름 없음", orphan, set())
    n += 1
    okk = sites == 6
    bad += 0 if okk else 1
    print(f"  {'✔' if okk else '✘'} 배관은 안 센다 — 부르는 자리 {sites}곳(기대 6 · `Ach` 몸통의 `Achievement.Add(…, counter, …)` 는 변수라 제외)")

    # ⓐ 훅 한 줄을 지운다 → 그 줄이 «영원히 0» 으로 잡혀야 한다(자의 목록은 이것을 못 본다)
    reached, zero, _, _, _ = run(ok_game.replace("Quests.Ach(app, Quests.AchAdWatch);\n", ""))
    check("훅 한 줄을 지우면 adWatch 가 0 으로 잡힌다", zero, {"adWatch", "petUpgrade"})

    # ⓐ' 이름을 «담아 두었다가 넘기는» 갈래 — 첫 판의 자가 여기서 헛울었다(ShopScreen.cs:762 · T287 뒷맛)
    reached, zero, _, _, _ = run(ok_game)
    check("상수를 지역 변수에 담아 넘겨도 «걸렸다» 로 센다", reached & {"chestOpenRare", "chestOpenEpic"},
          {"chestOpenRare", "chestOpenEpic"})
    # 그 갈래가 «담는 줄» 을 지우면 도로 0 이어야 한다 — 무는 것까지 봐야 갈래를 잰 것이다
    reached, zero, _, _, _ = run(ok_game.replace("if (achBox != null) Quests.Ach(App, achBox, n);\n", ""))
    check("담아 놓고 안 부르면 도로 0 이다", zero, {"chestOpenRare", "chestOpenEpic", "petUpgrade"})
    # 비교에 쓰인 글자는 «담길 이름» 이 아니다 — 안 걸러 두면 지워진 훅을 살아 있는 것처럼 덮는다
    sneak = ok_game.replace('Quests.Ach(app, Quests.AchAdWatch);\n', '') + '\n        string x = s == "adWatch" ? A : B;\n        Quests.Ach(app, x);\n'
    _, zero, _, _, _ = run(sneak)
    check("비교에 쓰인 글자로는 안 살아난다", zero & {"adWatch"}, {"adWatch"})

    # ⓑ 상수의 글자를 오타 낸다 → 표에 없는 이름으로 잡혀야 한다
    reached, zero, orphan, _, _ = run(ok_game.replace("AchArenaTry", "AchAdWatch"))
    check("상수를 잘못 가리키면 arenaTry 가 0 이 된다", zero, {"arenaTry", "petUpgrade"})

    # ⓒ `AchName` 의 잇기를 끊는다 → Bump 로만 닿던 줄이 떨어져 나가야 한다
    cut = quests.replace("if (counter == ExpeditionFastClaim) return AchExpeditionQuick;", "")
    reached, zero, _, _, _ = measure([("Quests.cs", cut), ("Game.cs", ok_game)], table, tl)
    check("잇기를 끊으면 expeditionQuick 이 0 이 된다", zero, {"expeditionQuick", "petUpgrade"})

    # ⓓ 표에 없는 이름을 잇는다 → 아무도 안 읽는 값으로 잡혀야 한다
    _, _, orphan, _, _ = run(ok_game, [t for t in table if t != "expeditionQuick"])
    check("표에서 줄을 빼면 잇기가 «아무도 안 읽는 값» 으로 잡힌다", orphan, {"expeditionQuick"})

    if bad:
        print(f"✗ check_ach_hooks --self-test: 갈래 {bad}/{n} 이 기대와 다르다 — 위 줄의 «잰 것 ↔ 기대» 를 보라")
        return 1
    print(f"✓ check_ach_hooks --self-test: 갈래 {n}개가 전부 기대대로 갈린다(훅 지움 · 상수 오타 · 잇기 끊김 · 표에서 빠짐 · 배관 제외 · **담아 넘기기** 세 갈래)")
    return 0


if __name__ == "__main__":
    sys.exit(main())

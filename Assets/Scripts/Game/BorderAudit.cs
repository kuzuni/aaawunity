using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 테두리 전수 점검(T69 5항 · 주인 «행·카드·칸마다 검은 아웃라인») — 화면의 활성 <see cref="UiTag"/> 가운데 «행·카드·칸» 꼴 이름표를 모아
    /// <see cref="UiKit.HasDarkBorder"/> 로 어두운 테두리가 있는지 판정한다. PlayMode 게이트(BorderGateTests)가 모든 화면을 열고 부른다.
    /// 화면 묶음(T69-lobby …)이 끝난 화면만 <see cref="StrictScreens"/> 에 넣어 실패로 세고, 나머지는 «[BorderGate]» 표로 CI 로그에만 남긴다(TextAudit.ClipStrict 와 같은 방식).
    /// </summary>
    public static class BorderAudit
    {
        /// <summary>테두리 없음이 실패인 화면(묶음이 끝날 때마다 추가 · 전부 끝나면 모든 화면).</summary>
        /// <summary>
        /// ⚑ T94 ⓑ(주인 2026-09-07 05:3X «메인 로비에 Border 있는 것들은 걍 없애셈») — **<c>01_lobby</c> 는 strict 목록에서 뺐다**.
        /// 로비만 예외이고 다른 화면의 T69 테두리는 그대로다. 로비는 «[BorderGate]» 표에만 남아 보고된다(실패로 세지 않는다).
        /// </summary>
        public static readonly HashSet<string> StrictScreens = new HashSet<string>
        {
            "02_battle", "06_gear", "07_gear_detail", "08_gear_fuse", "13_pet", "14_pet_detail", "09_shop_1", "10_shop_2", "12_settings",
            "20_dungeon", "21_dungeon_detail", "22_arena", "23_arena_enter", "24_arena_challenge", "25_arena_rank_reward", "26_arena_shop",   // T69-events(던전·아레나 묶음)
            "04_perks", "05_perks_list", "res_win", "res_lose",   // T69-overlay(특전 카드 · 결과 팝업 묶음)
            "15_quest", "16_attendance", "11_shop_special",   // T69-lobbypopups(로비 팝업 묶음 · 11 은 T72 lock 이 풀린 뒤 같은 묶음이 마저 닫았다)
            "17_daily_gift",   // T69 마무리 — 표의 마지막 «strict 아닌» 화면이었다(CI #255 에서 이미 5/5) · 이로써 01_lobby(주인이 뺀 화면 · T94 ⓑ)만 보고 전용이다
        };

        /// <summary>
        /// 테두리가 없는 게 맞는 것(ROUTINE T69 5항 «예외 목록») — 이름표 이름 또는 <see cref="UiTag.Members"/> 조각의 오브젝트 이름.
        /// 레퍼런스 jpg 에 «상자» 가 없는 담개(속의 칸이 각자 테두리를 가진다)를 넣는다 — 이름표 이름은 배치 표(ref-layout)의 이름이라 바꿀 수 없어서 여기서 뺀다.
        /// <para>딱 하나 다른 까닭으로 든 것이 있다: <b>주인이 그 화면의 테두리를 없애라고 한 자리</b>(로비 · T94 ⓑ · 맨 아래 줄). 새 지시가 T69 «전 화면 테두리» 보다 뒤라 그 화면에서는 새 지시가 이긴다.</para>
        /// </summary>
        public static readonly HashSet<string> Exempt = new HashSet<string>
        {
            "상단 바(아바타+재화 줄 전체)",   // 01·06·09·13·20 — 상단 재화 줄 «전체» 는 담개다(레퍼런스에 띠 상자가 없다 · 아바타·pill 이 각자 테두리)
            "PowerCell",                        // 01 전투력 = 칼 아이콘 + 주황 숫자뿐(레퍼런스 01 에 상자 없음)
            "장비 무대(캐릭터+슬롯)",           // 06 캐릭터 무대 = 들판 그림 전폭(레퍼런스 06 에 상자 없음 · 속의 슬롯 6칸이 각자 테두리) — 이름표에 «슬롯» 이 들어 걸린다(T69-gear)
            "이름줄",                           // 07 세부 팝업의 장비 이름 = 패널 위 맨 글자(레퍼런스 07 «Shadow Treads» 에 상자 없음 · 아래 pill 2개가 테두리) (T69-gear)
            "합계 줄",                          // 13 펫 탭의 «+0 ❤ | +0 🛡 | +0 🗡» = 어두운 바탕 위 맨 글자(레퍼런스 13 «+168 ❤ | +165 🛡 | +74 🗡» 에 상자 없음 · 14 의 «패시브 수치 줄» 도 같은 꼴이라 «수치» 로 이미 제외) (T69-pet · 결정 171)
            "광고/무료 카드 2개",               // 09·10 작은 상자 카드 아래 «광고 + 1회 가격» 버튼 줄의 측정용 빈 rect(Box:*/Bottom · 자식 없음) — 레퍼런스 10 도 그 줄에 상자가 없고 버튼 둘이 각자 외곽선 · 카드 자체가 Ink 링 (T69-shop · 결정 195)
            "티어 줄",                          // 22 아레나 카드 아래 «🥉 브론즈» = 카드 몸통 위 맨 아이콘+글자(레퍼런스 22 에 상자 없음 · 카드가 제 외곽선) (T69-events)
            "티켓 줄",                          // 21 던전 세부 팝업의 «🎫 3» = 버튼 두 개 위 맨 아이콘+숫자(레퍼런스 21 에 상자 없음) (T69-events)
            "티켓·전투력 줄",                   // 24 도전 팝업 머리줄 = 어두운 티켓 pill + 맨 «⚔ 전투력»(레퍼런스 24 에 줄 상자가 없고 pill 만 제 상자) (T69-events)
            "상대 목록(5줄)",                   // 24 상대 5줄의 측정용 빈 rect(줄들은 팝업 상자의 형제라 자식이 없다) — 레퍼런스 24 도 5줄을 감싸는 상자가 없고 줄마다 외곽선 (T69-events)
            "보상 목록(4줄)",                   // 25 순위 보상 4줄의 같은 꼴 담개 (T69-events)
            "트랙 아이콘(1칸)",                 // 15 점수 트랙의 첫 칸 = 메달 하나뿐(레퍼런스 15 «60» 은 맨몸 메달 · 뒤따르는 보상 칸 5개만 상자다) (T69-lobbypopups)
            "TrackScore",                       // 같은 칸이 «트랙 아이콘 줄(6칸)» 의 조각으로도 세어진다 — 조각 이름으로 한 번 더 뺀다(Track 헬퍼의 첫 칸 전용 이름) (T69-lobbypopups)
            "새로고침 줄",                      // 15 «⏱ 새로고침까지 --:--:--» = 트랙 상자와 목록 상자 «사이» 의 맨 글자 줄(레퍼런스 15 에 상자 없음 · «티켓 줄»·«티어 줄» 과 같은 꼴 · 글자 칸이 줄 rect 보다 넓어 링을 그리면 글자를 가로지른다) (T69-lobbypopups)
            "칸 머리(1칸)",                     // 16 하루 칸의 «N일차» 자주색 띠 = 칸 «안» 의 머리 구역(레퍼런스 16 도 칸 하나가 통째로 외곽선이고 머리는 그 위쪽 구역일 뿐 · 칸 자체는 DayFrame 이 Bordered) (T69-lobbypopups)
            "7일 칸 머리",                      // 16 7일차 넓은 칸의 같은 꼴 (T69-lobbypopups)
            "카드 그림(2)",                     // 11 특권 카드의 그림(영사기·티켓)은 카드의 «형제» 로 떠 있는 그림이다 — 레퍼런스 11 도 그림에 상자가 없고 카드가 제 외곽선을 낸다 (T69-lobbypopups)
            // 01 로비 챕터 카드 — 위의 것들과 달리 «담개라서» 가 아니라 **주인이 로비의 테두리를 다 없애라고 해서** 뺀다
            // (2026-09-07 T94 ⓑ «메인 로비에 Border 있는 것들은 걍 없애셈» · T69 «전 화면 검은 아웃라인» 보다 뒤에 온 지시라 로비에서는 이쪽이 이긴다).
            // 이 이름표는 로비 화면의 것이지만 로비 «위에» 뜨는 팝업 화면(12·15·16 …)을 감사할 때도 같이 세어지므로 여기서 한 번만 뺀다 — T118 · 결정 301.
            "챕터 카드(스테이지 그림)",
        };

        /// <summary>«행·카드·칸» 으로 보는 이름표 낱말 — 이 가운데 하나가 이름에 들어가면 대상.</summary>
        static readonly string[] CellWords = { "칸", "카드", "행", "슬롯", "기둥", "줄" };
        /// <summary>대상에서 빼는 낱말 — 글자 줄·버튼·타이머·컨테이너 같은 «칸이 아닌 것»(테두리 없는 게 맞는 것 · ROUTINE T69 5항 예외 목록).</summary>
        static readonly string[] SkipWords = { "버튼", "문구", "숫자", "타이머", "시각", "라벨", "(참고·컨테이너)", "진행바", "그리드", "격자", "영역", "제목", "안내", "수치", "선", "점(" };

        public sealed class Row
        {
            public string Screen, Tag, Path;
            public bool HasBorder;
            public override string ToString() => $"[{Screen}] «{Tag}» {Path}" + (HasBorder ? " ✓테두리" : " ⛔테두리 없음");
        }

        /// <summary>이 이름표가 «행·카드·칸» 대상인가.</summary>
        public static bool IsCellTag(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (var s in SkipWords) if (name.Contains(s)) return false;
            foreach (var w in CellWords) if (name.Contains(w)) return true;
            return false;
        }

        /// <summary>root 아래 활성 이름표를 판정한다 — 멤버(«줄(N칸)» 규약)가 있으면 멤버마다 한 줄.</summary>
        public static List<Row> Collect(string screen, Transform root)
        {
            var rows = new List<Row>();
            if (root == null) return rows;
            foreach (var tag in root.GetComponentsInChildren<UiTag>(false))
            {
                if (tag == null || !tag.isActiveAndEnabled || !IsCellTag(tag.Name) || Exempt.Contains(tag.Name)) continue;
                if (tag.Members.Count == 0) { rows.Add(new Row { Screen = screen, Tag = tag.Name, Path = PathOf(tag.transform, root), HasBorder = Bordered(tag.transform) }); continue; }
                foreach (var m in tag.Members)
                {
                    if (m == null || !m.gameObject.activeInHierarchy || Exempt.Contains(m.name)) continue;
                    rows.Add(new Row { Screen = screen, Tag = tag.Name, Path = PathOf(m, root), HasBorder = Bordered(m) });
                }
            }
            return rows;
        }

        /// <summary>
        /// 이 칸이 T69 «테두리» 규칙을 만족하는가 — ⓐ 어두운 링이 있거나(<see cref="UiKit.HasDarkBorder"/>) ⓑ <b>아이템 칸</b>(<c>ItemFrame_01</c> 조각)이다.
        /// ⓑ 는 T103 3항 ⓐ 의 면제다(주인 2026-09-07 «조각 그대로 · 색깔만 바뀌는 식» 이 T69 «전 화면 검은 아웃라인» 보다 뒤에 온 지시 · 워커 결정 기록):
        /// 조각 제 링은 등급 변형이면 짙은 갈색, 빈 칸이면 옅은 갈색이라 «어둡다» 판정에 걸리지 않는데, 그 밝기가 조각의 정본이다.
        /// </summary>
        static bool Bordered(Transform cell) => UiKit.HasDarkBorder(cell) || GearUi.HasItemFrame(cell);

        static string PathOf(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (var c = t; c != null && c != root && parts.Count < 7; c = c.parent) parts.Add(c.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>화면별 «대상 수 · 테두리 있음 · 없음 · strict» 표(마크다운) — CI 로그에서 묶음 워커가 읽는다.</summary>
        public static string Summary(List<Row> rows)
        {
            var byScreen = new Dictionary<string, List<Row>>();
            foreach (var r in rows) { if (!byScreen.TryGetValue(r.Screen, out var l)) byScreen[r.Screen] = l = new List<Row>(); l.Add(r); }
            var sb = new StringBuilder();
            sb.AppendLine("| 화면 | 행·카드·칸 | 테두리 있음 | 없음 | strict |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var kv in byScreen)
            {
                int ok = 0; foreach (var r in kv.Value) if (r.HasBorder) ok++;
                sb.AppendLine($"| {kv.Key} | {kv.Value.Count} | {ok} | {kv.Value.Count - ok} | {(StrictScreens.Contains(kv.Key) ? "✔" : "")} |");
            }
            return sb.ToString();
        }
    }
}

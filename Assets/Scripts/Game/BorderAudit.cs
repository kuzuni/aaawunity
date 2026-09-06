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
        public static readonly HashSet<string> StrictScreens = new HashSet<string> { "02_battle", "01_lobby", "06_gear", "07_gear_detail", "08_gear_fuse", "13_pet", "14_pet_detail", "09_shop_1", "10_shop_2" };

        /// <summary>
        /// 테두리가 없는 게 맞는 것(ROUTINE T69 5항 «예외 목록») — 이름표 이름 또는 <see cref="UiTag.Members"/> 조각의 오브젝트 이름.
        /// 레퍼런스 jpg 에 «상자» 가 없는 담개(속의 칸이 각자 테두리를 가진다)만 넣는다 — 이름표 이름은 배치 표(ref-layout)의 이름이라 바꿀 수 없어서 여기서 뺀다.
        /// </summary>
        public static readonly HashSet<string> Exempt = new HashSet<string>
        {
            "상단 바(아바타+재화 줄 전체)",   // 01·06·09·13·20 — 상단 재화 줄 «전체» 는 담개다(레퍼런스에 띠 상자가 없다 · 아바타·pill 이 각자 테두리)
            "PowerCell",                        // 01 전투력 = 칼 아이콘 + 주황 숫자뿐(레퍼런스 01 에 상자 없음)
            "장비 무대(캐릭터+슬롯)",           // 06 캐릭터 무대 = 들판 그림 전폭(레퍼런스 06 에 상자 없음 · 속의 슬롯 6칸이 각자 테두리) — 이름표에 «슬롯» 이 들어 걸린다(T69-gear)
            "이름줄",                           // 07 세부 팝업의 장비 이름 = 패널 위 맨 글자(레퍼런스 07 «Shadow Treads» 에 상자 없음 · 아래 pill 2개가 테두리) (T69-gear)
            "합계 줄",                          // 13 펫 탭의 «+0 ❤ | +0 🛡 | +0 🗡» = 어두운 바탕 위 맨 글자(레퍼런스 13 «+168 ❤ | +165 🛡 | +74 🗡» 에 상자 없음 · 14 의 «패시브 수치 줄» 도 같은 꼴이라 «수치» 로 이미 제외) (T69-pet · 결정 171)
            "광고/무료 카드 2개",               // 09·10 작은 상자 카드 아래 «광고 + 1회 가격» 버튼 줄의 측정용 빈 rect(Box:*/Bottom · 자식 없음) — 레퍼런스 10 도 그 줄에 상자가 없고 버튼 둘이 각자 외곽선 · 카드 자체가 Ink 링 (T69-shop · 결정 195)
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
                if (tag.Members.Count == 0) { rows.Add(new Row { Screen = screen, Tag = tag.Name, Path = PathOf(tag.transform, root), HasBorder = UiKit.HasDarkBorder(tag.transform) }); continue; }
                foreach (var m in tag.Members)
                {
                    if (m == null || !m.gameObject.activeInHierarchy || Exempt.Contains(m.name)) continue;
                    rows.Add(new Row { Screen = screen, Tag = tag.Name, Path = PathOf(m, root), HasBorder = UiKit.HasDarkBorder(m) });
                }
            }
            return rows;
        }

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

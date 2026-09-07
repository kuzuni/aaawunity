using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 프로필(T96-profile ⓒ · 주인 2026-09-07 «`Social_Profile_Avatar`·`Social_Profile_Nickname` 이거 좀 써라 프리팹들» ·
    /// 지시서 «상단 재화 바의 아바타를 누르면 아바타 고르기»).
    ///
    /// <b>1단계 = 아바타(테두리) 고르기</b> — 우리 초상은 언제나 내 캐릭터(<see cref="HeroView"/>)라 «고를 수 있는 것» 은 그 초상을 감싸는
    /// <c>ProfileFrame_02</c> 의 <b>색 다섯</b>(노랑 기본 · 파랑 · 빨강 · 자주 · 회색 — 팩에 있는 변형 그대로)이다. 고른 색은 세이브(<see cref="SaveData.ProfileColor"/>)에 남고
    /// <see cref="TopBar"/> 가 그 조각을 세운다(기본값 = 노랑 = 종전과 같은 조각이라 안 고르면 화면이 그대로다).
    ///
    /// 팝업은 주인이 지목한 <c>Social_Profile_Avatar</c> <b>그대로</b>: 칸(<c>ListItem_Avatar</c>) 여섯 중 <b>다섯</b>에 우리 색을 배정하고 남는 칸은 끈다(개수만 우리 데이터로 · T44 규칙).
    /// 칸 조각에는 <b>버튼이 없다</b>(실측 — 데모의 목록 줄) → <see cref="UiKit.Clickable"/> 이 칸 자체에 Button 을 붙인다(우편함 결정 303 과 같은 함정).
    ///
    /// <b>닉네임은 아직 안 붙였다</b> — <c>Social_Profile_Nickname</c> 의 입력칸(<c>InputField_03_Edit</c>)이 <b>전부 TMP</b>(TMP_InputField + TextMeshProUGUI)인데
    /// 우리 <see cref="UiKit.Adopt"/> 는 TMP_Text 를 파괴하고 uGUI Text 로 갈아 끼우므로 그대로 쓰면 입력칸이 제 글자 컴포넌트를 잃는다(결정 기록).
    /// 그 자리는 UiKit 에 «TMP 입력칸 → uGUI InputField» 를 더하는 별도 회차 몫이다.
    /// 이름 계약(테스트): 칸 = <c>Avatar:&lt;색&gt;</c> · 고르기 = <c>ChooseBtn</c>.
    /// </summary>
    public static class Profile
    {
        /// <summary>칸 오브젝트 이름 앞머리(테스트가 찾는다).</summary>
        public const string RowPrefix = "Avatar:";
        /// <summary>«선택» 버튼 이름(고정).</summary>
        public const string ChooseName = "ChooseBtn";
        /// <summary>팝업 안 칸 조각 이름(데모 프리팹).</summary>
        public const string RowPiece = "ListItem_Avatar";

        /// <summary>고를 수 있는 테두리 색 — 팩의 <c>ProfileFrame_02_*</c> 변형 다섯. 첫 값이 기본이다.</summary>
        public static readonly string[] Colors = { "yellow", "blue", "red", "plum", "gray" };

        /// <summary>지금 색(세이브에 없거나 모르는 값이면 기본 = 첫 색).</summary>
        public static string Current(SaveData s)
        {
            string c = s != null ? s.ProfileColor : null;
            if (!string.IsNullOrEmpty(c)) foreach (var k in Colors) if (k == c) return k;
            return Colors[0];
        }
        /// <summary>그 색의 테두리 조각 카탈로그 키.</summary>
        public static string FrameKey(SaveData s) => "ui.profileFrame." + Current(s);

        /// <summary>아바타(테두리) 고르기 팝업 — 탑바 아바타를 누르면 열린다.</summary>
        public static void OpenAvatar(App app)
        {
            if (app == null) return;
            var root = app.Overlay.OpenPrefab("ui.profileAvatar");
            var rt = (RectTransform)root.transform;
            var popup = UiKit.Find(rt, "Popup") as RectTransform;
            if (popup == null) return;                                   // 조각 구성이 바뀌면 조용히 빈 어둠(빨간 줄 0)
            Retitle(popup);

            string picked = Current(app.Save);
            var rows = new List<RectTransform>();
            foreach (var t in rt.GetComponentsInChildren<Transform>(true))
            {
                var r = t as RectTransform;
                if (r != null && r.name.StartsWith(RowPiece, System.StringComparison.Ordinal)) rows.Add(r);
            }
            rows.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));

            var checks = new List<Transform>();
            for (int i = 0; i < rows.Count; i++)
            {
                bool on = i < Colors.Length;
                rows[i].gameObject.SetActive(on);
                if (!on) continue;
                string color = Colors[i];
                rows[i].name = RowPrefix + color;
                // 칸 안 그림을 우리 색 조각으로 — 팝업에서 보는 것이 곧 탑바에 서는 것
                var area = UiKit.Find(rows[i], "ProfileArea") as RectTransform;
                if (area != null)
                {
                    UiKit.Clear(area);
                    var piece = UiKit.Spawn("ui.profileFrame." + color, area);
                    var prt = (RectTransform)piece.transform;
                    UiKit.FitScale(prt, area.rect.size);
                }
                var check = UiKit.Find(rows[i], "Check");
                if (check != null) { check.gameObject.SetActive(color == picked); checks.Add(check); }
                string c2 = color; int idx = i;
                UiKit.Clickable(rows[idx], () =>
                {
                    picked = c2;
                    for (int k = 0; k < checks.Count && k < Colors.Length; k++) checks[k].gameObject.SetActive(Colors[k] == picked);
                });
            }

            // «Choose» 버튼 = 고른 색을 저장하고 닫는다 · 닫기(X)는 그냥 닫는다
            var choose = ChooseButton(rt);
            if (choose != null)
            {
                choose.name = ChooseName;
                var label = choose.GetComponentInChildren<Text>(true);
                if (label != null) UiKit.SetText(label.transform, "", "선택", kind: TextKind.Button);
                UiKit.Clickable(choose.transform, () =>
                {
                    app.Save.ProfileColor = picked;
                    app.Persist();
                    app.Overlay.Close();
                    app.Current?.Refresh();
                    app.ShowScreen(app.Current != null ? app.Current.Name : "lobby");   // 탑바를 새 색으로 다시 세운다
                    app.Toast("아바타를 바꿨습니다");
                });
            }
            var close = UiKit.FindAny(rt, "Button_Close_01", "Button_Close_Square_01");
            if (close != null) UiKit.Clickable(close, () => app.Overlay.Close());
        }

        /// <summary>«Choose» 버튼 조각 — 칸(줄)이 아닌 버튼 가운데 아래쪽 것 하나(조각 이름 <c>Button_02_Blue</c>).</summary>
        static Transform ChooseButton(RectTransform rt)
        {
            var t = UiKit.Find(rt, "Button_02_Blue");
            if (t != null) return t;
            foreach (var b in rt.GetComponentsInChildren<Button>(true))
                if (b != null && !b.name.StartsWith(RowPrefix, System.StringComparison.Ordinal)) return b.transform;
            return null;
        }

        /// <summary>영문 데모 글자 → 우리말(T34 ⓒ) — 제목 «Avatar» · 버튼 «Choose»(버튼은 위에서 다시 쓴다).</summary>
        static void Retitle(RectTransform popup)
        {
            foreach (var t in popup.GetComponentsInChildren<Text>(true))
            {
                if (t == null) continue;
                string s = (t.text ?? "").Trim();
                if (s == "Avatar") UiKit.SetText(t.transform, "", "아바타", kind: TextKind.Title);
                else if (s == "Choose") UiKit.SetText(t.transform, "", "선택", kind: TextKind.Button);
            }
        }
    }
}

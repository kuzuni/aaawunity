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
    /// <b>2단계 = 이름(닉네임) 바꾸기</b> — 주인이 지목한 <c>Social_Profile_Nickname</c> <b>그대로</b>. 막고 있던 것(입력칸이 전부 TMP 라
    /// <see cref="UiKit.Adopt"/> 가 TMP_Text 를 파괴하면 제 글자를 잃는 것)은 <see cref="UiKit.Adopt"/> 가 <b>TMP 입력칸을 uGUI InputField 로 갈아 끼우게</b> 고쳐 풀었다.
    /// 규칙(2~12자 · 기본 «꼬마기사»)은 순수 C# <see cref="Nickname"/> 한 곳에 있고 길이 한도는 <b>그 조각에서 실측</b>했다(«/12» · «Enter at least 2 characters.»).
    /// <b>입구</b>는 아바타 팝업의 제목 줄이다 — 제목이 «Avatar» 가 아니라 <b>지금 내 이름</b>을 보여 주고 누르면 이름 바꾸기가 열린다(조각을 하나도 더하지 않는다 · 결정 기록).
    /// 지은 이름은 세이브(<see cref="SaveData.Nick"/>)에 남고 아레나 시상대 «나» 줄이 그 이름으로 선다(기본값이 종전 이름이라 안 고치면 화면 불변).
    /// 이름 계약(테스트): 칸 = <c>Avatar:&lt;색&gt;</c> · 고르기 = <c>ChooseBtn</c> · 이름 줄 = <c>NickBtn</c> · 입력칸 = <c>NickInput</c> · 확인 = <c>NickOkBtn</c> · 글자 수 = <c>NickCount</c>.
    /// </summary>
    public static class Profile
    {
        /// <summary>칸 오브젝트 이름 앞머리(테스트가 찾는다).</summary>
        public const string RowPrefix = "Avatar:";
        /// <summary>«선택» 버튼 이름(고정).</summary>
        public const string ChooseName = "ChooseBtn";
        /// <summary>팝업 안 칸 조각 이름(데모 프리팹).</summary>
        public const string RowPiece = "ListItem_Avatar";
        /// <summary>아바타 팝업 제목 = «지금 이름» 이자 이름 바꾸기 입구(테스트가 찾는다).</summary>
        public const string NickName = "NickBtn";
        /// <summary>이름 바꾸기 팝업의 입력칸·확인 버튼·글자 수 표시(고정 이름).</summary>
        public const string NickInputName = "NickInput", NickOkName = "NickOkBtn", NickCountName = "NickCount";

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
        /// <summary>아바타 테두리 조각의 카탈로그 키 앞머리 — 탑바가 «옛 색 조각» 을 찾아 지울 때 쓴다(T96-profile · <see cref="Game.TopBar"/>).</summary>
        public const string FrameKeyPrefix = "ui.profileFrame.";
        public static string FrameKey(SaveData s) => FrameKeyPrefix + Current(s);

        /// <summary>아바타(테두리) 고르기 팝업 — 탑바 아바타를 누르면 열린다.</summary>
        public static void OpenAvatar(App app)
        {
            if (app == null) return;
            var root = app.Overlay.OpenPrefab("ui.profileAvatar");
            var rt = (RectTransform)root.transform;
            var popup = UiKit.Find(rt, "Popup") as RectTransform;
            if (popup == null) return;                                   // 조각 구성이 바뀌면 조용히 빈 어둠(빨간 줄 0)
            Retitle(app, popup);

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

        /// <summary>
        /// 영문 데모 글자 → 우리말(T34 ⓒ) · 버튼 «Choose» → «선택»(버튼은 위에서 다시 쓴다).
        /// 제목 «Avatar» 자리는 <b>지금 내 이름</b>이 서고 그 줄을 누르면 <see cref="OpenNickname"/> 이 열린다 —
        /// 프리팹에 조각을 하나도 더하지 않고 «이름 바꾸기» 입구를 내기 위해서다(T96-profile 2단계 · 결정 기록).
        /// </summary>
        static void Retitle(App app, RectTransform popup)
        {
            foreach (var t in popup.GetComponentsInChildren<Text>(true))
            {
                if (t == null) continue;
                string s = (t.text ?? "").Trim();
                if (s == "Avatar")
                {
                    UiKit.SetText(t.transform, "", Nickname.Of(app.Save), kind: TextKind.Title);
                    var strip = Ribbon(t.transform, popup);
                    strip.name = NickName;
                    UiKit.Clickable(strip, () => OpenNickname(app));
                }
                else if (s == "Choose") UiKit.SetText(t.transform, "", "선택", kind: TextKind.Button);
            }
        }

        /// <summary>제목 글자를 품은 리본 조각(누를 자리) — 글자의 부모 중 팝업 바로 아래 것. 못 찾으면 글자 자신.</summary>
        static Transform Ribbon(Transform text, RectTransform popup)
        {
            var cur = text;
            while (cur != null && cur.parent != null && cur.parent != popup && cur.parent != popup.parent) cur = cur.parent;
            return cur != null && cur != popup ? cur : text;
        }

        /// <summary>
        /// 이름 바꾸기 팝업 — 주인 지목 <c>Social_Profile_Nickname</c> 그대로(자리·크기·글자 자리 불변 · 부품만 우리 것).
        /// 조각의 글자를 «원문» 으로 알아본다(제목 «Nickname» · 안내 «Enter at least …» · 글자 수 «0/12» · 버튼 «Choose») — 이름에 기대지 않아 조각이 바뀌어도 조용히 넘어간다.
        /// 닫거나 지으면 아바타 팝업으로 돌아간다(왔던 자리).
        /// </summary>
        public static void OpenNickname(App app)
        {
            if (app == null) return;
            var root = app.Overlay.OpenPrefab("ui.profileNick");
            var rt = (RectTransform)root.transform;
            var input = rt.GetComponentInChildren<InputField>(true);
            Text count = null, desc = null, okLabel = null;
            foreach (var t in rt.GetComponentsInChildren<Text>(true))
            {
                if (t == null) continue;
                if (input != null && t.transform.IsChildOf(input.transform)) continue;   // 입력칸 제 글자·자리표시는 건드리지 않는다
                string s = (t.text ?? "").Trim();
                if (s == "Nickname") UiKit.SetText(t.transform, "", "이름 바꾸기", kind: TextKind.Title);
                else if (s == "Choose") okLabel = t;
                else if (s.StartsWith("Enter", System.StringComparison.OrdinalIgnoreCase)) desc = t;
                else if (s.IndexOf('/') >= 0) count = t;
            }
            if (desc != null) UiKit.SetText(desc.transform, "", $"{Nickname.MinLen}~{Nickname.MaxLen}자로 지어 주세요", kind: TextKind.Aux);

            Button ok = null;
            if (okLabel != null)
            {
                var btn = Ribbon(okLabel.transform, (UiKit.Find(rt, "Popup") as RectTransform) ?? rt);
                btn.name = NickOkName;
                UiKit.SetText(okLabel.transform, "", "확인", kind: TextKind.Button);
                ok = UiKit.Clickable(btn, () =>
                {
                    string want = input != null ? input.text : null;
                    if (!Nickname.Set(app.Save, want)) { app.Toast($"이름은 {Nickname.MinLen}~{Nickname.MaxLen}자여야 합니다"); return; }
                    app.Persist();
                    app.ShowScreen(app.Current != null ? app.Current.Name : "lobby");   // 이름이 서는 자리(아레나 «나» 줄)를 새 이름으로 다시 세운다
                    app.Toast("이름을 바꿨습니다");
                    OpenAvatar(app);
                });
            }
            if (count != null) count.name = NickCountName;

            if (input != null)
            {
                input.name = NickInputName;
                input.characterLimit = Nickname.MaxLen;
                input.text = Nickname.Of(app.Save);
                input.onValueChanged.RemoveAllListeners();
                input.onValueChanged.AddListener(v => Tally(input, count, ok));
            }
            Tally(input, count, ok);

            var close = UiKit.FindAny(rt, "Button_Close_01", "Button_Close_Square_01");
            if (close != null) UiKit.Clickable(close, () => OpenAvatar(app));
        }

        /// <summary>글자 수 표시 갱신 + «확인» 을 쓸 수 있는지 — 규칙(<see cref="Nickname"/>)이 판정한다.</summary>
        static void Tally(InputField input, Text count, Button ok)
        {
            string v = input != null ? input.text : "";
            int n = Nickname.Clean(v).Length;
            if (count != null) UiKit.SetText(count.transform, "", $"{n}/{Nickname.MaxLen}", kind: TextKind.Aux);
            if (ok != null) UiKit.SetInteractable(ok, n >= Nickname.MinLen);
        }
    }
}

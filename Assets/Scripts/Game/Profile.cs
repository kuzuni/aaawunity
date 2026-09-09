using System.Collections.Generic;
using KkomaKnight.Core;
using TMPro;
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

        /// <summary>고를 수 있는 테두리 색 — 팩의 <c>ProfileFrame_02_*</c> 변형 다섯. 첫 값이 기본이다.
        /// <para>⚠ T262 ⓐ 뒤로 <b>팝업이 고르는 것은 색이 아니라 초상 아이콘</b>이다. 색은 세이브에 남아 테두리로 쓰이지만 지금 고르는 길은 없다(주인이 색 고르기를 없애라고 한 적은 없어 지우지 않았다).</para></summary>
        public static readonly string[] Colors = { "yellow", "blue", "red", "plum", "gray" };

        /// <summary>
        /// 고를 수 있는 <b>초상 아이콘</b>(T262 ⓐ · 주인 2026-09-09 «프로필 … 이미지가 PvP 에서 더미 데이터들 부분 아이콘들처럼 설정하게 해야 함. <b>플레이어 이미지 말고</b>»).
        /// <para>
        /// 아레나가 더미 상대에게 쓰는 그 넷과 <b>같은 키</b>다 — 주인이 «그 아이콘들처럼» 이라고 한 것이 이것이다.
        /// ⚠ 지금 같은 목록이 <c>EventsScreen.Foes</c> 에도 있다. <b>여기가 정본</b>이고 그쪽을 이리로 돌리는 것은 T262 ⓑ 의 일이다
        /// (그 파일이 T251 lock 안이라 이 회차에서 못 건드린다 · 규약 3항).
        /// </para>
        /// 첫 값이 기본이다(안 고르면 종전처럼 아무거나 아니라 <b>언제나 같은</b> 초상이 선다).
        /// </summary>
        public static readonly string[] Icons = { "ui.iconFoe1", "ui.iconFoe2", "ui.iconFoe3", "ui.iconFoe4" };

        /// <summary>지금 고른 초상 아이콘(세이브에 없거나 모르는 값이면 기본 = 첫 아이콘).</summary>
        public static string CurrentIcon(SaveData s)
        {
            string k = s != null ? s.ProfileIcon : null;
            if (!string.IsNullOrEmpty(k)) foreach (var i in Icons) if (i == k) return i;
            return Icons[0];
        }

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

        /// <summary>
        /// 테두리 조각(<c>ProfileFrame_02</c>) 안에 <b>초상 아이콘</b>을 넣는다(T262 ⓐ) — 팝업 칸과 탑바가 <b>같은 함수</b>를 쓴다.
        /// <para>
        /// 조각의 마스크 안에는 데모의 <c>Character</c> 그림이 들어 있어 먼저 끈다(종전 <see cref="HeroView"/> 자리와 같은 처리).
        /// 마스크를 못 찾으면 조각 자신에 넣는다 — 조각 구성이 바뀌어도 <b>초상이 사라지지는 않게</b>.
        /// </para>
        /// </summary>
        public static void Face(RectTransform frame, string iconKey)
        {
            if (frame == null || string.IsNullOrEmpty(iconKey)) return;
            var mask = UiKit.FindAny(frame, "Bg_MainColor(Mask)", "Mask") ?? frame;
            UiKit.Hide(mask, "Character");
            var old = UiKit.Find(mask, FaceName);
            if (old != null) Object.Destroy(old.gameObject);
            var im = UiKit.Icon(mask, FaceName, iconKey);
            if (im != null) { im.preserveAspect = true; UiKit.Stretch(im.rectTransform); }
        }

        /// <summary>탑바·팝업 칸 안 초상 그림의 오브젝트 이름(자가 «HeroView 가 아니라 아이콘이다» 를 이 이름으로 잰다).</summary>
        public const string FaceName = "AvatarFace";

        /// <summary>
        /// 초상 칸 하나를 «프로필 프레임 조각 + 그 안의 초상» 으로 채운다(T262 ⓑ·3항) — 비었으면 세우고, 이미 서 있으면 <b>색이 바뀐 때만</b> 다시 세운다.
        /// <para>
        /// 주인이 «프레임 부분이 실제 프로필 프레임이랑 디자인이 다르네» 라고 짚은 뒤로 <b>아레나 23·도전 24·PvP 인게임 33·결과 34 가 전부 이 함수 하나</b>를 쓴다 —
        /// 초상 칸을 세우는 자리가 네 파일에 흩어져 있어서, 규칙을 글로 적어 두면 반드시 한 곳이 뒤처진다.
        /// </para>
        /// 조각을 매번 부수고 다시 세우지 않는 까닭은 값이 아니라 <b>깜빡임</b>이다(Refresh 는 화면이 뜬 채로 자주 돈다) — 색이 그대로면 <see cref="Face"/> 만 다시 불러 그림만 갈아 끼운다.
        /// </summary>
        public static void Frame(RectTransform cell, string frameKey, string iconKey)
        {
            if (cell == null || string.IsNullOrEmpty(frameKey)) return;
            var frame = UiKit.Find(cell, frameKey) as RectTransform;
            if (frame == null)
            {
                UiKit.Clear(cell);                                   // 색이 바뀌었다(또는 옛 조각이 서 있다) = 걷어 낸다
                var go = UiKit.Spawn(frameKey, cell);
                if (go == null) return;                              // 카탈로그 미스 — 조용히 빈 칸(빨간 줄 0)
                frame = (RectTransform)go.transform; UiKit.Stretch(frame);
                GearUi.DarkFrame(go.transform);   // T115 — 프로필 조각에는 ItemFrame 링이 없어 «HighLight 끄기» 만 남는다(결정 433)
            }
            Face(frame, iconKey);
        }

        /// <summary>
        /// 더미(아레나 상대)의 초상 아이콘 — <b>순위 하나가 언제나 같은 얼굴</b>을 갖게 하는 규칙(T262 3항).
        /// <para>
        /// ⚠ 종전에는 <b>줄 번호</b>로 골랐고, 두 목록의 줄 번호 셈이 달랐다: 도전 팝업은 <c>rank = i + 2</c>, 순위 목록은 <c>rank = i + 4</c> 인데
        /// 둘 다 아이콘은 <c>i % 4</c> 였다 — 그래서 <b>«도전자 4» 가 23 화면과 24 팝업에서 다른 얼굴</b>이었다(같은 화면에서 나란히 보이는데도).
        /// 이제 <b>순위로</b> 고르므로 시상대·순위 줄·도전 줄·PvP 머리·결과 화면이 전부 같은 얼굴을 낸다.
        /// </para>
        /// </summary>
        public static string DummyIcon(int rank)
        {
            int n = Icons.Length;
            return Icons[((rank % n) + n) % n];
        }

        /// <summary>아바타(초상) 고르기 팝업 — 탑바 아바타를 누르면 열린다.</summary>
        public static void OpenAvatar(App app)
        {
            if (app == null) return;
            var root = app.Overlay.OpenPrefab("ui.profileAvatar");
            var rt = (RectTransform)root.transform;
            var popup = UiKit.Find(rt, "Popup") as RectTransform;
            if (popup == null) return;                                   // 조각 구성이 바뀌면 조용히 빈 어둠(빨간 줄 0)
            Retitle(app, popup);

            string picked = CurrentIcon(app.Save);
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
                bool on = i < Icons.Length;
                rows[i].gameObject.SetActive(on);
                if (!on) continue;
                string icon = Icons[i];
                rows[i].name = RowPrefix + icon;
                // 칸 안 그림 = «테두리 조각 + 그 안에 초상 아이콘» — 팝업에서 보는 것이 곧 탑바에 서는 것(T262 ⓐ)
                var area = UiKit.Find(rows[i], "ProfileArea") as RectTransform;
                if (area != null)
                {
                    UiKit.Clear(area);
                    var piece = UiKit.Spawn(FrameKey(app.Save), area);
                    var prt = (RectTransform)piece.transform;
                    UiKit.FitScale(prt, area.rect.size);
                    Face(prt, icon);
                }
                var check = UiKit.Find(rows[i], "Check");
                if (check != null) { check.gameObject.SetActive(icon == picked); checks.Add(check); }
                string c2 = icon; int idx = i;
                UiKit.Clickable(rows[idx], () =>
                {
                    picked = c2;
                    for (int k = 0; k < checks.Count && k < Icons.Length; k++) checks[k].gameObject.SetActive(Icons[k] == picked);
                });
            }

            // «Choose» 버튼 = 고른 색을 저장하고 닫는다 · 닫기(X)는 그냥 닫는다
            var choose = ChooseButton(rt);
            if (choose != null)
            {
                choose.name = ChooseName;
                var label = choose.GetComponentInChildren<TMP_Text>(true);
                if (label != null) UiKit.SetText(label.transform, "", "선택", kind: TextKind.Button);
                UiKit.Clickable(choose.transform, () =>
                {
                    app.Save.ProfileIcon = picked;
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
            foreach (var t in popup.GetComponentsInChildren<TMP_Text>(true))
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
            // T207 ② — 조각의 입력칸을 부수고 uGUI InputField 로 다시 세우던 자리가 사라졌다(Adopt 가 TMP 를 그대로 둔다).
            //   그러니 여기서 찾는 것도 조각이 달고 온 `TMP_InputField` 다 — 안 바꾸면 이 팝업이 «입력칸 없음» 이 된다(CI #455).
            var input = rt.GetComponentInChildren<TMP_InputField>(true);
            TMP_Text count = null, desc = null, okLabel = null;
            foreach (var t in rt.GetComponentsInChildren<TMP_Text>(true))
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
        static void Tally(TMP_InputField input, TMP_Text count, Button ok)
        {
            string v = input != null ? input.text : "";
            int n = Nickname.Clean(v).Length;
            if (count != null) UiKit.SetText(count.transform, "", $"{n}/{Nickname.MaxLen}", kind: TextKind.Aux);
            if (ok != null) UiKit.SetInteractable(ok, n >= Nickname.MinLen);
        }
    }
}

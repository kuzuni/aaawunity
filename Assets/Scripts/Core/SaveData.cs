using System;
using System.Collections.Generic;
using System.Globalization;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 계정 저장 데이터 — index.html 의 세이브 v2(`kkoma-knight-v2`)와 같은 필드. 순수 C# (직렬화는 MiniJson).
    /// 저장 매체(PlayerPrefs·파일)는 게임 층이 준다. 정규화(<see cref="Normalize"/>)는 index.html 의 로드 보정과 같다.
    /// </summary>
    public sealed class SaveData
    {
        public const int Version = 2;
        public double Gold, Gem;
        public int MaxChapter = 1, SelChapter = 1;
        /// <summary>배경음 음소거(T28 — 옛 `muted` 를 여기로 이관 · JSON `muteBgm` · 없으면 `muted` 값) · 효과음 음소거(`muteSfx` · 없으면 false).</summary>
        public bool MuteBgm, MuteSfx;
        /// <summary>옛 이름(소리 전체 = BGM 스위치) — T28 이후 BGM 음소거의 별칭. 기존 호출·테스트 호환용.</summary>
        public bool Muted { get => MuteBgm; set => MuteBgm = value; }
        /// <summary>전투 배속(x1/x2) 기억 — T18. index.html `kkoma-knight-v2` 에 없는 필드라 «없으면 1». 표시·연출 배속이지 게임 수치가 아니다.</summary>
        public int Speed = SpeedMin;
        public const int SpeedMin = 1, SpeedMax = 2;
        public List<GearItem> Inv = new List<GearItem>();
        public Dictionary<string, int> Eq = new Dictionary<string, int>();        // 부위 → uid
        public Dictionary<string, int> Slots = new Dictionary<string, int>();     // 부위 → 슬롯 레벨
        public Dictionary<string, GachaState> GachaBoxes = new Dictionary<string, GachaState>();
        public int Pulls, Fuses, Uid = 1;
        /// <summary>
        /// 상점의 «하루 1번» 자리들 → <b>마지막으로 쓴 날</b>(<c>yyyy-MM-dd</c> · T259 4항 · 규칙은 <see cref="ShopFree"/>).
        /// 자리 이름은 <see cref="ShopFree.Gem"/>·<see cref="ShopFree.Gold"/>·<see cref="ShopFree.BoxRare"/>·<see cref="ShopFree.BoxLegend"/> 넷이다.
        /// <para>옛 세이브는 빈 표이고, 옛 필드 <c>freeDay</c> 가 있으면 <see cref="ShopFree.Gem"/> 자리로 옮겨 읽는다(지시서 4항 «예전 FreeDay 는 freeGem 으로»).</para>
        /// </summary>
        public Dictionary<string, string> FreeDays = new Dictionary<string, string>();
        /// <summary>
        /// 다이아 무료 보급을 마지막으로 받은 날 — <b>옛 이름이자 짧은 길</b>. 읽고 쓰면 <see cref="FreeDays"/> 의 <see cref="ShopFree.Gem"/> 칸이 그대로 움직인다.
        /// <para>
        /// T259 가 이 자리를 넷으로 쪼갤 때 이름을 지우지 않은 까닭 — 이 이름으로 세워 둔 자리(<c>ShopScreen</c>·자)가 여럿이고,
        /// 한꺼번에 갈아엎으면 «쪼갠 것» 과 «부르는 쪽을 옮긴 것» 둘이 한 회차에 섞여 무엇이 깨졌는지 못 가른다.
        /// 두 이름이 <b>같은 칸</b>을 보므로 갈라질 자리가 없다(<see cref="DailyGiftData.Milestone.Gem"/> 이 같은 규약이다).
        /// </para>
        /// </summary>
        public string FreeDay
        {
            get => FreeDays.TryGetValue(ShopFree.Gem, out var d) ? (d ?? "") : "";
            set => FreeDays[ShopFree.Gem] = value ?? "";
        }
        /// <summary>데일리 기프트(T77 · 주인 2026-09-07) — 누적이 살아 있는 날짜(<c>yyyy-MM-dd</c> · 비어 있으면 «아직 한 번도 안 열었다»).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 기본값»(옛 세이브 호환 · <see cref="Speed"/>·<see cref="FreeDay"/> 와 같은 방식).</summary>
        public string GiftDay = "";
        /// <summary>오늘 본 광고 누적 횟수(상한 = dailyGift.json 마지막 줄).</summary>
        public int GiftAds;
        /// <summary>오늘 «오늘의 선물» 무료 칸을 받았는가.</summary>
        public bool GiftFree;
        /// <summary>줄별 수령 여부(dailyGift.json milestones 순 · 길이는 <see cref="DailyGift.Roll"/> 이 표에 맞춘다).</summary>
        public List<bool> GiftClaimed = new List<bool>();
        /// <summary>프로필 아바타 테두리 색(T96-profile · 주인 2026-09-07 «상단 재화 바의 아바타를 누르면 프로필») —
        /// <c>ui.profileFrame.&lt;색&gt;</c>(ProfileFrame_02 다섯 변형)의 색 이름. 빈 값 = 기본(노랑 · 종전과 같은 조각).
        /// 이 레포 전용 필드라 «없으면 기본값»(옛 세이브 호환).</summary>
        public string ProfileColor = "";
        /// <summary>프로필 <b>초상 아이콘</b> 키(T262 ⓐ · 주인 2026-09-09 «플레이어 이미지 말고» — 더미 초상 아이콘에서 고른다).
        /// 비었거나 모르는 값이면 기본(<c>Profile.Icons[0]</c>)이라 옛 세이브도 그대로 읽힌다(세이브 버전 안 올림).</summary>
        public string ProfileIcon = "";
        /// <summary>플레이어 이름(T96-profile 2단계 · 주인 2026-09-07 «<c>Social_Profile_Nickname</c> 이거 좀 써라 프리팹들») —
        /// 규칙·기본값은 <see cref="Nickname"/> 한 곳이 갖는다. 빈 값 = 안 지었다(= <see cref="Nickname.Default"/>).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 기본값»(옛 세이브 호환 · <see cref="ProfileColor"/> 와 같은 방식).</summary>
        public string Nick = "";
        /// <summary>탐험(T97 · 주인 2026-09-07) — <b>마지막 정산 시각</b>(UTC 유닉스 초 · 0 이면 «아직 한 번도 안 열었다» → 여는 순간이 시작점).
        /// 쌓인 양을 저장하지 않고 이 시각 하나만 두므로 «켜 두든 꺼 두든»(오프라인) 같은 속도로 쌓인다(<see cref="Expedition"/>).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 기본값»(옛 세이브 호환 · <see cref="GiftDay"/> 와 같은 방식).</summary>
        public double ExpSettle;
        /// <summary>빠른 탐험 횟수가 살아 있는 날짜(<c>yyyy-MM-dd</c>) — <b>T265 로 쓰이지 않는다</b>(하루 제한 → 충전제).
        /// 지우지 않고 둔다: 옛 세이브에 이 값이 들어 있고, 되돌릴 때 그대로 쓰인다.</summary>
        public string ExpQuickDay = "";
        /// <summary>오늘 쓴 빠른 탐험 횟수 — <b>T265 로 쓰이지 않는다</b>(위와 같은 까닭).</summary>
        public int ExpQuickUsed;
        /// <summary>빠른 탐험 <b>보유 횟수</b>(T265 · 주인 «…에 한 번씩 3번» · <b>주기는 T270 에서 2시간</b> · 값은 <c>expedition.json</c> 이 든다). 상한 = expedition.json <c>quickMax</c>.
        /// 옛 세이브에는 없다 — 그때는 <see cref="ExpQuickAt"/> 가 0 이라 <see cref="Expedition.Roll"/> 이 «가득» 으로 시작시킨다(결정 기록 참조).</summary>
        public int ExpQuickCharge;
        /// <summary>빠른 탐험 <b>지금 차고 있는 몫의 기준 시각</b>(UTC 유닉스 초 · 0 이면 «아직 시작 안 함»).
        /// 날짜가 아니라 <b>시각</b>이라 앱을 꺼 둔 동안도 지난 시간만큼 찬다(오프라인 회복). 꽉 차 있으면 <see cref="Expedition.Roll"/> 이 «지금» 으로 붙들어
        /// 다 쓴 <b>그 순간부터</b> 한 주기(2시간)가 시작되게 한다(안 그러면 오래 꽉 차 있던 만큼이 한꺼번에 들어온다).</summary>
        public double ExpQuickAt;
        /// <summary>던전 티켓(T99 · 주인 2026-09-07) — 티켓·하루치 횟수가 살아 있는 날짜(<c>yyyy-MM-dd</c> · 바뀌면 <see cref="DungeonTickets.Roll"/> 이 보충한다).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 기본값»(옛 세이브 호환 · <see cref="GiftDay"/> 와 같은 방식).</summary>
        public string DunDay = "";
        /// <summary>던전 키(hell·expedition) → 보유 티켓 수.</summary>
        /// <summary>업적 <b>누적</b>(평생 · T258 · 카운터 이름 → 지금까지 몇). 받아도 줄지 않는다 — 일일·주간(퀘스트)과 달리 초기화가 없다.
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 기본값»(옛 세이브 호환).</summary>
        public Dictionary<string, int> Ach = new Dictionary<string, int>();
        /// <summary>업적 <b>받은 단계 수</b>(T258 · 카운터 이름 → 몇 단계까지 받았나). 지금 도전 중인 단계 = 이것 + 1.</summary>
        public Dictionary<string, int> AchClaimed = new Dictionary<string, int>();
        /// <summary>업적 중 <b>하루 한 번만</b> 오르는 것의 «마지막으로 센 날짜»(<c>yyyy-MM-dd</c> · 지금은 출석 하나 · 주인 명시).</summary>
        public Dictionary<string, string> AchDay = new Dictionary<string, string>();
        /// <summary>
        /// 가진 <b>장비 레시피</b>(T290 · 부위 → 개수 · 주인 2026-09-09 «부위마다 레시피가 있게 해»).
        /// 슬롯 강화가 <see cref="GearSystem.SlotUp"/> 에서 골드와 <b>같이</b> 뺀다 — 담는 자리는 여기 하나다(<see cref="Recipes"/> 가 규칙을 갖는다).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 빈 표»(옛 세이브 호환 · <see cref="DunFloor"/> 와 같은 꼴).
        /// </summary>
        public Dictionary<string, int> Recipes = new Dictionary<string, int>();
        public Dictionary<string, int> DunTickets = new Dictionary<string, int>();
        /// <summary>던전 키 → 오늘 쓴 «광고 보고 티켓» 횟수(상한 = dungeon.json <c>adPerDay</c>).</summary>
        public Dictionary<string, int> DunAdUsed = new Dictionary<string, int>();
        /// <summary>던전 키 → 오늘 쓴 «다이아로 티켓» 횟수(상한 = dungeon.json <c>gemPerDay</c>).</summary>
        public Dictionary<string, int> DunGemUsed = new Dictionary<string, int>();
        /// <summary>
        /// 던전 키 → <b>클리어한 가장 높은 층</b>(0 = 한 번도 못 깼다 · T228 · 주인 2026-09-08 07:4X «도전을 해서 클리어를 했었던 챕터만 소탕이 가능한 건데»).
        /// 소탕은 이 수 하나로 «되는가»(≥ 1)와 «무엇을 주는가»(그 층의 보상)가 정해진다 — <see cref="Dungeon"/> 의 <c>DungeonSweep</c> 이 규칙을 갖는다.
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 빈 목록»(옛 세이브 호환 · <see cref="DunDay"/> 와 같은 방식).
        /// </summary>
        public Dictionary<string, int> DunFloor = new Dictionary<string, int>();
        /// <summary>
        /// 가진 <b>펫알</b> 개수(T228 2단계 ⓐ · 주인 표 <c>dungeon.json</c> 의 소탕·클리어 보상이 «펫알 + 골드» 다).
        /// <para>
        /// <b>왜 세이브 필드인가</b> — 1단계에서 지급을 뗀 까닭이 «펫알을 담을 자리가 없다» 였다(결정 633). 자리는 여기 하나면 되고,
        /// 이것이 없으면 주인이 준 표의 <b>절반</b>(지옥의 문 펫알 5)이 매번 조용히 버려진다. <see cref="Gold"/>·<see cref="Gem"/> 과 같은 꼴(재화 = <c>double</c>)이다.
        /// </para>
        /// <b>쓰는 곳</b>(T293 ⓕ) — 펫 소환이 이것으로 뽑는다(<see cref="Pets.Offer"/> 가 «펫알이냐 다이아냐» 를 정하고
        /// <see cref="Pets.Draw"/> 가 여기서 뺀다). 등재 때(T228)는 «쌓기만» 했는데, 주인이 09:3X 에 소환 값을 주며 쓸 곳이 정해졌다.
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 0»(옛 세이브 호환).
        /// </summary>
        public double PetEgg;
        /// <summary>
        /// 가진 <b>펫</b>(T293 ⓕ · 펫 id → 레벨). <b>표에 있으면 = 가진 것</b>이고 첫 획득이 곧 Lv 1 이라, «가졌나» 를 따로 안 적는다.
        /// <para>규칙(무엇이 올라가고 무엇이 드는가)은 <see cref="Pets"/> 한 곳이 갖는다 — 여기는 <b>담는 자리</b>일 뿐이다.</para>
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 빈 표»(옛 세이브 호환 · <see cref="Ach"/> 와 같은 꼴).
        /// </summary>
        public Dictionary<string, int> PetLv = new Dictionary<string, int>();
        /// <summary>펫 id → <b>조각</b>(중복으로 쌓인 수 · 레벨업 재료 · T293). 레벨업이 <see cref="Pets.Need"/> 만큼 뺀다.</summary>
        public Dictionary<string, int> PetFrag = new Dictionary<string, int>();
        /// <summary>
        /// 장착 칸(0부터) → 펫 id · 빈 칸은 빈 글자(T293 5항). <b>길이는 표가 정한다</b>(<c>pet.json slots</c>) — 세이브에 칸 수를 안 박는다.
        /// <para>잠긴 칸·모르는 id 는 <b>읽는 쪽</b>(<see cref="Pets.Equipped"/>)이 거른다 — 세이브는 펫 표를 못 본다(<see cref="GameData"/> 에 아직 그 표가 없다).</para>
        /// </summary>
        public List<string> PetEq = new List<string>();
        /// <summary>
        /// <b>누적</b> 펫 뽑기 횟수(x10 은 10 으로 센다 · 주인 확정) — 장착 칸 해금(0·100·200회)이 이 수 하나로 정해진다(<see cref="Pets.SlotsOpen"/>).
        /// 받아도 줄지 않는다. index.html 세이브에 없는 이 레포 전용 필드라 «없으면 0»(옛 세이브 호환).
        /// </summary>
        public int PetPulls;
        /// <summary>
        /// 아레나 <b>승점</b>(🏆 · T240 · 주인 2026-09-08 11:2X «이기면 승점 올라가고 순위 올라가고 지면 승점 떨어지고»).
        /// <para>
        /// <b>순위와 티어는 저장하지 않는다</b> — 둘 다 이 수 하나에서 나온다(<see cref="ArenaMatch.RankOf"/>·<see cref="ArenaMatch.TierOf"/>).
        /// 따로 적어 두면 표를 고칠 때 세이브와 어긋날 자리가 생기는데, 어긋나도 화면에는 «그럴듯한 수» 로만 보여 아무도 못 본다.
        /// </para>
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 0»(옛 세이브 호환 · <see cref="PetEgg"/> 와 같은 방식) —
        /// 그리고 표의 <c>startScore</c> 가 0(바닥)이라 옛 세이브는 <b>새로 시작한 것과 같은 자리</b>에 놓인다(세이브 버전 그대로).
        /// </summary>
        public double ArenaScore;
        /// <summary>
        /// 가진 <b>아레나 코인</b>(상인 화폐 · T243). <see cref="PetEgg"/> 와 같은 까닭으로 자리를 만든다 —
        /// 순위 보상이 «아레나 코인» 을 줄 수 있는데 담을 곳이 없으면 <see cref="Mail"/> 이 그 우편을 아예 못 받는다(받아도 조용히 버려진다).
        /// ⚠ <b>쓰는 곳은 아직 없다</b> — 상인(26 · <c>arenaShop.json</c>) 구매 배선이 아직이라 여기서는 <b>쌓기만</b> 한다(§1 · 소비는 지어내지 않는다).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 0»(옛 세이브 호환).
        /// </summary>
        public double ArenaCoin;
        /// <summary>
        /// 가진 <b>부활권</b>(T254 · 주인 2026-09-09 «부활권 1개로 게임 1회 부활 가능하게 하기»).
        /// 쓰는 곳은 <see cref="Core.Revive"/> 한 곳이고 아이콘은 이미 있다(<c>ui.iconRevive</c> — 아레나 상인 표가 쓰는 그것 · 새 그림 0).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 0»(옛 세이브 호환 · <see cref="PetEgg"/> 와 같은 방식 · 세이브 버전 그대로).
        /// </summary>
        public int Revive;
        /// <summary>
        /// 가진 <b>뽑기 «키»</b> 3종(T255 · 주인 2026-09-09 «희귀 상자는 파란색 키로 1회 뽑기 가능 …»).
        /// 규칙(어느 키가 어느 상자를 여는가 · 이름 · 아이콘)은 <see cref="Core.GachaKeys"/> 한 곳이 갖는다 — 여기는 <b>담는 자리</b>일 뿐이다.
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 0»(옛 세이브 호환 · <see cref="Revive"/> 와 같은 방식 · 세이브 버전 그대로).
        /// </summary>
        public int KeyBlue, KeyPurple, KeyYellow;

        /// <summary>퀘스트 진행(T257 2단계) — <b>일일·주간은 따로 센다</b>(같은 사건을 세지만 지워지는 때가 달라서 한 표에 못 담는다).
        /// 열쇠는 <c>quest.json</c> 의 <c>counter</c> 이고, 표에 없는 이름이 들어와도 그냥 쌓이기만 한다(화면은 표에 있는 줄만 그린다).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 기본값»(옛 세이브 호환 · <see cref="GiftDay"/> 와 같은 방식).</summary>
        public Dictionary<string, int> QuestDaily = new Dictionary<string, int>();
        public Dictionary<string, int> QuestWeekly = new Dictionary<string, int>();
        /// <summary>그 셈이 살아 있는 날(<c>yyyy-MM-dd</c>)과 주(그 주 시작 날 · 같은 꼴) — 바뀌면 <see cref="QuestRun.Roll"/> 이 0 으로 민다.</summary>
        public string QuestDay = "", QuestWeek = "";
        /// <summary>트랙에서 <b>이미 받은 칸</b>(quest.json track 순 · 길이는 <see cref="QuestRun.Roll"/> 이 표에 맞춘다).</summary>
        public List<bool> QuestDailyGot = new List<bool>();
        public List<bool> QuestWeeklyGot = new List<bool>();
        /// <summary>주간 «로그인 5일» 이 <b>마지막으로 하루를 센 날</b> — 같은 날 여러 번 켜도 하루는 하루다(<see cref="QuestDay"/> 는 매일 지워지므로 이 값이 따로 있어야 한다).</summary>
        public string QuestLoginDay = "";
        /// <summary>
        /// 출석(16)에서 <b>받은 칸 수</b>(0~7 · T253) · 그 칸을 받은 <b>날짜</b>(<c>yyyy-MM-dd</c> · 비어 있으면 «아직 한 번도 안 받았다»).
        /// 규칙은 <see cref="Core.Attendance"/> 한 곳이 갖는다 — «며칠 연속인가» 가 아니라 <b>받은 칸 수</b>로 나아간다(주인이 «연속이 끊기면» 을 말한 적이 없다 · §1).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 0/빈 값»(옛 세이브 호환 · 세이브 버전 그대로).
        /// </summary>
        public int AttDone;
        /// <inheritdoc cref="AttDone"/>
        public string AttDay = "";
        /// <summary>특권(11) 카드를 <b>산 날</b>(카드 키 → <c>yyyy-MM-dd</c> · T264). <b>키가 있으면 «가졌다»</b> 이고 값은 기간 있는 카드(월간)가 쓴다.
        /// 공짜 카드(«데일리 기프트»)는 여기 안 들어간다 — 누구나 가진 것이라 <see cref="Core.Privilege.Owned"/> 가 언제나 참을 준다.
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 빈 표»(옛 세이브 호환 · 세이브 버전 그대로).</summary>
        public Dictionary<string, string> PrivBuy = new Dictionary<string, string>();
        /// <summary>특권 카드마다 <b>마지막으로 받은 날</b>(카드 키 → <c>yyyy-MM-dd</c> · T264) — 받기는 <b>카드마다 따로 하루 1회</b>다(주인 명시).</summary>
        public Dictionary<string, string> PrivDay = new Dictionary<string, string>();
        /// <summary>아레나 <b>최고 순위</b>(1 이 가장 높다 · <b>0 = 아직 한 판도 안 했다</b> · T240 5항). 승점과 달리 «되돌아가지 않는» 기록이라 따로 적는다.</summary>
        public int ArenaBest;
        /// <summary>아레나 <b>티켓</b>(T240 6항 · 결정 695) · 그 티켓이 살아 있는 날짜(<c>yyyy-MM-dd</c> · 바뀌면 <see cref="ArenaTickets.Roll"/> 이 채운다).
        /// 던전(<see cref="DunTickets"/>·<see cref="DunDay"/>)과 같은 꼴인데 아레나는 한 곳이라 <b>키가 없고 수 하나</b>다.
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 0/빈 값»(옛 세이브 호환 · 첫 접근에 그날치가 채워진다).</summary>
        public int ArenaTicket;
        /// <inheritdoc cref="ArenaTicket"/>
        public string ArenaDay = "";
        /// <summary>이미 받은 <b>챕터 보상</b>(Chapter Chest) — 챕터 → «받은 단» 비트(T137 · 주인 2026-09-07 «챕터 보상은 챕터당 3개» · 단 하나가 비트 하나).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 빈 목록»(옛 세이브 호환 · <see cref="DunDay"/> 와 같은 방식) — 세이브 버전은 그대로 둔다.
        /// T98 때의 «챕터 번호 목록» 세이브는 <see cref="ChapterChest.OldSaveAll"/> 로 읽어 두었다가 <see cref="ChapterChest.Normalize"/> 가 «단 다 받음» 으로 옮긴다.</summary>
        public Dictionary<int, int> ChestClaimed = new Dictionary<int, int>();
        /// <summary>챕터 → 그 챕터에서 잡아 본 <b>최고 처치 수</b>(T137 3항 · 챕터 보상 진행도) — <b>이기든 지든</b> <c>BattleScreen.EndRun</c> 한 곳에서 남는다.
        /// 이 레포 전용 필드라 «없으면 빈 목록»(옛 세이브 호환 · 세이브 버전 유지) — 이미 깬 챕터는 값이 없어도 «전멸» 로 친다(<see cref="ChapterChest.Progress"/>).</summary>
        public Dictionary<int, int> ChestKills = new Dictionary<int, int>();
        /// <summary>
        /// <b>우편함</b>에 든 것(T243 · 주인 2026-09-08 11:5X «우편함은 아레나 보상만») — 규칙·지급은 <see cref="Core.Mail"/> 한 곳이 갖는다.
        /// 나머지 보상(상점·탐험·광고·데일리 기프트·출석·챕터·던전)은 <b>받는 순간 즉시 지급</b>이라 여기 안 들어온다.
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 빈 목록»(옛 세이브 호환).
        /// </summary>
        public List<MailItem> Mail = new List<MailItem>();

        public static SaveData NewSave(GameData D)
        {
            var s = new SaveData();
            s.Normalize(D);
            return s;
        }

        public GearItem InvById(int u) { foreach (var g in Inv) if (g.Uid == u) return g; return null; }
        public GearItem EquippedGear(string part) => Eq.TryGetValue(part, out var u) ? InvById(u) : null;
        public bool IsEquipped(GearItem g) => Eq.TryGetValue(g.Part, out var u) && u == g.Uid;
        public int SlotLv(string part) => Slots.TryGetValue(part, out var l) ? l : 0;
        public GearItem NewGear(string part, string type, int rar, int plus) => new GearItem { Uid = Uid++, Part = part, Type = type, Rar = rar, Plus = plus };

        public Build CurBuild(GameData D)
        {
            var b = new Build();
            foreach (var pt in D.Gear.Parts) { b.Eq[pt] = EquippedGear(pt); b.Slots[pt] = SlotLv(pt); }
            return b;
        }

        public HashSet<GearItem> EquippedSet(GameData D)
        {
            var set = new HashSet<GearItem>();
            foreach (var pt in D.Gear.Parts) { var g = EquippedGear(pt); if (g != null) set.Add(g); }
            return set;
        }

        /// <summary>index.html 의 로드 보정 — 상한 클램프 · 인벤 검증 · 전설 +3 이상 → 신화 0강 · uid 유일성 · 상자별 피티 카운터.</summary>
        public void Normalize(GameData D)
        {
            MaxChapter = Math.Max(1, Math.Min(MaxChapter, D.Tune.MaxChapter));
            SelChapter = Math.Max(1, Math.Min(SelChapter, MaxChapter));
            Gold = Math.Max(0, Gold); Gem = Math.Max(0, Gem);
            Speed = Math.Max(SpeedMin, Math.Min(Speed, SpeedMax));
            GiftAds = Math.Max(0, GiftAds); if (GiftClaimed == null) GiftClaimed = new List<bool>();   // 표 길이 보정은 DailyGift.Roll (표를 여기서 모른다)
            if (ChestClaimed == null) ChestClaimed = new Dictionary<int, int>(); ChapterChest.Normalize(this, D);
            if (ExpSettle < 0) ExpSettle = 0; ExpQuickUsed = Math.Max(0, ExpQuickUsed);   // 빠른 탐험 상한은 Expedition.Roll (표를 여기서 모른다) · 시계 되돌림도 거기서
            ExpQuickCharge = Math.Max(0, ExpQuickCharge); if (ExpQuickAt < 0) ExpQuickAt = 0;   // 상한(quickMax)도 Roll 이 안다(T265)
            PetPulls = Math.Max(0, PetPulls); Pets.NormalizeSave(this);   // T293 — 펫 «표» 는 여기서 모른다(GameData 가 아직 안 든다) · 표가 필요한 정리는 Pets.Equipped 가 한다
            Inv.RemoveAll(g => g == null || Array.IndexOf(D.Gear.Parts, g.Part) < 0 || !D.Gear.Options.ContainsKey(g.Type) || g.Rar < 0 || g.Rar >= D.Gear.RarName.Length);
            foreach (var g in Inv) { g.Plus = Math.Max(0, g.Plus); if (g.Rar == D.Gear.RarLegend && g.Plus >= D.Gear.LegendToMythPlus) { g.Rar = D.Gear.RarMyth; g.Plus = 0; } }
            Uid = Math.Max(1, Uid);
            // 이름은 다듬어 두고, 규칙에 못 미치면 «안 지었다»(빈 값 = 기본 이름)로 되돌린다 — 화면은 Nickname.Of 만 본다
            Nick = Nickname.Clean(Nick);
            if (Nick.Length < Nickname.MinLen) Nick = "";
            var seen = new HashSet<int>();
            foreach (var g in Inv) { if (g.Uid <= 0) continue; if (seen.Contains(g.Uid)) g.Uid = 0; else seen.Add(g.Uid); }
            foreach (var u in seen) if (u >= Uid) Uid = u + 1;
            foreach (var g in Inv) if (g.Uid <= 0) g.Uid = Uid++;
            foreach (var pt in D.Gear.Parts) Slots[pt] = Math.Max(0, Math.Min(SlotLv(pt), D.Gear.SlotLvMax));
            var badEq = new List<string>();
            foreach (var kv in Eq) { var g = InvById(kv.Value); if (g == null || g.Part != kv.Key) badEq.Add(kv.Key); }
            foreach (var k in badEq) Eq.Remove(k);
            foreach (var b in D.Gacha.Boxes)
            {
                if (!GachaBoxes.TryGetValue(b.Key, out var st) || st == null) GachaBoxes[b.Key] = new GachaState();
                else { st.P50 = Math.Max(0, st.P50); st.P10 = Math.Max(0, st.P10); st.Pulls = Math.Max(0, st.Pulls); st.PRare = Math.Max(0, st.PRare); }
            }
        }

        public string ToJson()
        {
            var o = new Dictionary<string, object>
            {
                ["v"] = (double)Version, ["gold"] = Gold, ["gem"] = Gem, ["maxChapter"] = (double)MaxChapter, ["selChapter"] = (double)SelChapter,
                ["muted"] = MuteBgm, ["muteBgm"] = MuteBgm, ["muteSfx"] = MuteSfx, ["speed"] = (double)Speed, ["pulls"] = (double)Pulls, ["fuses"] = (double)Fuses, ["uid"] = (double)Uid, ["freeDay"] = FreeDay ?? "",
                ["giftDay"] = GiftDay ?? "", ["giftAds"] = (double)GiftAds, ["giftFree"] = GiftFree,
                ["expSettle"] = ExpSettle, ["expQuickDay"] = ExpQuickDay ?? "", ["expQuickUsed"] = (double)ExpQuickUsed,
                ["expQuickCharge"] = (double)ExpQuickCharge, ["expQuickAt"] = ExpQuickAt,   // T265 충전제
                ["profileColor"] = ProfileColor ?? "", ["profileIcon"] = ProfileIcon ?? "", ["nick"] = Nick ?? "",   // profileIcon = T262 ⓐ
            };
            var gc = new List<object>(); foreach (var b in GiftClaimed) gc.Add(b); o["giftClaimed"] = gc;
            o["dunDay"] = DunDay ?? "";
            var inv = new List<object>();
            foreach (var g in Inv)
            {
                var gi = new Dictionary<string, object> { ["u"] = (double)g.Uid, ["part"] = g.Part, ["type"] = g.Type, ["rar"] = (double)g.Rar, ["plus"] = (double)g.Plus };
                if (g.IsNew) gi["nw"] = 1.0;
                inv.Add(gi);
            }
            o["inv"] = inv;
            var eq = new Dictionary<string, object>(); foreach (var kv in Eq) eq[kv.Key] = (double)kv.Value; o["eq"] = eq;
            var sl = new Dictionary<string, object>(); foreach (var kv in Slots) sl[kv.Key] = (double)kv.Value; o["slots"] = sl;
            var df = new Dictionary<string, object>(); foreach (var kv in DunFloor) df[kv.Key] = (double)kv.Value; o["dunFloor"] = df;
            o["petEgg"] = PetEgg;
            var pl = new Dictionary<string, object>(); foreach (var kv in PetLv) pl[kv.Key] = (double)kv.Value; o["petLv"] = pl;          // T293
            var pf = new Dictionary<string, object>(); foreach (var kv in PetFrag) pf[kv.Key] = (double)kv.Value; o["petFrag"] = pf;     // T293
            var pe = new List<object>(); foreach (var id in PetEq) pe.Add(id ?? ""); o["petEq"] = pe;                                    // T293 — 칸 순서 그대로
            o["petPulls"] = (double)PetPulls;                                                                                            // T293
            o["arenaScore"] = ArenaScore; o["arenaBest"] = (double)ArenaBest;   // T240
            o["arenaTicket"] = (double)ArenaTicket; o["arenaDay"] = ArenaDay ?? "";   // T240 6항
            o["revive"] = (double)Revive;   // T254
            o["keyBlue"] = (double)KeyBlue; o["keyPurple"] = (double)KeyPurple; o["keyYellow"] = (double)KeyYellow;   // T255
            o["arenaCoin"] = ArenaCoin;   // T243
            var qd = new Dictionary<string, object>(); foreach (var kv in QuestDaily) qd[kv.Key] = (double)kv.Value; o["questDaily"] = qd;              // T257
            var qw = new Dictionary<string, object>(); foreach (var kv in QuestWeekly) qw[kv.Key] = (double)kv.Value; o["questWeekly"] = qw;            // T257
            o["questDay"] = QuestDay ?? ""; o["questWeek"] = QuestWeek ?? ""; o["questLoginDay"] = QuestLoginDay ?? "";                                  // T257
            var qdg = new List<object>(); foreach (var b in QuestDailyGot) qdg.Add(b); o["questDailyGot"] = qdg;                                        // T257
            var qwg = new List<object>(); foreach (var b in QuestWeeklyGot) qwg.Add(b); o["questWeeklyGot"] = qwg;                                      // T257
            o["attDone"] = (double)AttDone; o["attDay"] = AttDay ?? "";   // T253
            var pb = new Dictionary<string, object>(); foreach (var kv in PrivBuy) pb[kv.Key] = kv.Value ?? ""; o["privBuy"] = pb;   // T264
            var pd = new Dictionary<string, object>(); foreach (var kv in PrivDay) pd[kv.Key] = kv.Value ?? ""; o["privDay"] = pd;   // T264
            var ml = new List<object>();
            foreach (var m in Mail)
            {
                var rw = new List<object>();
                foreach (var r in m.Rewards) rw.Add(new Dictionary<string, object> { ["item"] = r.Item, ["amount"] = r.Amount });
                ml.Add(new Dictionary<string, object> { ["id"] = m.Id, ["kind"] = m.Kind, ["title"] = m.Title, ["desc"] = m.Desc, ["rewards"] = rw });
            }
            o["mail"] = ml;
            var ac = new Dictionary<string, object>(); foreach (var kv in Ach) ac[kv.Key] = (double)kv.Value; o["ach"] = ac;                 // T258
            var acc = new Dictionary<string, object>(); foreach (var kv in AchClaimed) acc[kv.Key] = (double)kv.Value; o["achClaimed"] = acc;
            var acd = new Dictionary<string, object>(); foreach (var kv in AchDay) acd[kv.Key] = kv.Value ?? ""; o["achDay"] = acd;
            var rc = new Dictionary<string, object>(); foreach (var kv in Recipes) rc[kv.Key] = (double)kv.Value; o["recipes"] = rc;   // T290
            // T259 — 상점 «하루 1번» 자리 넷의 날짜 도장. 옛 키 `freeDay` 도 위에서 그대로 적는다(다이아 칸의 짧은 길) —
            //        옛 판으로 되돌아가도 다이아 보급만은 오늘 몫을 지킨다. 두 이름이 같은 칸을 보므로 갈라질 자리가 없다.
            var fd = new Dictionary<string, object>(); foreach (var kv in FreeDays) fd[kv.Key] = kv.Value ?? ""; o["freeDays"] = fd;
            var dt = new Dictionary<string, object>(); foreach (var kv in DunTickets) dt[kv.Key] = (double)kv.Value; o["dunTickets"] = dt;
            var da = new Dictionary<string, object>(); foreach (var kv in DunAdUsed) da[kv.Key] = (double)kv.Value; o["dunAdUsed"] = da;
            var dgm = new Dictionary<string, object>(); foreach (var kv in DunGemUsed) dgm[kv.Key] = (double)kv.Value; o["dunGemUsed"] = dgm;
            // T137 — «챕터 → 받은 단 비트» 라 목록이 아니라 표로 적는다(옛 세이브의 목록 꼴도 읽는다 · FromJson)
            var cc = new Dictionary<string, object>(); foreach (var kv in ChestClaimed) cc[kv.Key.ToString(CultureInfo.InvariantCulture)] = (double)kv.Value; o["chestClaimed"] = cc;
            var ck = new Dictionary<string, object>(); foreach (var kv in ChestKills) ck[kv.Key.ToString(CultureInfo.InvariantCulture)] = (double)kv.Value; o["chestKills"] = ck;
            var gb = new Dictionary<string, object>();
            // T261 — `pRare`(희귀 확정 천장 카운터)를 같이 적는다. 옛 세이브에는 이 키가 없으니 읽을 때 0 이 되고,
            //  0 이면 «막 리셋된 것» 과 같아 손해가 없다(천장이 늦게 오지 빨리 오지 않는다).
            foreach (var kv in GachaBoxes) gb[kv.Key] = new Dictionary<string, object> { ["p50"] = (double)kv.Value.P50, ["p10"] = (double)kv.Value.P10, ["pulls"] = (double)kv.Value.Pulls, ["pRare"] = (double)kv.Value.PRare };
            o["gachaBoxes"] = gb;
            return MiniJson.Serialize(o);
        }

        /// <summary>
        /// 상점 «하루 1번» 날짜 도장을 읽는다 — <b>새 표가 먼저, 옛 필드는 채워지지 않은 자리에만</b>(T259 4항 마이그레이션).
        /// <para>
        /// 순서가 중요하다. 옛 필드(<c>freeDay</c>)를 나중에 넣으면 <b>이미 읽은 다이아 칸을 덮는다</b> —
        /// 새 판에서 오늘 받은 뒤 옛 판으로 한 번 갔다 온 세이브는 두 값이 다를 수 있고, 그때 옛 값이 이기면 <b>오늘 몫이 되살아난다</b>.
        /// «먼저 읽은 쪽이 이긴다» 로 두면 새 표가 없는 옛 세이브만 옛 필드로 채워진다.
        /// </para>
        /// <para>빈 문자열은 «안 받았다» 와 같은 뜻이라 굳이 표에 적지 않는다 — 적으면 왕복할 때마다 빈 칸이 늘어난다.</para>
        /// </summary>
        void ReadFreeDays(JNode j)
        {
            foreach (var k in j["freeDays"].Keys)
            {
                var v = j["freeDays"][k].Str("");
                if (!string.IsNullOrEmpty(v)) FreeDays[k] = v;
            }
            var old = j["freeDay"].Str("");
            if (!string.IsNullOrEmpty(old) && !FreeDays.ContainsKey(ShopFree.Gem)) FreeDays[ShopFree.Gem] = old;
        }

        public static SaveData FromJson(string json, GameData D)
        {
            var s = new SaveData();
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var j = new JNode(MiniJson.Parse(json));
                    s.Gold = j["gold"].Num(); s.Gem = j["gem"].Num(); s.MaxChapter = j["maxChapter"].Int(1); s.SelChapter = j["selChapter"].Int(1);
                    s.MuteBgm = j.Has("muteBgm") ? j["muteBgm"].Bool() : j["muted"].Bool(); s.MuteSfx = j["muteSfx"].Bool(); s.Speed = j["speed"].Int(SpeedMin); s.Pulls = j["pulls"].Int(); s.Fuses = j["fuses"].Int(); s.Uid = j["uid"].Int(1); s.ReadFreeDays(j);
                    s.GiftDay = j["giftDay"].Str(""); s.GiftAds = j["giftAds"].Int(); s.GiftFree = j["giftFree"].Bool();
                    s.ExpSettle = j["expSettle"].Num(); s.ExpQuickDay = j["expQuickDay"].Str(""); s.ExpQuickUsed = j["expQuickUsed"].Int();
                    s.ExpQuickCharge = j["expQuickCharge"].Int(); s.ExpQuickAt = j["expQuickAt"].Num();   // 없으면 0 — Roll 이 «가득» 으로 시작시킨다(T265)
                    s.ProfileColor = j["profileColor"].Str(""); s.ProfileIcon = j["profileIcon"].Str(""); s.Nick = j["nick"].Str("");   // 없으면 빈 값 = 기본 초상(옛 세이브 호환 · T262 ⓐ)
                    foreach (var c in j["giftClaimed"].Items()) s.GiftClaimed.Add(c.Bool());
                    foreach (var g in j["inv"].Items())
                        s.Inv.Add(new GearItem { Uid = g["u"].Int(), Part = g["part"].Str(), Type = g["type"].Str(), Rar = g["rar"].Int(), Plus = g["plus"].Int(), IsNew = g["nw"].Num() != 0 });
                    foreach (var k in j["eq"].Keys) s.Eq[k] = j["eq"][k].Int();
                    foreach (var k in j["slots"].Keys) s.Slots[k] = j["slots"][k].Int();
                    s.DunDay = j["dunDay"].Str("");
                    foreach (var k in j["ach"].Keys) s.Ach[k] = j["ach"][k].Int();                             // T258 — 없으면 빈 표(옛 세이브)
                    foreach (var k in j["achClaimed"].Keys) s.AchClaimed[k] = j["achClaimed"][k].Int();
                    foreach (var k in j["achDay"].Keys) s.AchDay[k] = j["achDay"][k].Str("");
                    foreach (var k in j["recipes"].Keys) s.Recipes[k] = j["recipes"][k].Int();                 // T290 — 없으면 빈 표(옛 세이브)
                    foreach (var k in j["dunTickets"].Keys) s.DunTickets[k] = j["dunTickets"][k].Int();
                    foreach (var k in j["dunFloor"].Keys) s.DunFloor[k] = j["dunFloor"][k].Int();
                    s.PetEgg = j["petEgg"].Num();   // 없으면 0(옛 세이브 호환 · T228)
                    foreach (var k in j["petLv"].Keys) s.PetLv[k] = j["petLv"][k].Int();           // T293 — 없으면 빈 표(옛 세이브)
                    foreach (var k in j["petFrag"].Keys) s.PetFrag[k] = j["petFrag"][k].Int();     // T293
                    foreach (var e in j["petEq"].Items()) s.PetEq.Add(e.Str(""));                  // T293 — 빈 글자 = 빈 칸
                    s.PetPulls = j["petPulls"].Int();                                              // T293
                    s.ArenaScore = j["arenaScore"].Num(); s.ArenaBest = j["arenaBest"].Int();   // 없으면 0 = «아직 한 판도 안 했다»(옛 세이브 호환 · T240)
                    s.ArenaTicket = j["arenaTicket"].Int(); s.ArenaDay = j["arenaDay"].Str("");   // 없으면 0/빈 값 → 첫 접근에 그날치가 채워진다(T240 6항)
                    s.Revive = j["revive"].Int();   // 없으면 0(옛 세이브 호환 · T254)
                    s.KeyBlue = j["keyBlue"].Int(); s.KeyPurple = j["keyPurple"].Int(); s.KeyYellow = j["keyYellow"].Int();   // 없으면 0(옛 세이브 호환 · T255)
                    s.ArenaCoin = j["arenaCoin"].Num();   // 없으면 0(옛 세이브 호환 · T243)
                    foreach (var k in j["questDaily"].Keys) s.QuestDaily[k] = j["questDaily"][k].Int();       // 없으면 빈 표(옛 세이브 호환 · T257)
                    foreach (var k in j["questWeekly"].Keys) s.QuestWeekly[k] = j["questWeekly"][k].Int();    // T257
                    s.QuestDay = j["questDay"].Str(""); s.QuestWeek = j["questWeek"].Str(""); s.QuestLoginDay = j["questLoginDay"].Str("");   // T257
                    foreach (var b in j["questDailyGot"].Items()) s.QuestDailyGot.Add(b.Bool());              // T257
                    foreach (var b in j["questWeeklyGot"].Items()) s.QuestWeeklyGot.Add(b.Bool());            // T257
                    s.AttDone = j["attDone"].Int(); s.AttDay = j["attDay"].Str("");   // 없으면 0/빈 값(옛 세이브 호환 · T253)
                    foreach (var k in j["privBuy"].Keys) s.PrivBuy[k] = j["privBuy"][k].Str("");   // 없으면 빈 표(옛 세이브 호환 · T264)
                    foreach (var k in j["privDay"].Keys) s.PrivDay[k] = j["privDay"][k].Str("");   // T264
                    foreach (var m in j["mail"].Items())
                    {
                        var mi = new MailItem { Id = m["id"].Str(""), Kind = m["kind"].Str(KkomaKnight.Core.Mail.KindArena), Title = m["title"].Str(""), Desc = m["desc"].Str("") };
                        foreach (var r in m["rewards"].Items()) mi.Rewards.Add(new ArenaRankData.Reward { Item = r["item"].Str(""), Amount = r["amount"].Num() });
                        s.Mail.Add(mi);
                    }
                    foreach (var k in j["dunAdUsed"].Keys) s.DunAdUsed[k] = j["dunAdUsed"][k].Int();
                    foreach (var k in j["dunGemUsed"].Keys) s.DunGemUsed[k] = j["dunGemUsed"][k].Int();
                    // T137 — 새 세이브는 «챕터 → 비트» 표, T98 옛 세이브는 «챕터 번호 목록»(그 챕터는 «단 다 받음» = ChapterChest.Normalize 가 옮긴다)
                    var cc = j["chestClaimed"];
                    if (cc.IsArray) foreach (var c in cc.Items()) s.ChestClaimed[c.Int()] = ChapterChest.OldSaveAll;
                    else foreach (var k in cc.Keys) if (int.TryParse(k, NumberStyles.Integer, CultureInfo.InvariantCulture, out var c)) s.ChestClaimed[c] = cc[k].Int();
                    foreach (var k in j["chestKills"].Keys) if (int.TryParse(k, NumberStyles.Integer, CultureInfo.InvariantCulture, out var c)) s.ChestKills[c] = j["chestKills"][k].Int();
                    foreach (var k in j["gachaBoxes"].Keys) s.GachaBoxes[k] = new GachaState { P50 = j["gachaBoxes"][k]["p50"].Int(), P10 = j["gachaBoxes"][k]["p10"].Int(), Pulls = j["gachaBoxes"][k]["pulls"].Int(), PRare = j["gachaBoxes"][k]["pRare"].Int() };
                }
                catch (Exception) { s = new SaveData(); }
            }
            s.Normalize(D);
            return s;
        }
    }
}

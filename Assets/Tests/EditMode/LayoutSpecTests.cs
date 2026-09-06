using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// docs/ref-layout.md(aaaw docs/ui/ref-layout.md 사본)의 ①~⑦ 표 행 ↔ <see cref="Layout"/> 상수 대조.
    /// 표는 «자» 라 코드 상수가 표에서 어긋나면 빨개진다(전사 오류 방지 · T5 게이트). 허용 오차 0.05 (표 그대로).
    /// </summary>
    public class LayoutSpecTests
    {
        static string SpecPath()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                var cand = Path.Combine(dir.FullName, "docs", "ref-layout.md");
                if (File.Exists(cand)) return cand;
            }
            throw new FileNotFoundException("docs/ref-layout.md 를 찾을 수 없다 (cwd=" + Directory.GetCurrentDirectory() + ")");
        }

        /// <summary>섹션(①~⑦) → 요소 이름 → (x,y,w,h · null = «—»).</summary>
        static Dictionary<string, Dictionary<string, float?[]>> Parse()
        {
            var res = new Dictionary<string, Dictionary<string, float?[]>>();
            string sec = null;
            foreach (var raw in File.ReadAllLines(SpecPath()))
            {
                var line = raw.Trim();
                var h = Regex.Match(line, @"^## ([①-⑳㉑-㉟])");
                if (h.Success) { sec = h.Groups[1].Value; res[sec] = new Dictionary<string, float?[]>(); continue; }
                if (line.StartsWith("## ")) { sec = null; continue; }   // 정정 절 이하는 표가 아니다
                if (sec == null || !line.StartsWith("|")) continue;
                var cells = new List<string>(); foreach (var c in line.Trim('|').Split('|')) cells.Add(c.Trim());
                if (cells.Count < 5 || cells[0] == "요소" || cells[0].StartsWith("---")) continue;
                var name = Regex.Replace(cells[0], @"\*\*|`|\s*\(T\d+\)", "").Trim();
                var v = new float?[4];
                for (int i = 0; i < 4; i++)
                {
                    var t = Regex.Replace(cells[i + 1], @"\*\*|`", "").Trim();
                    v[i] = float.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : (float?)null;
                }
                res[sec][name] = v;
            }
            return res;
        }

        static void Same(Dictionary<string, Dictionary<string, float?[]>> spec, string sec, string name, Layout.R r)
        {
            Assert.That(spec.ContainsKey(sec), Is.True, "섹션 없음 " + sec);
            Assert.That(spec[sec].ContainsKey(name), Is.True, $"{sec} 표에 행 없음: {name}");
            var v = spec[sec][name]; var got = new[] { r.X, r.Y, r.W, r.H }; var lbl = new[] { "x", "y", "w", "h" };
            for (int i = 0; i < 4; i++) if (v[i].HasValue) Assert.That(got[i], Is.EqualTo(v[i].Value).Within(0.05f), $"{sec} «{name}» {lbl[i]}");
        }
        static void SameV(Dictionary<string, Dictionary<string, float?[]>> spec, string sec, string name, int idx, float value)
        {
            Assert.That(spec[sec].ContainsKey(name), Is.True, $"{sec} 표에 행 없음: {name}");
            var v = spec[sec][name]; Assert.That(v[idx].HasValue, Is.True, $"{sec} «{name}» 칸 {idx} 비어 있음");
            Assert.That(value, Is.EqualTo(v[idx].Value).Within(0.05f), $"{sec} «{name}»");
        }

        [Test]
        public void Lobby_MatchesSpec()
        {
            var s = Parse();
            Same(s, "①", "상단 바(아바타+재화 줄 전체)", Layout.LobbyTopBar); Same(s, "①", "아바타(정사각)", Layout.LobbyAvatar); Same(s, "①", "재화 pill 줄", Layout.LobbyPills);
            Same(s, "①", "메뉴(☰) 버튼", Layout.LobbyMenu);
            // T78 — 이벤트 배너·성 버튼 행은 표 ① 에서 삭제 · T96-menu — 사이드 기둥 둘(특권 · 출석/데일리/퀘스트)도 메뉴로 옮겨 표에서 뺐다
            Same(s, "①", "챕터 제목", Layout.LobbyChapTitle); Same(s, "①", "챕터 밑줄·선택 화살표", Layout.LobbyChapUnderline); Same(s, "①", "챕터 카드(스테이지 그림)", Layout.LobbyCard);
            Same(s, "①", "좌 화살표", Layout.LobbyArrowL); Same(s, "①", "우 화살표", Layout.LobbyArrowR); Same(s, "①", "보조 버튼 2개 줄", Layout.LobbySubRow);
            Same(s, "①", "START 버튼", Layout.LobbyStart); Same(s, "①", "하단 탭바", Layout.TabBar);
        }
        [Test]
        public void Hud_MatchesSpec()
        {
            var s = Parse();
            Same(s, "②", "상단 HUD pill 2개", Layout.HudPills); Same(s, "②", "메뉴(☰) 버튼", Layout.HudMenu); Same(s, "②", "챕터 제목", Layout.HudChapTitle); Same(s, "②", "진행 바", Layout.HudProgress);
            Same(s, "②", "지면(길) 띠", Layout.GroundBand);
            SameV(s, "②", "플레이어 발밑 y", 1, Layout.PlayerFootY); SameV(s, "②", "적 행 y", 1, Layout.EnemyTopY);
            SameV(s, "②", "플레이어 높이", 1, Layout.PlayerTopY); SameV(s, "②", "플레이어 높이", 3, Layout.PlayerHeight);
            SameV(s, "②", "적 높이", 1, Layout.EnemyTopY); SameV(s, "②", "적 높이", 3, Layout.EnemyHeight);
            SameV(s, "②", "체력 라벨 줄", 1, Layout.HpLabelY); SameV(s, "②", "체력 라벨 줄", 3, Layout.HpLabelH);
            SameV(s, "②", "플레이어 중심 x", 0, Layout.PlayerCenterX); SameV(s, "②", "플레이어 발밑 바 폭", 2, Layout.PlayerFootBarW); SameV(s, "②", "적 발밑 바 폭", 2, Layout.EnemyFootBarW);
            Same(s, "②", "배속 버튼", Layout.HudSpeed); Same(s, "②", "우하단 원형 버튼", Layout.HudRound); Same(s, "②", "하단 패널", Layout.HudPanel);
            Same(s, "②", "EXP 바", Layout.HudExp); Same(s, "②", "HP 바", Layout.HudHp); Same(s, "②", "실드 바", Layout.HudSh);
            Same(s, "②", "스탯 그리드", Layout.HudStats); Same(s, "②", "스탯칸(1칸)", Layout.HudStatCell); Same(s, "②", "인포(책) 버튼", Layout.HudInfo);
        }
        [Test]
        public void Gear_MatchesSpec()
        {
            var s = Parse();
            Same(s, "③", "상단 바", Layout.LobbyTopBar); Same(s, "③", "장비 무대(캐릭터+슬롯)", Layout.GearStage);
            Same(s, "③", "좌 슬롯열(3칸)", Layout.GearSlotColL); Same(s, "③", "우 슬롯열(3칸)", Layout.GearSlotColR); Same(s, "③", "슬롯 1칸", Layout.GearSlot);
            Same(s, "③", "캐릭터", Layout.GearHero); Same(s, "③", "스탯 요약줄(3칸)", Layout.GearStats); Same(s, "③", "액션바(Forge)", Layout.GearForgeBtn);
            Same(s, "③", "인벤 그리드", Layout.GearInv); Same(s, "③", "인벤 1칸", Layout.GearInvCell); Same(s, "③", "하단 탭바", Layout.TabBar);
        }
        [Test]
        public void GearDetail_MatchesSpec()
        {
            var s = Parse();
            Same(s, "④", "팝업 박스", Layout.GdBox); Same(s, "④", "등급 배지", Layout.GdBadge); Same(s, "④", "아이템 아이콘(정사각)", Layout.GdIcon);
            Same(s, "④", "이름줄", Layout.GdName); Same(s, "④", "메타줄(레벨·부위)", Layout.GdMeta); Same(s, "④", "스탯 섹션", Layout.GdStats);
            Same(s, "④", "옵션 목록", Layout.GdOpts); Same(s, "④", "비용줄", Layout.GdCost); Same(s, "④", "버튼 2개", Layout.GdBtns);
            SameV(s, "④", "닫기 안내", 1, Layout.GdClose.Y); SameV(s, "④", "닫기 안내", 3, Layout.GdClose.H);
        }
        [Test]
        public void Shop_MatchesSpec()
        {
            var s = Parse();
            Same(s, "⑤", "상단 바", Layout.LobbyTopBar); Same(s, "⑤", "광고/무료 카드 2개", Layout.ShopFreeRow);
            SameV(s, "⑤", "섹션 헤더", 1, Layout.ShopSec1.Y); SameV(s, "⑤", "섹션 헤더", 3, Layout.ShopSec1.H);
            Same(s, "⑤", "상품 카드(1칸)", Layout.ShopCard1); Same(s, "⑤", "상품 카드 2행", new Layout.R(Layout.ShopCardRow2.X, Layout.ShopCardRow2.Y, Layout.ShopCardW, Layout.ShopCardRow2.H));
            SameV(s, "⑤", "두 번째 섹션 헤더", 1, Layout.ShopSec2.Y);
            Same(s, "⑤", "두 번째 섹션 카드행", new Layout.R(Layout.ShopCardRow3.X, Layout.ShopCardRow3.Y, Layout.ShopCardW, Layout.ShopCardRow3.H));
            Same(s, "⑤", "하단 탭바", Layout.TabBar);
        }
        [Test]
        public void Forge_MatchesSpec()
        {
            var s = Parse();
            Same(s, "⑥", "대장간 무대", Layout.ForgeStage); Same(s, "⑥", "결과 슬롯", Layout.ForgeResult); Same(s, "⑥", "화살표", Layout.ForgeArrow);
            Same(s, "⑥", "재료 슬롯", Layout.ForgeMat); Same(s, "⑥", "안내 문구", Layout.ForgeBanner); Same(s, "⑥", "액션바", Layout.ForgeActionBar);
            Same(s, "⑥", "자동 버튼", Layout.ForgeAuto); Same(s, "⑥", "합성 버튼", Layout.ForgeFuse); Same(s, "⑥", "인벤 그리드", Layout.ForgeInv); Same(s, "⑥", "뒤로 버튼", Layout.ForgeBack);
        }
        [Test]
        public void Perks_MatchesSpec()
        {
            var s = Parse();
            Same(s, "⑦", "상단 스탯 줄(8칸)", Layout.OvStats); Same(s, "⑦", "상단 스탯 칸(1칸)", Layout.OvStatCell);
            Same(s, "⑦", "배너(Level Up!)", Layout.OvBanner); Same(s, "⑦", "부제(Choose…)", Layout.OvSub);
            Same(s, "⑦", "특전 카드 1", Layout.OvCard1); Same(s, "⑦", "특전 카드 2", Layout.OvCard2); Same(s, "⑦", "특전 카드 3", Layout.OvCard3);
            Assert.That(Layout.OvCard2.Y - Layout.OvCard1.Y, Is.EqualTo(Layout.OvCardPitch).Within(0.05f));
            Same(s, "⑦", "카드 아이콘", Layout.OvCardIcon); Same(s, "⑦", "카드 문구", Layout.OvCardText);
            Same(s, "⑦", "하단 버튼", Layout.OvFoot); Same(s, "⑦", "인포(책) 버튼", Layout.OvInfo);
            Same(s, "⑦", "(인포 팝업) 박스", Layout.BookBox); Same(s, "⑦", "(인포 팝업) 제목 리본", Layout.BookRibbon); Same(s, "⑦", "(인포 팝업) 목록 카드", Layout.BookCard);
            SameV(s, "⑦", "(인포 팝업) 닫기 안내", 1, Layout.BookClose.Y);
        }
        [Test]
        public void Pet_MatchesSpec()
        {
            // ⑩ 펫 탭(13_pet.jpg) · ⑪ 펫 세부(14_pet_detail.jpg) — T42 워커 실측표 ↔ Layout.Pet*/Pd* 상수
            var s = Parse();
            Same(s, "⑩", "상단 바", Layout.LobbyTopBar); Same(s, "⑩", "펫 격자(9칸)", Layout.PetGrid); Same(s, "⑩", "펫 칸(1칸)", Layout.PetCell);
            Same(s, "⑩", "펫 Lv 라벨(1칸)", Layout.PetLv); Same(s, "⑩", "펫 진행바(1칸)", Layout.PetBar); Same(s, "⑩", "합계 줄", Layout.PetSum);
            Same(s, "⑩", "장착 띠", Layout.PetEqBand); Same(s, "⑩", "«장착중» 라벨", Layout.PetEqLabel); Same(s, "⑩", "장착 슬롯 줄(4칸)", Layout.PetSlots); Same(s, "⑩", "장착 슬롯 1칸", Layout.PetSlot);
            Same(s, "⑩", "전체 강화 버튼", Layout.PetUpgradeAll); Same(s, "⑩", "빠른 장착 버튼", Layout.PetQuickEquip); Same(s, "⑩", "소환 버튼", Layout.PetSummon); Same(s, "⑩", "소환 x10 버튼", Layout.PetSummon10);
            Same(s, "⑩", "하단 탭바", Layout.TabBar);
            // 격자 = 4열 × 3행이 표의 합집합과 맞는가(마지막 열 우변 · 마지막 행 아랫변) · 슬롯 줄 = 4칸 피치
            Assert.That(Layout.PetCell.X + 3 * Layout.PetColPitch + Layout.PetCell.W, Is.EqualTo(Layout.PetGrid.X + Layout.PetGrid.W).Within(0.15f));
            Assert.That(Layout.PetCell.Y + 2 * Layout.PetRowPitch + Layout.PetCell.H, Is.EqualTo(Layout.PetGrid.Y + Layout.PetGrid.H).Within(0.15f));
            Assert.That(Layout.PetSlot.X + 3 * Layout.PetSlotPitch + Layout.PetSlot.W, Is.EqualTo(Layout.PetSlots.X + Layout.PetSlots.W).Within(0.15f));
            Same(s, "⑪", "팝업 박스", Layout.PdBox); Same(s, "⑪", "펫 칸(세부)", Layout.PdCell); Same(s, "⑪", "진행바(세부)", Layout.PdBar); Same(s, "⑪", "설명 박스", Layout.PdDesc);
            Same(s, "⑪", "패시브 제목", Layout.PdPassiveTitle); Same(s, "⑪", "패시브 수치 줄", Layout.PdPassive); Same(s, "⑪", "강화 버튼", Layout.PdBtnL); Same(s, "⑪", "장착 버튼", Layout.PdBtnR);
            SameV(s, "⑪", "닫기 안내", 1, Layout.BookClose.Y - 1.1f);   // 표 90.4 = 공통 «탭하여 닫기» 줄(BookClose 91.5) 과 1.1 차 · 팝업은 BookClose 줄을 그대로 쓴다(±3%p 안)
        }
        [Test]
        public void Section12to18_DungeonArenaShellsMatchTables()
        {
            // ⑫~⑱ 던전·아레나 껍데기(T43) — 표 행 ↔ Layout 상수(워커 실측 · 화면 코드는 이 상수만 쓴다)
            var s = Parse();
            Same(s, "⑫", "상단 바", Layout.LobbyTopBar); Same(s, "⑫", "제목(Dungeons)", Layout.DgTitle); Same(s, "⑫", "제목 밑줄", Layout.DgTitleLine); Same(s, "⑫", "부제", Layout.DgSub);
            Same(s, "⑫", "던전 카드 1", Layout.DgCard1); Same(s, "⑫", "카드 제목 띠", Layout.DgCardHead); Same(s, "⑫", "카드 그림", Layout.DgCardPic); Same(s, "⑫", "입장 버튼", Layout.DgEnter);
            Same(s, "⑫", "보상 아이콘 줄", Layout.DgRewards); Same(s, "⑫", "던전 카드 2", Layout.DgCard2); Same(s, "⑫", "준비 중 카드", Layout.DgSoon);
            Same(s, "⑫", "하단 바", Layout.DgFoot); Same(s, "⑫", "뒤로 버튼", Layout.DgBack); Same(s, "⑫", "던전/PvP 탭(2칸)", Layout.DgTabs);
            Assert.That(Layout.DgCard2.Y - Layout.DgCard1.Y, Is.EqualTo(Layout.DgCardPitch).Within(0.05f));
            Same(s, "⑬", "팝업 박스", Layout.DdBox); Same(s, "⑬", "제목 띠", Layout.DdHead); Same(s, "⑬", "그림 띠", Layout.DdPic); Same(s, "⑬", "조건 문구", Layout.DdNote);
            Same(s, "⑬", "층수 화살표", Layout.DdArrow); Same(s, "⑬", "층수 원", Layout.DdFloor); Same(s, "⑬", "보상 박스", Layout.DdRewards); Same(s, "⑬", "보상 칸(4개)", Layout.DdRewardCells);
            Same(s, "⑬", "티켓 줄", Layout.DdTicket); Same(s, "⑬", "버튼 2개", Layout.DdBtns);
            Same(s, "⑭", "상단 바", Layout.LobbyTopBar); Same(s, "⑭", "제목(PvP)", Layout.ArTitle); Same(s, "⑭", "제목 밑줄", Layout.DgTitleLine); Same(s, "⑭", "부제", Layout.ArSub);
            Same(s, "⑭", "아레나 카드", Layout.ArCard); Same(s, "⑭", "카드 제목 띠", Layout.ArCardHead); Same(s, "⑭", "카드 그림", Layout.ArCardPic); Same(s, "⑭", "시즌 타이머", Layout.ArSeason);
            Same(s, "⑭", "입장 버튼", Layout.ArEnter); Same(s, "⑭", "티어 줄", Layout.ArTier); Same(s, "⑭", "준비 중 카드", Layout.ArSoon);
            Same(s, "⑭", "하단 바", Layout.DgFoot); Same(s, "⑭", "뒤로 버튼", Layout.DgBack); Same(s, "⑭", "던전/PvP 탭(2칸)", Layout.DgTabs);
            Same(s, "⑮", "상단 바", Layout.LobbyTopBar); Same(s, "⑮", "시상대 무대", Layout.AeStage); Same(s, "⑮", "티어 제목", Layout.AeTier); Same(s, "⑮", "시즌 타이머", Layout.AeSeason);
            Same(s, "⑮", "우측 아이콘 열(2개)", Layout.AeSideIcons); Same(s, "⑮", "시상대 초상(3개)", Layout.AePortraits); Same(s, "⑮", "1위 초상", Layout.AePortrait1); Same(s, "⑮", "시상대 배너(3개)", Layout.AeBanners);
            Same(s, "⑮", "순위 목록", Layout.AeList); Same(s, "⑮", "순위 줄(1칸)", Layout.AeRow); Same(s, "⑮", "승급 안내", Layout.AePromo);
            Same(s, "⑮", "하단 바", Layout.DgFoot); Same(s, "⑮", "뒤로 버튼", Layout.DgBack); Same(s, "⑮", "도전 버튼", Layout.AeChallenge);
            Same(s, "⑯", "팝업 박스", Layout.AcBox); Same(s, "⑯", "제목 띠", Layout.AcHead); Same(s, "⑯", "티켓·전투력 줄", Layout.AcInfoRow); Same(s, "⑯", "상대 목록(5줄)", Layout.AcList);
            Same(s, "⑯", "상대 줄(1칸)", Layout.AcRow); Same(s, "⑯", "줄 도전 버튼", Layout.AcRowBtn); Same(s, "⑯", "무료 새로고침 버튼", Layout.AcRefresh);
            Same(s, "⑰", "팝업 박스", Layout.RrBox); Same(s, "⑰", "제목 띠", Layout.RrHead); Same(s, "⑰", "티어 띠", Layout.RrTiers); Same(s, "⑰", "리셋 타이머", Layout.RrTimer);
            Same(s, "⑰", "안내 문구", Layout.RrNote); Same(s, "⑰", "보상 목록(4줄)", Layout.RrList); Same(s, "⑰", "보상 줄(1칸)", Layout.RrRow); Same(s, "⑰", "하단 탭(2개)", Layout.RrTabs);
            Same(s, "⑱", "상단 바", Layout.LobbyTopBar); Same(s, "⑱", "상인 배너", Layout.MeBanner); Same(s, "⑱", "제목(Merchant)", Layout.MeTitle); Same(s, "⑱", "시즌 타이머", Layout.MeSeason);
            Same(s, "⑱", "상품 격자", Layout.MeGrid); Same(s, "⑱", "상품 카드(1칸)", Layout.MeCard); Same(s, "⑱", "하단 바", Layout.DgFoot); Same(s, "⑱", "뒤로 버튼", Layout.DgBack);
        }
        [Test]
        public void SidePopups_MatchesSpec()
        {
            // ⑲ 특권(11) · ⑳ 퀘스트(15) · ㉑ 출석(16) · ㉒ 데일리 기프트(17) · ㉓ 7일 챌린지(18) · ㉔ 패스(19) — T44 워커 F 실측표 ↔ Layout.Pr*/Qs*/At*/Gf*/C7*/Ps* 상수(⑫~⑱ 은 T43 던전·아레나)
            var s = Parse();
            Same(s, "⑲", "상단 바", Layout.LobbyTopBar); Same(s, "⑲", "제목 줄", Layout.PrTitle); Same(s, "⑲", "제목 밑줄", Layout.PrUnderline); Same(s, "⑲", "부제", Layout.PrSub);
            Same(s, "⑲", "특권 카드 1", Layout.PrCard1); Same(s, "⑲", "카드 1 보상 칸", Layout.PrCard1Reward); Same(s, "⑲", "카드 1 버튼", Layout.PrCard1Btn);
            Same(s, "⑲", "특권 카드 2", Layout.PrCard2); Same(s, "⑲", "카드 제목 띠(2)", Layout.PrCardTitle); Same(s, "⑲", "카드 설명 상자(2)", Layout.PrCardDesc); Same(s, "⑲", "카드 그림(2)", Layout.PrCardPic);
            Same(s, "⑲", "카드 보상 칸(2)", Layout.PrCardReward); Same(s, "⑲", "카드 버튼(2)", Layout.PrCardBtn); Same(s, "⑲", "특권 카드 3", Layout.PrCard3); Same(s, "⑲", "특권 카드 4 (참고·컨테이너)", Layout.PrCard4);
            Same(s, "⑲", "바닥 바", Layout.PrFootBar); Same(s, "⑲", "뒤로 버튼", Layout.PrBack); Same(s, "⑲", "전체 받기 버튼", Layout.PrClaimAll);

            // T78 — 제목 조각이 프리팹 Title_Tapered_01_Brown 으로 바뀌어 표 ⑳ 행 이름만 «제목 띠» → «제목 리본»(자리·크기는 불변)
            Same(s, "⑳", "제목 리본", Layout.QsTitleBand); Same(s, "⑳", "팝업 박스", Layout.QsBox); Same(s, "⑳", "점수 트랙 상자", Layout.QsTrackBox);
            Same(s, "⑳", "트랙 아이콘 줄(6칸)", Layout.QsTrackIcons); Same(s, "⑳", "트랙 아이콘(1칸)", Layout.QsTrackIcon); Same(s, "⑳", "트랙 숫자 줄", Layout.QsTrackNums); Same(s, "⑳", "새로고침 줄", Layout.QsRefresh);
            Same(s, "⑳", "목록 상자", Layout.QsListBox); Same(s, "⑳", "퀘스트 줄 1", Layout.QsRow1); Same(s, "⑳", "퀘스트 줄 2", Layout.QsRow2);
            Same(s, "⑳", "퀘스트 보상 메달(1줄)", Layout.QsRowMedal); Same(s, "⑳", "퀘스트 제목(1줄)", Layout.QsRowTitle); Same(s, "⑳", "퀘스트 진행바(1줄)", Layout.QsRowBar); Same(s, "⑳", "이동 버튼(1줄)", Layout.QsRowGo);
            Same(s, "⑳", "탭 줄(3칸)", Layout.QsTabs); Same(s, "⑳", "탭(1칸)", Layout.QsTab);
            Assert.That(Layout.QsRow2.Y - Layout.QsRow1.Y, Is.EqualTo(Layout.QsRowPitch).Within(0.05f));
            Assert.That(Layout.QsTrackIcon.X + 5 * Layout.QsTrackPitch + Layout.QsTrackIcon.W, Is.EqualTo(Layout.QsTrackIcons.X + Layout.QsTrackIcons.W).Within(0.3f));
            Assert.That(Layout.QsTab.X + 2 * Layout.QsTabPitch + Layout.QsTab.W, Is.EqualTo(Layout.QsTabs.X + Layout.QsTabs.W).Within(0.3f));

            Same(s, "㉑", "제목 리본", Layout.AtRibbon); Same(s, "㉑", "팝업 박스", Layout.AtBox); Same(s, "㉑", "출석 격자(6칸)", Layout.AtGrid); Same(s, "㉑", "출석 칸(1칸)", Layout.AtCell);
            Same(s, "㉑", "칸 머리(1칸)", Layout.AtCellHead); Same(s, "㉑", "칸 보상 아이콘(1칸)", Layout.AtCellIcon); Same(s, "㉑", "7일 칸", Layout.AtDay7); Same(s, "㉑", "7일 칸 머리", Layout.AtDay7Head); Same(s, "㉑", "7일 보상 줄(2칸)", Layout.AtDay7Rewards);
            Assert.That(Layout.AtCell.X + 2 * Layout.AtColPitch + Layout.AtCell.W, Is.EqualTo(Layout.AtGrid.X + Layout.AtGrid.W).Within(0.15f));
            Assert.That(Layout.AtCell.Y + Layout.AtRowPitch + Layout.AtCell.H, Is.EqualTo(Layout.AtGrid.Y + Layout.AtGrid.H).Within(0.15f));
            Assert.That(Layout.AtDay7Cell.X + Layout.AtDay7Pitch + Layout.AtDay7Cell.W, Is.EqualTo(Layout.AtDay7Rewards.X + Layout.AtDay7Rewards.W).Within(0.15f));

            Same(s, "㉒", "선물 그림", Layout.GfPic); Same(s, "㉒", "제목 리본", Layout.GfRibbon); Same(s, "㉒", "팝업 박스", Layout.GfBox); Same(s, "㉒", "종료 시각 줄", Layout.GfTimer); Same(s, "㉒", "오늘의 선물 칸", Layout.GfTodayCell);
            Same(s, "㉒", "광고 줄 1", Layout.GfRow1); Same(s, "㉒", "광고 줄 2", Layout.GfRow2); Same(s, "㉒", "광고 줄 제목(1줄)", Layout.GfRowTitle); Same(s, "㉒", "광고 줄 진행바(1줄)", Layout.GfRowBar);
            Same(s, "㉒", "광고 줄 보상 아이콘(1줄)", Layout.GfRowReward); Same(s, "㉒", "광고 줄 버튼(1줄)", Layout.GfRowBtn); Same(s, "㉒", "오늘의 선물 버튼", Layout.GfTodayBtn);
            Assert.That(Layout.GfRow2.Y - Layout.GfRow1.Y, Is.EqualTo(Layout.GfRowPitch).Within(0.05f));
            // T77 «행은 중앙 정렬» — 광고 줄이 팝업 박스 가로 가운데(좌우 여백 같음) · 폭은 박스 안폭의 ~90% · 오늘의 선물 칸과 같은 폭
            Assert.That(Layout.GfRow1.X - Layout.GfBox.X, Is.EqualTo(Layout.GfBox.X + Layout.GfBox.W - (Layout.GfRow1.X + Layout.GfRow1.W)).Within(0.15f), "광고 줄 좌우 여백이 같다(중앙 정렬)");
            Assert.That(Layout.GfRow1.W / Layout.GfBox.W, Is.EqualTo(0.90).Within(0.02), "광고 줄 폭 = 상자 안폭의 ~90%");
            Assert.That(Layout.GfRow1.X, Is.EqualTo(Layout.GfTodayCell.X).Within(0.05f)); Assert.That(Layout.GfRow1.W, Is.EqualTo(Layout.GfTodayCell.W).Within(0.05f));
            // 레퍼런스 ✅(66.7 + 9.4/2 = 71.4) 와 버튼 가운데가 같다 — 폭만 버튼 글자에 맞춰 넓혔다
            Assert.That(Layout.GfRowBtn.X + Layout.GfRowBtn.W / 2f, Is.EqualTo(71.4f).Within(0.2f), "줄 버튼 가운데 = 레퍼런스 ✅ 가운데");
            Assert.That(Layout.GfTodayBtn.X, Is.EqualTo(Layout.GfRowBtn.X).Within(0.05f)); Assert.That(Layout.GfTodayBtn.W, Is.EqualTo(Layout.GfRowBtn.W).Within(0.05f));

            // ㉓ 7일 챌린지 · ㉔ 패스 — T78(주인 2026-09-07)로 화면째 삭제 · 표도 폐기
        }
        [Test]
        public void Common_TopBarAndTabBarSharedAcrossTabs()
        {
            // ⑧ 상단 바 같은 y·h · 하단 탭바 y92.6 h7.4 · 인벤 그리드 장비=대장간 · 팝업 폭 87 여백 6.5
            Assert.That(Layout.TabBar.Y, Is.EqualTo(92.6f).Within(0.01f)); Assert.That(Layout.TabBar.H, Is.EqualTo(7.4f).Within(0.01f));
            Assert.That(Layout.GearInv.X, Is.EqualTo(Layout.ForgeInv.X)); Assert.That(Layout.GearInv.Y, Is.EqualTo(Layout.ForgeInv.Y)); Assert.That(Layout.GearInv.W, Is.EqualTo(Layout.ForgeInv.W));
            foreach (var box in new[] { Layout.GdBox, Layout.BookBox, Layout.EvBox }) { Assert.That(box.W, Is.EqualTo(Layout.PopupW)); Assert.That(box.X, Is.EqualTo(Layout.PopupMarginX)); }
            var w = Layout.GdBtnL.Within(Layout.GdBox); Assert.That(w.X, Is.GreaterThan(0)); Assert.That(w.X + w.W, Is.LessThan(100));
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T293 ⓒ — 펫 그림 9벌의 자(<see cref="PetLook"/> + <c>catalog.json</c>).
    /// <para>
    /// 여기서 재는 것은 «어떤 그림이 예쁜가» 가 아니라 <b>표·키·카탈로그 셋이 어긋나지 않는가</b> 다 —
    /// 그림 하나가 카탈로그에 없으면 부팅이 그 키를 못 찾아 <b>PlayMode 가 통째로 빨개진다</b>(T211).
    /// 그래서 EditMode 에서 <c>catalog.json</c> 을 직접 읽어 9벌 × 3키가 다 있는지 먼저 본다.
    /// </para>
    /// <para>
    /// ⚠ <b>«9개가 다 다른가» 를 파일 경로로 잰다</b> — 키만 세면 언제나 다르다(키에 id 가 들어 있으니까).
    /// 눈이 보는 것은 파일이고, 아홉 마리가 같은 그림이면 «펫이 아홉 종» 이라는 말이 거짓이 된다.
    /// </para>
    /// </summary>
    public class PetLookTests
    {
        static PetData Load() => PetData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "pet.json"))));

        /// <summary>catalog.json 의 sprites 를 «키 → 경로» 로 읽는다(MiniJson · 게임이 쓰는 그 파서).</summary>
        static Dictionary<string, string> Sprites()
        {
            var j = new JNode(MiniJson.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "catalog.json")))))["sprites"];
            var map = new Dictionary<string, string>();
            foreach (var k in j.Keys) map[k] = j[k].Str();
            return map;
        }

        [Test]
        public void 아홉_마리가_저마다_투구_갑옷_손_세_키를_갖고_카탈로그에_다_있다()
        {
            var d = Load(); var sp = Sprites();
            Assert.AreEqual(9, d.Pets.Count, "펫은 아홉 마리다(주인 · 등급 3 × 발동 3)");
            foreach (var p in d.Pets)
                foreach (var key in new[] { PetLook.HelmetKey(p.Id), PetLook.ChestKey(p.Id), PetLook.HandKey(p.Id) })
                {
                    Assert.IsTrue(key.StartsWith(PetLook.Prefix), "펫 키는 " + PetLook.Prefix + " 로 시작한다: " + key);
                    Assert.IsTrue(sp.ContainsKey(key), "catalog.json 에 없는 펫 그림 키다 — 부팅이 여기서 운다(T211): " + key);
                }
        }

        [Test]
        public void 아홉_벌이_저마다_다른_그림이다()
        {
            var d = Load(); var sp = Sprites();
            var seen = new Dictionary<string, string>();
            foreach (var p in d.Pets)
            {
                // 한 마리의 «옷 한 벌» = 투구 + 갑옷 (손은 등급끼리 일부러 같이 쓴다 — 무기가 등급을 말한다)
                var suit = sp[PetLook.HelmetKey(p.Id)] + "|" + sp[PetLook.ChestKey(p.Id)];
                Assert.IsFalse(seen.ContainsKey(suit), "펫 «" + p.Name + "» 이 «" + (seen.ContainsKey(suit) ? seen[suit] : "") + "» 과 같은 옷을 입었다 — 아홉 종이 아홉으로 안 보인다");
                seen[suit] = p.Name;
            }
        }

        [Test]
        public void 손에_든_것은_표의_등급_발사체가_정한다()
        {
            var d = Load(); var sp = Sprites();
            foreach (var p in d.Pets)
            {
                var g = d.GradeOfPet(p);
                var slot = PetLook.HandSlot(d, p);
                var path = sp[PetLook.HandKey(p.Id)];
                Assert.AreEqual(g.Shot == PetKey.ShotBolt ? PetLook.SlotStaff : PetLook.SlotAxe, slot,
                    "번개를 쏘면 지팡이 · 도끼를 던지면 도끼여야 한다: " + p.Name);
                // 카탈로그 경로도 그 슬롯 폴더여야 한다 — 슬롯만 맞고 그림이 딴것이면 «도끼 슬롯에 지팡이» 가 된다
                Assert.IsTrue(path.Contains("/HandRight/" + slot + "/"),
                    "펫 «" + p.Name + "» 의 손 그림이 " + slot + " 폴더가 아니다: " + path);
            }
        }

        [Test]
        public void 방패_역할만_왼손에_방패를_든다()
        {
            var d = Load();
            foreach (var p in d.Pets)
            {
                var want = p.TriggerKey == PetKey.Hit ? PetLook.ShieldKey : null;
                Assert.AreEqual(want, PetLook.Shield(p), "방패는 피격 발동 펫만 든다: " + p.Name);
            }
        }

        [Test]
        public void 펫_그림은_기사_장비_그림과_한_장도_안_겹친다()
        {
            // 겹치면 «전설 펫» 이 «신화 갑옷 입은 나» 와 똑같이 보인다 — 펫이 제 것으로 안 읽힌다.
            var d = Load(); var sp = Sprites();
            var others = new HashSet<string>();
            foreach (var kv in sp)
                if (!kv.Key.StartsWith(PetLook.Prefix) && Regex.IsMatch(kv.Key, @"^cm\.")) others.Add(kv.Value);
            foreach (var p in d.Pets)
                foreach (var key in new[] { PetLook.HelmetKey(p.Id), PetLook.ChestKey(p.Id), PetLook.HandKey(p.Id) })
                    Assert.IsFalse(others.Contains(sp[key]), "펫 그림이 이미 쓰는 그림과 같다: " + key + " → " + sp[key]);
        }

        [Test]
        public void 표가_펫을_늘리면_그림도_같이_늘어야_한다()
        {
            // 이 자의 몫 — «표에 열 번째 펫을 적었는데 그림을 안 넣었다» 를 사람이 아니라 자가 먼저 본다.
            var d = Load(); var sp = Sprites();
            int keys = 0;
            foreach (var k in sp.Keys) if (k.StartsWith(PetLook.Prefix)) keys++;
            Assert.AreEqual(d.Pets.Count * 3, keys, "펫 그림 키는 «펫 수 × 3(투구·갑옷·손)» 이어야 한다 — 표와 카탈로그 중 한쪽만 늘었다");
        }
    }
}

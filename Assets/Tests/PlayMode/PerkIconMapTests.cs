using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T489 — <see cref="Icons.Perk"/> 의 <b>맨 끝 한 줄</b>이 조용히 틀리는 자리다.
    /// <para>
    /// 그 메서드는 특전 id 의 <b>어근을 차례로 재는 <c>if</c> 사슬</b>이고, 아무 줄도 안 맞으면 <c>"pi.star"</c> 를 돌려준다.
    /// 그런데 별은 <b>«모음» 특전(<c>p_coll…</c>)의 진짜 그림이기도 하다</b> — 그래서 화면에서 이 둘이 <b>한 글자도 안 다르다</b>:
    /// ⓐ «이 특전의 그림은 별이다» 와 ⓑ «이 id 는 어느 줄도 안 탔다» 가 같은 별로 뜬다.
    /// </para>
    /// <para>
    /// ⚠ <b>T458·T485 가 만든 자리다.</b> 그 전에는 전투 팝에 특전 아이콘이 아예 없어 이 메서드를 부르는 것은 특전 카드 화면뿐이었고,
    /// 거기 id 는 표에서 곧장 오니 눈에 띄었다. 이제는 <b>전투 중 뜨는 수</b>가 이 메서드를 부른다 — 엉뚱한 별이 떠도
    /// 빨간 줄도 로그도 안 난다(<c>Palette.cs</c> 가 <c>RarColors</c> 주석에서 스스로 경고한 그 병: «컴파일도 되고 빨간 줄도 안 난다»).
    /// </para>
    /// <para>
    /// 그래서 이 자가 재는 것은 «별이 몇 개인가» 가 <b>아니라</b> <b>«별이면 그 뿌리가 <c>coll</c> 인가»</b> 하나다 —
    /// 새 «모음» 특전이 늘어도 안 울고, <b>어느 줄도 안 탄 id 가 생기면 그 id 이름을 대고 운다</b>.
    /// (2026-09-12 실측: 특전 100개 · catch-all 에 닿는 id 0개 · 별 셋은 전부 <c>p_coll…</c>.)
    /// </para>
    /// </summary>
    public class PerkIconMapTests
    {
        /// <summary>«모음» 특전만 별을 쓴다 — <see cref="Icons.Perk"/> 의 <c>coll</c> 줄이 그 뜻이다.</summary>
        const string StarKey = "pi.star";

        [UnityTest]
        public IEnumerator EveryPerkIdMatchesARuleSoTheStarOnlyEverMeansTheCollectPerk()
        {
            GameData data = null; string err = null;
            yield return DataLoader.Load(d => data = d, e => err = e);
            Assert.IsNull(err, err);
            Assert.IsNotNull(data, "perks.json 이 안 올라왔다");

            var perks = data.Perks.Perks;
            Assert.That(perks.Count, Is.GreaterThan(0), "특전이 하나도 안 올라왔다 — 이 자가 재려던 것을 못 쟀다");

            var strays = new List<string>();
            int stars = 0;
            foreach (var p in perks)
            {
                Assert.That(p.Id, Is.Not.Null.And.Not.Empty, "id 없는 특전이 있다");
                string key = Icons.Perk(p.Id);
                Assert.That(key, Is.Not.Null.And.Not.Empty, p.Id + " 의 아이콘 키가 비었다");
                if (key != StarKey) continue;

                stars++;
                // 별이 나왔다 — 「모음」 이라서인가, 아무 줄도 안 타서인가. 뿌리로 가른다.
                string root = p.Id.StartsWith("p_") ? p.Id.Substring(2) : p.Id;
                if (!root.StartsWith("coll")) strays.Add(p.Id);
            }

            Assert.That(stars, Is.GreaterThan(0),
                "별을 쓰는 특전이 하나도 없다 — 「모음」 특전이 사라졌거나 Icons.Perk 의 coll 줄이 바뀌었다. "
                + "그렇다면 이 자가 지키던 «별 = 모음» 이라는 뜻 자체가 없어진 것이니, 자를 지울지 어근을 고칠지 사람이 정해야 한다");

            Assert.That(strays, Is.Empty,
                "Icons.Perk 의 어느 줄도 안 탄 특전이 있다 — 이 id 들은 전투 팝에서 «모음» 특전과 똑같은 별을 달고 뜬다(T489). "
                + "고칠 곳은 Assets/Scripts/Game/Palette.cs 의 Icons.Perk: 이 어근을 받는 줄을 하나 더하라. "
                + "그 줄은 «더 좁은 것이 먼저» 차례를 지켜야 한다(예: Bolt 는 crit 보다 먼저). 안 탄 id = "
                + string.Join(", ", strays.ToArray()));
        }
    }
}

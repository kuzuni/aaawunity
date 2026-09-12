using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;

namespace KkomaKnight.Tests.Play
{
    /// <summary>T504 — 피격 알갱이 버스트는 가산 블렌드도 라이트도 없다(주인 2026-09-12 «힛 이펙트가 너무 반짝임 · 너무 글로우임»).</summary>
    public class HitBurstTests
    {
        [Test]
        public void 힛_버스트는_알파_블렌드이고_라이트가_없다()
        {
            var go = Fx.HitBurst(Vector3.zero, Fx.HitGrain, 1f, 10);
            try
            {
                Assert.IsNotNull(go);
                var psr = go.GetComponent<ParticleSystemRenderer>();
                Assert.IsNotNull(psr);
                Assert.AreEqual("Sprites/Default", psr.sharedMaterial.shader.name, "알파 블렌드(가산 아님) — 주인 «너무 반짝임 너무 글로우» 의 정체가 «… add.mat» 이었다");
                Assert.IsNotNull(psr.sharedMaterial.mainTexture, "둥근 알갱이 텍스처");
                Assert.AreEqual(0, go.GetComponentsInChildren<Light>(true).Length, "포인트 라이트 0(CFXR Hit A 에는 있었다)");
                Assert.AreEqual(Fx.SortingOrder, psr.sortingOrder, "다른 이펙트와 같은 층");
                var ps = go.GetComponent<ParticleSystem>();
                Assert.IsFalse(ps.main.loop, "한 번 튀고 끝");
                Assert.AreEqual(ParticleSystemSimulationSpace.World, ps.main.simulationSpace);
                Assert.Greater(ps.emission.burstCount, 0, "버스트 하나");
            }
            finally { if (go != null) Object.DestroyImmediate(go); }
        }
    }
}

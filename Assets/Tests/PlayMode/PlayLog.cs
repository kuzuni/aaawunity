using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// «플레이 콘솔 빨간 줄 0» 검사 도우미 (T11). 콘솔의 <b>빨간 줄</b>(Error · Exception · Assert)만 모아 두고 <see cref="AssertNoRed"/> 에서 전부 나열해 실패시킨다.
    /// ⚠ <c>LogAssert.NoUnexpectedReceived()</c> 를 쓰지 않는다 — 이 프로젝트의 Test Framework 에서는 일반 <c>Debug.Log</c>(예: Bootstrap 의 «data loaded …»)도
    /// «예상 밖 로그» 로 보고 테스트를 실패시켰다(CI 런 #33 · HeroViewTests 회귀). 노란 경고는 여기서 안 보고, 프리팹 경로/카탈로그 키 경고는 UiSmokeTests 가 따로 잡는다.
    /// 에러 로그가 «자동으로» 테스트를 실패시키는 Test Framework 기본 동작은 그대로다 — 이 도우미는 «어느 화면에서» 났는지를 메시지에 남기는 용도.
    /// </summary>
    public sealed class PlayLog : IDisposable
    {
        readonly List<string> _red = new List<string>();
        public PlayLog() { Application.logMessageReceived += OnLog; }
        public void Dispose() { Application.logMessageReceived -= OnLog; }
        void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _red.Add($"[{type}] {msg}" + (string.IsNullOrEmpty(stack) ? "" : "\n    " + stack.Trim().Replace("\n", "\n    ")));
        }
        public int RedCount => _red.Count;
        /// <summary>지금까지 모인 빨간 줄이 있으면 전부 나열하며 실패. 검사 뒤 목록은 비운다(다음 지점은 그 뒤 것만 본다).</summary>
        public void AssertNoRed(string where)
        {
            if (_red.Count == 0) return;
            var all = string.Join("\n", _red); _red.Clear();
            Assert.Fail($"[{where}] 플레이 콘솔 빨간 줄 {all.Split('\n').Length}줄(에러·예외·Assert) — 주인 상시 지시 «플레이 콘솔 에러 0»:\n{all}");
        }
    }
}

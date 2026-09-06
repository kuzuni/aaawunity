namespace KkomaKnight.Core
{
    /// <summary>글자 종류 — 종류마다 최소 크기 하한이 다르다(T63 · 주인 2026-09-06 «글씨가 너무 작아 안 읽힌다 · 다 바꿔라»).</summary>
    public enum TextKind
    {
        /// <summary>본문·설명·목록 줄 — 최소 <see cref="TextSize.Body"/>.</summary>
        Body = 0,
        /// <summary>버튼 글자 — 최소 <see cref="TextSize.Button"/>.</summary>
        Button = 1,
        /// <summary>보조 라벨(pill 숫자 · «남은 횟수» · 타이머 · 등수) — 최소 <see cref="TextSize.Aux"/>.</summary>
        Aux = 2,
        /// <summary>제목·명판 — 최소 <see cref="TextSize.Title"/>.</summary>
        Title = 3,
        /// <summary>정말 작아야 하는 곳(아이콘 위 «+1» 배지 등) — 하한 없음. 호출부가 명시해야만 쓰인다(= 지시서의 allowSmall:true).</summary>
        Small = 4,
    }

    /// <summary>
    /// 글자 최소 크기 규칙 한 곳(T63 1항). 프레임 1080×2337 기준 — 폰 폭 412css px 면 프레임 1px ≈ 0.38css px 라 40 ≈ 15px.
    /// UiKit 의 Text/Label/SetText/Button/프리팹 변환이 <see cref="Floor"/> 로 올리고, PlayMode 게이트(TextSizeGateTests)가 모든 화면의 활성 Text 를 모아 이 하한을 단언한다.
    /// 수치는 밸런스가 아니라 표시 규칙(주인 지시 · ROUTINE T63).
    /// </summary>
    public static class TextSize
    {
        public const int Body = 40;
        public const int Button = 44;
        public const int Aux = 36;
        public const int Title = 60;
        /// <summary>bestFit 이 자동으로 줄일 수 있는 최소 — 이 밑으로는 못 내려간다(Small 제외).</summary>
        public const int BestFitMin = 32;
        /// <summary>데미지 팝·전투 숫자 — 지금보다 1.3배(전투 화면 하위 행이 쓴다).</summary>
        public const float BattleNumberMul = 1.3f;

        /// <summary>
        /// «글자 칸 세로 = 크기 × 1.4» (결정 141 · T63-events 가 Jua 로 실측해 정한 규격 — 잉크 ≈ 크기×0.75+2 에 줄 사이 여백까지).
        /// 칸이 이보다 낮으면 잘리거나 bestFit 이 <see cref="BestFitMin"/> 까지 말없이 줄인다(게이트 표에는 안 잡히는 쪽).
        /// </summary>
        public const float LineBox = 1.4f;

        /// <summary>글자 <paramref name="size"/> 로 <paramref name="lines"/> 줄을 담으려면 칸 세로가 최소 얼마여야 하나(px).</summary>
        public static float BoxHeight(int size, int lines = 1) => size * LineBox * (lines < 1 ? 1 : lines);

        public static int Min(TextKind kind)
        {
            switch (kind)
            {
                case TextKind.Body: return Body;
                case TextKind.Button: return Button;
                case TextKind.Aux: return Aux;
                case TextKind.Title: return Title;
                default: return 0;
            }
        }

        /// <summary>요청 크기가 종류 하한보다 작으면 하한으로(경고 없이). Small 은 그대로.</summary>
        public static int Floor(int size, TextKind kind = TextKind.Body)
        {
            int m = Min(kind);
            return size < m ? m : size;
        }

        /// <summary>bestFit 최소 크기 하한 — Small 이 아니면 <see cref="BestFitMin"/> 아래로 못 내려간다.</summary>
        public static int BestFitFloor(int min, TextKind kind = TextKind.Body)
        {
            if (kind == TextKind.Small) return min;
            return min < BestFitMin ? BestFitMin : min;
        }
    }
}

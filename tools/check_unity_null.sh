#!/usr/bin/env bash
# 유니티 «가짜 null» 게이트 (T11 · 주인 지시 2026-09-05 «같은 패턴 재발 금지»).
#
# `GetComponent<T>() ?? AddComponent<T>()` · `Find(...) ?? ...` 는 UnityEngine.Object 가 == 만 재정의하고 ?? 는 재정의하지 못해
# 에디터의 «파괴됐거나 없는 컴포넌트(가짜 null)» 에서 왼쪽을 그대로 돌려준다 → AddComponent 가 안 돌아 MissingComponentException
# (대장간 «Button_02_Orange CanvasGroup» 사고 · e64ff41 이 UiKit.Ensure<T> 로 고침). 이 스크립트는 Assets/Scripts 에 그 패턴이 0건인지 본다.
#
# 사용: tools/check_unity_null.sh            # 문제 있으면 exit 1 (CI dotnet 잡 · ROUTINE §3 게이트)
set -u
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DIR="$ROOT/Assets/Scripts"
# ⓐ GetComponent·GetComponentInChildren·GetComponentInParent·AddComponent·Find·FindAny·FindObjectOfType 류 호출 뒤 `??`
# ⓑ `as T ??`(UnityEngine.Object 캐스트 뒤 ??) 는 UI 코드에서 흔한 실수라 함께 본다. 주석 줄(// …)은 뺀다.
PATTERN='(GetComponent(InChildren|InParent|s)?(<[^>]*>)?\([^)]*\)|AddComponent(<[^>]*>)?\([^)]*\)|\bFind(Any|ByName|Child|FirstObjectByType|ObjectOfType|ObjectsByType)?(<[^>]*>)?\([^)]*\)|\bas +(Transform|RectTransform|GameObject|Component|[A-Z][A-Za-z]*Behaviour|Image|Text|Button|Canvas|CanvasGroup|Camera|Slider|Animator|SpriteRenderer))[[:space:]]*\?\?'
hits=$(grep -rnE --include='*.cs' "$PATTERN" "$DIR" | grep -vE '^[^:]+:[0-9]+:[[:space:]]*//' | grep -vE '///' || true)
if [ -n "$hits" ]; then
  echo "✗ 유니티 가짜 null 패턴 발견 — UnityEngine.Object 에는 ?? 를 쓰지 않는다. UiKit.Ensure<T>(go) 또는 «if (x == null)» 로 바꾼다:"
  echo "$hits"
  exit 1
fi
echo "✓ check_unity_null: Assets/Scripts 에 «GetComponent…() ??»·«Find(…) ??» 패턴 0건"

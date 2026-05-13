using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 씬 HUD — 활성 외침(Shout) 메시지를 화면 상단에 순환 표시한다.
/// ShoutManager.Begin()이 인스턴스를 생성·캐시하고,
/// SetMessages()로 메시지 목록을 전달받아 4초 간격으로 회전 표시한다.
/// 모든 메시지를 1회 순회 완료하면 OnAllShoutsCompleted를 발행하고 Hide()한다.
/// </summary>
public class UI_HUDShout : UI_Base
{
    // 표시할 메시지 텍스트 컴포넌트 — Inspector에서 참조 연결 필요
    [SerializeField] TMP_Text _messageText;

    // 반투명 배경 Image — Inspector에서 참조 연결 필요
    [SerializeField] Image _background;

    // 메시지 1개 표시 지속 시간 (초) — Inspector에서 조정 가능
    [SerializeField] float _rotateSeconds = 4f;

    // 현재 표시 중인 외침 목록
    List<ShoutDto> _messages = new List<ShoutDto>();

    // 현재 표시 중인 메시지 인덱스
    int _currentIdx;

    // 메시지 회전 코루틴 참조 — 재시작 시 이전 코루틴 중단용
    Coroutine _rotateCoroutine;

    /// <summary>
    /// 마지막 외침까지 모두 표시 완료된 시점에 호출된다.
    /// ShoutManager가 본 외침 id 저장 용도로 구독한다.
    /// Hide() 자체에서는 절대 호출하지 않음 — RotateRoutine 정상 종료 시에만 발행.
    /// </summary>
    public Action OnAllShoutsCompleted;

    /// <summary>
    /// 표시할 외침 목록을 설정하고 HUD를 활성화한다.
    /// 빈 목록이거나 모든 메시지가 이미 만료된 경우 즉시 숨긴다.
    /// 메시지 개수와 무관하게 항상 RotateRoutine을 시작하여 만료 감시와 회전을 함께 처리한다.
    /// </summary>
    /// <param name="messages">표시할 외침 DTO 목록</param>
    public void SetMessages(List<ShoutDto> messages)
    {
        // 빈 목록이면 숨김 처리 후 즉시 반환
        if (messages == null || messages.Count == 0)
        {
            Hide();
            return;
        }

        // 목록 갱신 및 인덱스 초기화
        _messages = messages;
        _currentIdx = 0;

        gameObject.SetActive(true);

        // 첫 메시지가 이미 만료됐을 수 있으므로 유효한 인덱스 탐색 후 표시
        if (!TryAdvanceToValidIndex(_currentIdx))
        {
            // 모든 메시지가 이미 만료된 경우
            Hide();
            return;
        }
        DisplayCurrent();

        // 메시지 개수와 무관하게 코루틴 항상 시작 — 만료 감시 + 회전 + 1회 순회 종료 담당
        if (_rotateCoroutine != null)
            StopCoroutine(_rotateCoroutine);
        _rotateCoroutine = StartCoroutine(RotateRoutine());
    }

    /// <summary>
    /// HUD를 숨기고 회전 코루틴을 중단한다.
    /// ShoutManager.End() 또는 1회 순회 완료 시 호출된다.
    /// 이 메서드 자체에서는 OnAllShoutsCompleted를 호출하지 않는다.
    /// </summary>
    public void Hide()
    {
        // 진행 중인 회전 코루틴 중단
        if (_rotateCoroutine != null)
        {
            StopCoroutine(_rotateCoroutine);
            _rotateCoroutine = null;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 1초 단위 tick으로 만료 여부를 감시하고, _rotateSeconds 누적 시 다음 메시지로 회전한다.
    /// 모든 메시지를 1회 순회하면 OnAllShoutsCompleted를 발행하고 종료한다.
    /// Count == 1 케이스도 동일 흐름으로 처리 — 첫 tick에서 shownCount >= Count 충족 시 종료.
    /// </summary>
    private IEnumerator RotateRoutine()
    {
        // tick 주기: 1초마다 만료 여부 체크
        const float TICK = 1f;
        float elapsed = 0f;

        // SetMessages에서 DisplayCurrent()를 이미 호출했으므로 첫 메시지는 표시된 것으로 카운트
        int shownCount = 1;

        while (true)
        {
            yield return new WaitForSeconds(TICK);
            elapsed += TICK;

            // 현재 메시지 만료 여부 — 만료됐거나 회전 타이밍이 됐으면 처리
            bool currentExpired = DateTime.UtcNow >= ParseUtcSafe(_messages[_currentIdx].expiresAt);
            bool shouldRotate   = elapsed >= _rotateSeconds;

            if (!currentExpired && !shouldRotate)
                continue;

            // 이미 N개 모두 표시했으면 1회 순회 완료 — 정상 종료 (콜백 발행 후 Hide)
            if (shownCount >= _messages.Count)
            {
                OnAllShoutsCompleted?.Invoke();
                Hide();
                yield break;
            }

            // 다음 유효 인덱스 탐색 (만료된 것 스킵)
            int nextIdx = (_currentIdx + 1) % _messages.Count;
            if (!TryAdvanceFrom(nextIdx, out int validIdx))
            {
                // 남은 메시지가 모두 만료 — 1회 순회 처리로 간주하고 정상 종료
                OnAllShoutsCompleted?.Invoke();
                Hide();
                yield break;
            }

            _currentIdx = validIdx;
            DisplayCurrent();
            shownCount++;
            elapsed = 0f;
        }
    }

    /// <summary>
    /// startIdx부터 순환하며 만료되지 않은 첫 번째 인덱스를 반환한다.
    /// </summary>
    /// <param name="startIdx">탐색 시작 인덱스</param>
    /// <param name="foundIdx">발견된 유효 인덱스. 없으면 -1</param>
    /// <returns>유효한 메시지가 있으면 true</returns>
    private bool TryAdvanceFrom(int startIdx, out int foundIdx)
    {
        int idx = startIdx;
        for (int i = 0; i < _messages.Count; i++)
        {
            // 만료 안 된 메시지 발견
            if (DateTime.UtcNow < ParseUtcSafe(_messages[idx].expiresAt))
            {
                foundIdx = idx;
                return true;
            }
            idx = (idx + 1) % _messages.Count;
        }

        // 전체 순환 후에도 유효한 메시지 없음
        foundIdx = -1;
        return false;
    }

    /// <summary>
    /// SetMessages 초기 진입 시 _currentIdx를 유효한 첫 번째 인덱스로 갱신한다.
    /// </summary>
    /// <param name="startIdx">탐색 시작 인덱스 (보통 0)</param>
    /// <returns>유효한 메시지가 있으면 true</returns>
    private bool TryAdvanceToValidIndex(int startIdx)
    {
        if (TryAdvanceFrom(startIdx, out int found))
        {
            _currentIdx = found;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 현재 인덱스의 메시지를 화면에 출력한다.
    /// </summary>
    private void DisplayCurrent()
    {
        if (_messageText == null) return;
        if (_messages == null || _currentIdx >= _messages.Count) return;

        _messageText.text = _messages[_currentIdx].message;
    }

    /// <summary>
    /// ISO 8601 문자열을 UTC DateTime으로 안전하게 파싱한다.
    /// 파싱 실패 시 DateTime.MaxValue를 반환하여 만료 없음으로 처리한다.
    /// </summary>
    /// <param name="iso">ISO 8601 형식 날짜 문자열 (expiresAt 필드)</param>
    /// <returns>UTC DateTime. 파싱 실패 시 DateTime.MaxValue</returns>
    private static DateTime ParseUtcSafe(string iso)
    {
        if (string.IsNullOrEmpty(iso))
            return DateTime.MaxValue;

        // RoundtripKind — Z(UTC) 접미사 및 오프셋 표기 모두 처리
        if (DateTime.TryParse(iso, null, DateTimeStyles.RoundtripKind, out DateTime dt))
            return dt.ToUniversalTime();

        // 파싱 실패 — 만료 안 됨으로 간주
        Debug.LogWarning($"[UI_HUDShout] expiresAt 파싱 실패: {iso}");
        return DateTime.MaxValue;
    }

    // UI_Base의 언어 변경 이벤트 구독은 HUD 외침에서 불필요 — 빈 오버라이드로 차단
    protected override void OnEnable()
    {
        // 외침 HUD는 언어 변경 이벤트를 구독하지 않음
    }

    protected override void OnDisable()
    {
        // 외침 HUD는 언어 변경 이벤트를 해제하지 않음
    }
}

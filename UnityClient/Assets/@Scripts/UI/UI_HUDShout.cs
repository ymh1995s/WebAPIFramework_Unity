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
/// 빈 목록이거나 만료되면 Hide()로 자동 숨김 처리한다.
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
    /// 표시할 외침 목록을 설정하고 HUD를 활성화한다.
    /// 빈 목록이면 즉시 숨긴다.
    /// </summary>
    /// <param name="messages">표시할 외침 DTO 목록 (만료 필터링은 ShoutManager 측에서 처리)</param>
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

        // 첫 번째 메시지 즉시 표시
        DisplayCurrent();

        // 메시지가 2개 이상이면 회전 코루틴 시작 (이미 실행 중이면 재시작)
        if (_messages.Count > 1)
        {
            if (_rotateCoroutine != null)
                StopCoroutine(_rotateCoroutine);
            _rotateCoroutine = StartCoroutine(RotateRoutine());
        }
        else
        {
            // 메시지 1개면 회전 불필요 — 기존 코루틴만 중단
            if (_rotateCoroutine != null)
            {
                StopCoroutine(_rotateCoroutine);
                _rotateCoroutine = null;
            }
        }
    }

    /// <summary>
    /// HUD를 숨기고 회전 코루틴을 중단한다.
    /// ShoutManager.End() 또는 만료 감지 시 호출된다.
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
    /// 4초 간격으로 다음 메시지로 전환하는 무한 코루틴.
    /// 만료된 메시지는 건너뛰고, 유효한 메시지가 없으면 Hide()로 종료한다.
    /// </summary>
    private IEnumerator RotateRoutine()
    {
        while (true)
        {
            // _rotateSeconds 대기 후 다음 메시지로 전환
            yield return new WaitForSeconds(_rotateSeconds);

            // 다음 인덱스로 전진 (순환)
            int nextIdx = (_currentIdx + 1) % _messages.Count;

            // 만료된 메시지를 스킵하며 유효한 메시지 탐색
            int searched = 0;
            while (searched < _messages.Count)
            {
                // 만료 여부 확인 (UTC 비교)
                if (DateTime.UtcNow < ParseUtcSafe(_messages[nextIdx].expiresAt))
                    break;  // 유효한 메시지 발견

                // 만료됨 — 다음 인덱스로
                nextIdx = (nextIdx + 1) % _messages.Count;
                searched++;
            }

            // 모든 메시지가 만료된 경우 HUD 숨김
            if (searched >= _messages.Count)
            {
                Hide();
                yield break;
            }

            _currentIdx = nextIdx;
            DisplayCurrent();
        }
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

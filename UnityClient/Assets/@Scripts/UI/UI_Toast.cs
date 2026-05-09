using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 하단에 잠깐 표시되었다가 자동으로 사라지는 토스트 메시지 UI.
/// UIManager.ShowToast() 를 통해 호출하며, 지속 시간이 끝나면 자체적으로 닫힌다.
/// 팝업 스택과는 독립적으로 동작하므로 PopupRoot 대신 ToastRoot 에 배치된다.
/// </summary>
public class UI_Toast : UI_Base
{
    // 메시지 텍스트 컴포넌트 — Inspector에서 참조 연결 필요
    [SerializeField] TMP_Text _messageText;

    // 배경 Image — 알파값 애니메이션에 사용
    [SerializeField] Image _background;

    // 기본 표시 지속 시간 (초)
    const float DEFAULT_DURATION = 2.0f;

    // 페이드 아웃 시간 (초)
    const float FADE_DURATION = 0.4f;

    // 현재 실행 중인 자동 닫힘 코루틴 참조 — 중복 호출 시 이전 코루틴 취소용
    Coroutine _autoHideCoroutine;

    /// <summary>
    /// 토스트 메시지를 설정하고 표시 타이머를 시작한다.
    /// 이미 표시 중인 경우 이전 타이머를 취소하고 새 메시지로 갱신한다.
    /// </summary>
    /// <param name="message">표시할 메시지</param>
    /// <param name="duration">표시 지속 시간(초). 0 이하이면 기본값 사용</param>
    public void Show(string message, float duration = DEFAULT_DURATION)
    {
        // 이전 자동 닫힘 코루틴이 실행 중이면 취소
        if (_autoHideCoroutine != null)
        {
            StopCoroutine(_autoHideCoroutine);
            _autoHideCoroutine = null;
        }

        // 텍스트 설정
        if (_messageText != null)
            _messageText.text = message;

        // 완전 불투명 상태로 초기화
        SetAlpha(1f);
        gameObject.SetActive(true);

        // 지속 시간 후 자동 숨김 코루틴 시작
        float showDuration = duration > 0f ? duration : DEFAULT_DURATION;
        _autoHideCoroutine = StartCoroutine(AutoHideRoutine(showDuration));
    }

    /// <summary>
    /// 지정 시간 대기 후 페이드 아웃하여 토스트를 숨기는 코루틴
    /// </summary>
    private IEnumerator AutoHideRoutine(float duration)
    {
        // 지정 시간 동안 표시 유지
        yield return new WaitForSeconds(duration);

        // 페이드 아웃
        float elapsed = 0f;
        while (elapsed < FADE_DURATION)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / FADE_DURATION);
            SetAlpha(alpha);
            yield return null;
        }

        // 완전히 숨김 처리
        gameObject.SetActive(false);
        SetAlpha(1f);
        _autoHideCoroutine = null;
    }

    /// <summary>
    /// 배경과 텍스트의 알파값을 동시에 설정한다
    /// </summary>
    private void SetAlpha(float alpha)
    {
        if (_background != null)
        {
            var c = _background.color;
            c.a = alpha;
            _background.color = c;
        }

        if (_messageText != null)
        {
            var c = _messageText.color;
            c.a = alpha;
            _messageText.color = c;
        }
    }

    // UI_Base의 언어 변경 이벤트 구독은 토스트에서 불필요 — 빈 오버라이드로 차단
    protected override void OnEnable()
    {
        // 토스트는 언어 변경 이벤트를 구독하지 않음
    }

    protected override void OnDisable()
    {
        // 토스트는 언어 변경 이벤트를 해제하지 않음
    }
}

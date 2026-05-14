using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전화면 반투명 마스크 UI — 버튼 이중 클릭 방지 3계층 중 A계층(글로벌 마스크) 구현체.
/// UIManager.BeginBusy() 가 카운트 0→1 시 Show, 1→0 시 Hide를 호출한다.
/// IUI_Popup 미상속 — 팝업 스택과 완전 독립. BusyRoot에 배치됨.
/// </summary>
public class UI_BusyMask : UI_Base
{
    // 전화면 반투명 배경 — Raycast Target이 켜져 있어 하위 UI 입력을 차단한다
    [SerializeField] Image _background;

    // 작업 내용 안내 텍스트 — label 인자가 있을 때만 표시
    [SerializeField] TMP_Text _labelText;

    /// <summary>
    /// BusyMask를 활성화한다.
    /// label이 비어 있으면 텍스트를 숨기고, 있으면 내용을 갱신하여 표시한다.
    /// </summary>
    /// <param name="label">마스크 위에 표시할 안내 문구. null 또는 빈 문자열이면 텍스트 비표시</param>
    public void Show(string label = null)
    {
        gameObject.SetActive(true);

        // 텍스트 컴포넌트가 연결된 경우에만 갱신
        if (_labelText != null)
        {
            bool hasLabel = !string.IsNullOrEmpty(label);
            _labelText.gameObject.SetActive(hasLabel);
            if (hasLabel)
                _labelText.text = label;
        }
    }

    /// <summary>
    /// BusyMask를 비활성화한다. 카운트가 0이 되었을 때 UIManager가 호출한다.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // BusyMask는 언어 변경 이벤트 구독이 불필요 — UI_Base의 OnEnable/OnDisable 차단
    protected override void OnEnable()
    {
        // 언어 변경 이벤트 구독 안 함 (UI_Base 기본 동작 차단)
    }

    protected override void OnDisable()
    {
        // 언어 변경 이벤트 해제 안 함 (UI_Base 기본 동작 차단)
    }
}

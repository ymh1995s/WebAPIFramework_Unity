using System;
using UnityEngine;

/// <summary>
/// 확인/취소 선택 팝업 UI.
/// 안내 메시지를 표시하고 두 버튼(OkBtn, CancelBtn)에 각각의 콜백을 연결한다.
/// 버튼 클릭 시 팝업을 닫은 뒤 해당 콜백을 실행한다.
/// PopupService.ShowSelect 를 통해 표시한다.
/// </summary>
public class UI_WithdrawPopup : UI_UGUI, IUI_Popup
{
    // TMP_Text 자식 오브젝트 이름 열거형 — 프리팹 자식 이름과 일치해야 함
    enum Texts   { Text }

    // Button 자식 오브젝트 이름 열거형 — 프리팹의 OkBtn, CancelBtn 과 일치
    enum Buttons { OkBtn, CancelBtn }

    /// <summary>
    /// 확인(Ok) 버튼 클릭 시 실행되는 콜백.
    /// 팝업이 닫힌 후 실행된다.
    /// </summary>
    public Action OnOk     { get; set; }

    /// <summary>
    /// 취소(Cancel) 버튼 클릭 시 실행되는 콜백.
    /// 팝업이 닫힌 후 실행된다.
    /// </summary>
    public Action OnCancel { get; set; }

    protected override void Awake()
    {
        base.Awake();

        // 자식 컴포넌트 바인딩
        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        // 각 버튼에 클릭 핸들러 등록
        GetButton((int)Buttons.OkBtn).onClick.AddListener(OnClickOk);
        GetButton((int)Buttons.CancelBtn).onClick.AddListener(OnClickCancel);
    }

    /// <summary>
    /// 팝업에 표시할 선택 안내 메시지를 설정한다.
    /// </summary>
    /// <param name="message">사용자에게 보여줄 선택 안내 문구</param>
    public void SetText(string message)
        => GetText((int)Texts.Text).text = message;

    /// <summary>
    /// 확인 버튼 클릭 처리.
    /// 팝업을 먼저 닫고 OnOk 콜백을 실행한다.
    /// </summary>
    private void OnClickOk()
    {
        UIManager.Instance.ClosePopupUI();
        OnOk?.Invoke();
    }

    /// <summary>
    /// 취소 버튼 클릭 처리.
    /// 팝업을 먼저 닫고 OnCancel 콜백을 실행한다.
    /// </summary>
    private void OnClickCancel()
    {
        UIManager.Instance.ClosePopupUI();
        OnCancel?.Invoke();
    }
}

using System;
using UnityEngine;

/// <summary>
/// 오류 안내 팝업 UI.
/// 오류 메시지를 표시하고 확인 버튼을 누르면 OnOk 콜백을 실행한다.
/// PopupService.ShowError 를 통해 표시하며, 콜백 기본값은 게임 재시작이다.
/// </summary>
public class UI_ErrorPopup : UI_UGUI, IUI_Popup
{
    // TMP_Text 자식 오브젝트 이름 열거형 — 프리팹 자식 GameObject 이름과 일치해야 함
    enum Texts   { Text }

    // Button 자식 오브젝트 이름 열거형
    enum Buttons { ConfirmBtn }

    /// <summary>
    /// 확인 버튼 클릭 시 실행되는 콜백.
    /// PopupService 에서 RestartGame 으로 설정한다.
    /// </summary>
    public Action OnOk { get; set; }

    protected override void Awake()
    {
        base.Awake();

        // 자식 컴포넌트 바인딩
        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        // 확인 버튼 클릭 시 OnOk 콜백 실행
        GetButton((int)Buttons.ConfirmBtn).onClick.AddListener(() => OnOk?.Invoke());
    }

    /// <summary>
    /// 팝업에 표시할 오류 메시지를 설정한다.
    /// </summary>
    /// <param name="message">사용자에게 보여줄 오류 문구</param>
    public void SetText(string message)
        => GetText((int)Texts.Text).text = message;
}

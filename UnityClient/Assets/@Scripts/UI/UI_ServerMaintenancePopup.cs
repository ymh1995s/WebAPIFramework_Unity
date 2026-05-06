using System;
using UnityEngine;

/// <summary>
/// 서버 점검 안내 팝업 UI.
/// 점검 메시지를 표시하고 확인 버튼 클릭 시 OnConfirm 콜백을 실행한다.
/// PopupService.ShowMaintenance 를 통해 표시하며, 중복 표시는 _maintenanceShown 플래그로 방지한다.
/// </summary>
public class UI_ServerMaintenancePopup : UI_UGUI, IUI_Popup
{
    // TMP_Text 자식 오브젝트 이름 열거형 — 프리팹 자식 이름과 일치해야 함
    enum Texts   { Text }

    // Button 자식 오브젝트 이름 열거형
    enum Buttons { ConfirmBtn }

    /// <summary>
    /// 확인 버튼 클릭 시 실행되는 콜백.
    /// 호출부에서 앱 종료 또는 재시도 로직을 주입한다.
    /// </summary>
    public Action OnConfirm { get; set; }

    protected override void Awake()
    {
        base.Awake();

        // 자식 컴포넌트 바인딩
        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        // 확인 버튼 클릭 시 OnConfirm 콜백 실행
        GetButton((int)Buttons.ConfirmBtn).onClick.AddListener(() => OnConfirm?.Invoke());
    }

    /// <summary>
    /// 팝업에 표시할 점검 안내 메시지를 설정한다.
    /// </summary>
    /// <param name="message">서버 점검 안내 텍스트</param>
    public void SetText(string message)
        => GetText((int)Texts.Text).text = message;
}

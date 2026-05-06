using System;
using UnityEngine;

/// <summary>
/// 클라이언트 강제 업데이트 안내 팝업 UI.
/// 최신 버전 정보를 포함한 업데이트 메시지를 표시하고,
/// 확인 버튼 클릭 시 OnConfirm 콜백(스토어 이동 등)을 실행한다.
/// PopupService.ShowUpdate 를 통해 표시한다.
/// </summary>
public class UI_UpdatePopup : UI_UGUI, IUI_Popup
{
    // TMP_Text 자식 오브젝트 이름 열거형 — 프리팹 자식 이름과 일치해야 함
    enum Texts   { Text }

    // Button 자식 오브젝트 이름 열거형
    enum Buttons { ConfirmBtn }

    /// <summary>
    /// 확인 버튼 클릭 시 실행되는 콜백.
    /// 호출부에서 앱 스토어 링크 열기 등의 로직을 주입한다.
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
    /// 팝업에 표시할 업데이트 안내 메시지를 설정한다.
    /// PopupService 에서 버전 정보를 포함한 문구를 생성하여 전달한다.
    /// </summary>
    /// <param name="message">업데이트 안내 텍스트 (버전 정보 포함)</param>
    public void SetText(string message)
        => GetText((int)Texts.Text).text = message;
}

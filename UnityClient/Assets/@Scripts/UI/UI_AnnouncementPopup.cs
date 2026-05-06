using System;
using UnityEngine;

/// <summary>
/// 서버 공지사항 팝업 UI.
/// 공지 내용을 표시하고 확인 버튼을 누르면 OnConfirm 콜백을 실행한다.
/// PopupService.ShowAnnouncement 를 통해 표시한다.
/// </summary>
public class UI_AnnouncementPopup : UI_UGUI, IUI_Popup
{
    // TMP_Text 자식 오브젝트 이름 열거형 — 프리팹 자식 이름과 일치해야 함
    enum Texts   { Text }

    // Button 자식 오브젝트 이름 열거형
    enum Buttons { ConfirmBtn }

    /// <summary>
    /// 확인 버튼 클릭 시 실행되는 콜백.
    /// PopupService 에서 팝업 닫기(ClosePopupUI)로 기본 설정한다.
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
    /// 팝업에 표시할 공지 내용을 설정한다.
    /// </summary>
    /// <param name="message">공지사항 본문 텍스트</param>
    public void SetText(string message)
        => GetText((int)Texts.Text).text = message;
}

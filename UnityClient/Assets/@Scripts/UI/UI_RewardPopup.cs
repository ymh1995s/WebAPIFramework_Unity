using System;
using UnityEngine;

// 스테이지 클리어 보상 팝업 UI — 보상 내용을 표시하고 확인 버튼으로 닫는다
// PopupService.ShowReward 를 통해 표시한다
public class UI_RewardPopup : UI_UGUI, IUI_Popup
{
    // 프리팹 자식 TMP Text 이름
    enum Texts   { Text }

    // 프리팹 자식 Button 이름
    enum Buttons { ConfirmBtn }

    /// <summary>확인 버튼 클릭 시 실행될 콜백</summary>
    public Action OnConfirm { get; set; }

    protected override void Awake()
    {
        base.Awake();
        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));
        GetButton((int)Buttons.ConfirmBtn).onClick.AddListener(OnClickConfirm);
    }

    // 팝업에 표시할 보상 메시지 설정
    public void SetText(string message)
        => GetText((int)Texts.Text).text = message;

    private void OnClickConfirm()
    {
        UIManager.Instance.ClosePopupUI();
        OnConfirm?.Invoke();
    }
}

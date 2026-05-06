using System;
using UnityEngine;

/// <summary>
/// 이용약관 동의 팝업 UI.
/// 동의(AgreeBtn) 버튼: PlayerPrefs 에 동의 여부 저장 후 팝업을 닫고 OnAgree 콜백을 실행한다.
/// 거부(DisAgreeBtn) 버튼: Application.Quit 으로 앱을 즉시 종료한다.
/// PopupService.ShowTerms 를 통해 표시한다.
/// </summary>
public class UI_TermsPopup : UI_UGUI, IUI_Popup
{
    // TMP_Text 자식 오브젝트 이름 열거형 — 프리팹 자식 이름과 일치해야 함
    enum Texts   { Text }

    // Button 자식 오브젝트 이름 열거형 — 프리팹의 AgreeBtn, DisAgreeBtn 과 일치
    enum Buttons { AgreeBtn, DisAgreeBtn }

    // PlayerPrefs 저장 키 — 약관 동의 여부 영구 보존용
    const string TERMS_ACCEPTED_KEY = "TermsAccepted";

    /// <summary>
    /// 약관 동의 후 실행되는 콜백.
    /// PopupService 에서 로그인 진행 등의 다음 단계를 주입한다.
    /// </summary>
    public Action OnAgree { get; set; }

    protected override void Awake()
    {
        base.Awake();

        // 자식 컴포넌트 바인딩
        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        // 동의 버튼: PlayerPrefs 저장 + 팝업 닫기 + 콜백 실행
        GetButton((int)Buttons.AgreeBtn).onClick.AddListener(OnClickAgree);

        // 거부 버튼: 앱 종료 (약관 미동의 시 서비스 이용 불가)
        GetButton((int)Buttons.DisAgreeBtn).onClick.AddListener(() => Application.Quit());
    }

    /// <summary>
    /// 동의 버튼 클릭 처리.
    /// PlayerPrefs 에 동의 플래그를 저장하고 팝업을 닫은 뒤 OnAgree 콜백을 실행한다.
    /// </summary>
    private void OnClickAgree()
    {
        // 약관 동의 여부를 로컬에 영구 저장하여 재실행 시 팝업 표시 생략
        PlayerPrefs.SetInt(TERMS_ACCEPTED_KEY, 1);
        PlayerPrefs.Save();

        UIManager.Instance.ClosePopupUI();
        OnAgree?.Invoke();
    }
}

using System;
using UnityEngine;

// 단순 확인(버튼 1개) 팝업 공통 UI
// ShowError / ShowAnnouncement / ShowMaintenance / ShowUpdate / ShowReward / ShowBanned 등
// 메시지 표시 + 확인 버튼 콜백 주입이 전부인 팝업에서 공통으로 사용한다
public class UI_ConfirmPopup : UI_UGUI, IUI_Popup
{
    // 프리팹 자식 TMP_Text 이름 열거형 — BackGround/Text 와 일치
    enum Texts   { Text }

    // 프리팹 자식 Button 이름 열거형 — BackGround/ConfirmBtn 과 일치
    enum Buttons { ConfirmBtn }

    // 확인 버튼 클릭 시 실행되는 콜백 — PopupService에서 주입
    public Action OnConfirm { get; set; }

    protected override void Awake()
    {
        base.Awake();

        // 자식 컴포넌트 바인딩
        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        // 확인 버튼 클릭 핸들러 등록
        GetButton((int)Buttons.ConfirmBtn).onClick.AddListener(OnClickConfirm);
    }

    // UIManager 캐싱으로 인해 SetActive(true) 재활성화 시 Awake는 재호출되지 않음
    // OnEnable은 최초 활성화 및 SetActive(true) 재활성화 시 모두 호출되므로
    // 팝업이 열릴 때마다 상태를 초기화하는 진입점으로 사용한다
    protected override void OnEnable()
    {
        base.OnEnable();

        // 버튼 활성 상태 복원 — ShowMaintenance에서 SetButtonActive(false)로 숨긴 채 닫힌 뒤
        // 인스턴스가 재사용될 때 확인 버튼이 없는 상태로 남는 버그 방지
        SetButtonActive(true);

        // 버튼 라벨을 기본값으로 복원 — ShowUpdate 등에서 변경된 라벨이 잔존하는 버그 방지
        // Awake 바인딩 완료 이후 OnEnable이 호출되므로 GetButton 호출은 안전
        SetButtonLabel("확인");
    }

    // 팝업 본문 메시지 설정
    public void SetText(string message)
        => GetText((int)Texts.Text).text = message;

    // 확인 버튼 GameObject 활성/비활성화 — 버튼 없는 팝업(점검 안내 등)에서 사용
    public void SetButtonActive(bool active)
        => GetButton((int)Buttons.ConfirmBtn).gameObject.SetActive(active);

    // 확인 버튼 라벨 변경 — 기본 "확인", ShowUpdate는 "업데이트" 등으로 변경
    public void SetButtonLabel(string label)
    {
        // ConfirmBtn 자식의 TMP_Text를 가져와 라벨 설정
        var tmp = GetButton((int)Buttons.ConfirmBtn)
            .GetComponentInChildren<TMPro.TMP_Text>();
        if (tmp != null)
            tmp.text = label;
    }

    // 확인 버튼 클릭 처리 — UI_Base.GuardReentry로 이중 클릭 방지
    // 팝업을 먼저 닫은 뒤 콜백 실행 — 콜백이 씬 전환/앱 종료를 호출해도 UIManager 스택이 안전하게 유지됨
    private void OnClickConfirm() => GuardReentry(() =>
    {
        // 콜백을 로컬에 캡처한 뒤 팝업 닫기 → 콜백 실행
        var cb = OnConfirm;
        UIManager.Instance.ClosePopupUI();
        cb?.Invoke();
    });
}

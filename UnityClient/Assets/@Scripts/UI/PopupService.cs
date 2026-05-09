using System;
using UnityEngine;

/// <summary>
/// 팝업 표시를 한 곳에서 관리하는 정적 서비스 클래스.
/// UIManager.ShowPopupUI 를 직접 호출하는 대신 이 클래스를 경유하여
/// 팝업 생성·콜백 설정을 일관된 방식으로 처리한다.
/// 단순 확인 팝업은 모두 UI_ConfirmPopup 을 재사용한다.
/// </summary>
public static class PopupService
{
    // 서버 점검 팝업이 이미 표시되었는지 추적하는 플래그
    // — 여러 API 요청이 동시에 503을 받아도 팝업이 중복 표시되지 않도록 방지
    static bool _maintenanceShown;

    // ─────────────────────────────────────────────────────────────────────
    // 오류 팝업
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 오류 메시지를 표시하는 팝업을 열고 반환한다.
    /// 확인 버튼을 누르면 게임을 재시작(RestartGame)한다.
    /// </summary>
    /// <param name="message">사용자에게 표시할 오류 메시지</param>
    public static UI_ConfirmPopup ShowError(string message)
    {
        // 팝업 인스턴스 생성 및 텍스트·콜백 설정
        var p = UIManager.Instance.ShowPopupUI<UI_ConfirmPopup>();
        p.SetText(message);
        p.OnConfirm = RestartGame;
        return p;
    }

    /// <summary>
    /// ApiError 객체를 받아 사용자 친화적 메시지로 오류 팝업을 표시한다.
    /// UserMessage 가 없으면 기본 문구를 사용한다.
    /// </summary>
    /// <param name="error">API 호출에서 반환된 오류 정보</param>
    public static UI_ConfirmPopup ShowError(ApiError error)
        => ShowError(error?.UserMessage ?? "알 수 없는 오류가 발생했습니다.");

    // ─────────────────────────────────────────────────────────────────────
    // 공지 팝업
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 서버 공지사항을 표시하는 팝업을 열고 반환한다.
    /// 확인 버튼을 누르면 팝업을 닫는다 (UI_ConfirmPopup 내부에서 ClosePopupUI 처리).
    /// </summary>
    /// <param name="message">공지 내용</param>
    public static UI_ConfirmPopup ShowAnnouncement(string message)
    {
        var p = UIManager.Instance.ShowPopupUI<UI_ConfirmPopup>();
        p.SetText(message);
        // 확인 시 팝업만 닫음 — OnConfirm = null 이면 ClosePopupUI 호출 후 콜백 생략
        p.OnConfirm = null;
        return p;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 서버 점검 팝업
    // ─────────────────────────────────────────────────────────────────────

    // 표시 중인 점검 팝업 참조 — HideMaintenance 에서 닫을 때 사용
    static UI_ConfirmPopup _maintenancePopup;

    /// <summary>
    /// 서버 점검 안내 팝업을 열고 반환한다.
    /// 이미 표시 중이면 null 을 반환하여 중복 표시를 방지한다.
    /// </summary>
    /// <param name="message">점검 안내 메시지</param>
    /// <param name="onConfirm">확인 버튼 콜백 (앱 종료 등)</param>
    public static UI_ConfirmPopup ShowMaintenance(string message, Action onConfirm)
    {
        // 이미 점검 팝업이 표시된 경우 중복 생성 방지
        if (_maintenanceShown) return null;

        _maintenanceShown = true;
        var p = UIManager.Instance.ShowPopupUI<UI_ConfirmPopup>();
        p.SetText(message);
        p.OnConfirm = onConfirm;
        _maintenancePopup = p;
        return p;
    }

    /// <summary>
    /// 버튼 없는 점검 안내 팝업을 열고 반환한다.
    /// 자동 재시도 흐름에서 사용 — 점검 해제 시 HideMaintenance 로 닫는다.
    /// </summary>
    /// <param name="message">점검 안내 메시지</param>
    public static UI_ConfirmPopup ShowMaintenance(string message)
    {
        // 이미 점검 팝업이 표시된 경우 중복 생성 방지
        if (_maintenanceShown) return null;

        _maintenanceShown = true;
        var p = UIManager.Instance.ShowPopupUI<UI_ConfirmPopup>();
        p.SetText(message);
        // 버튼 없는 팝업 — 자동 재시도 성공 시 HideMaintenance 로 프로그래밍 방식으로 닫음
        p.SetButtonActive(false);
        p.OnConfirm = null;
        _maintenancePopup = p;
        return p;
    }

    /// <summary>
    /// 현재 표시 중인 점검 팝업을 프로그래밍 방식으로 닫는다.
    /// 자동 재시도 성공 후 부팅 흐름을 재개하기 전에 호출한다.
    /// 팝업이 없으면 무시한다.
    /// </summary>
    public static void HideMaintenance()
    {
        if (_maintenancePopup == null) return;

        // UIManager 스택 최상단이 점검 팝업인 경우에만 ClosePopupUI 호출
        // — 다른 팝업이 최상단에 쌓여 있으면 닫지 않고 참조만 해제
        var top = UIManager.Instance.GetLastPopupUI<UI_ConfirmPopup>();
        if (top == _maintenancePopup)
            UIManager.Instance.ClosePopupUI();

        _maintenancePopup = null;
        _maintenanceShown  = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 업데이트 팝업
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 클라이언트 강제 업데이트 안내 팝업을 열고 반환한다.
    /// 확인 버튼 라벨을 "업데이트"로 변경하고, 콜백에서 스토어 링크 이동 등을 처리한다.
    /// </summary>
    /// <param name="latestVersion">서버가 요구하는 최신 버전 문자열 (예: "1.2.3")</param>
    /// <param name="onConfirm">확인 버튼 콜백 (스토어 이동 등)</param>
    public static UI_ConfirmPopup ShowUpdate(string latestVersion, Action onConfirm)
    {
        var p = UIManager.Instance.ShowPopupUI<UI_ConfirmPopup>();
        // 버전 정보를 포함한 안내 문구를 자동 생성
        p.SetText($"새 버전({latestVersion})이 있습니다. 업데이트 후 이용해주세요.");
        // 업데이트 팝업은 버튼 라벨을 "업데이트"로 변경
        p.SetButtonLabel("업데이트");
        p.OnConfirm = onConfirm;
        return p;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 보상 팝업
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 스테이지 클리어 보상 팝업을 열고 반환한다.
    /// 확인 버튼 콜백에서 씬 전환 등을 처리한다.
    /// </summary>
    /// <param name="message">보상 내용 메시지</param>
    /// <param name="onConfirm">확인 버튼 콜백</param>
    public static UI_ConfirmPopup ShowReward(string message, Action onConfirm)
    {
        var p = UIManager.Instance.ShowPopupUI<UI_ConfirmPopup>();
        p.SetText(message);
        p.OnConfirm = onConfirm;
        return p;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 밴(계정 정지) 팝업
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 계정 정지 안내 팝업을 열고 반환한다.
    /// 확인 버튼을 누르면 게임을 재시작(RestartGame)한다.
    /// </summary>
    /// <param name="message">정지 사유 메시지</param>
    public static UI_ConfirmPopup ShowBanned(string message)
    {
        var p = UIManager.Instance.ShowPopupUI<UI_ConfirmPopup>();
        p.SetText(message);
        p.OnConfirm = RestartGame;
        return p;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 약관 동의 팝업
    // ─────────────────────────────────────────────────────────────────────

    // 이용약관 동의 팝업 — ShowSelect 재사용, 동의 시 PlayerPrefs 저장 후 콜백 실행
    public static UI_WithdrawPopup ShowTerms(Action onConfirm)
    {
        return ShowSelect(
            "서비스 이용을 위해 이용약관에 동의해주세요.\n\n동의하지 않으시면 앱이 종료됩니다.",
            onOk: () =>
            {
                // 약관 동의 여부 로컬 저장 — 재실행 시 팝업 생략용
                PlayerPrefs.SetInt("TermsAccepted", 1);
                PlayerPrefs.Save();
                onConfirm?.Invoke();
            },
            onCancel: () =>
            {
#if UNITY_EDITOR
                // 에디터에서는 종료 대신 로그만 출력
                Debug.Log("[약관 비동의] 에디터 환경에서는 게임을 종료하지 않습니다.");
#else
                Application.Quit();
#endif
            }
        );
    }

    // ─────────────────────────────────────────────────────────────────────
    // 선택(Ok/Cancel) 팝업
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 확인/취소 선택지를 제공하는 팝업을 열고 반환한다.
    /// 각 버튼 콜백에서 팝업을 닫은 후 인자로 받은 액션을 실행한다.
    /// </summary>
    /// <param name="message">선택 안내 메시지</param>
    /// <param name="onOk">확인 버튼 콜백</param>
    /// <param name="onCancel">취소 버튼 콜백</param>
    public static UI_WithdrawPopup ShowSelect(string message, Action onOk, Action onCancel)
    {
        var p = UIManager.Instance.ShowPopupUI<UI_WithdrawPopup>();
        p.SetText(message);
        p.OnOk = onOk;
        p.OnCancel = onCancel;
        return p;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 유틸리티
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 서버 점검 팝업 중복 방지 플래그를 초기화한다.
    /// BootstrapScene 진입 시 반드시 호출하여 재접속 후 팝업이 정상 표시되도록 한다.
    /// </summary>
    public static void ClearMaintenanceFlag() => _maintenanceShown = false;

    /// <summary>
    /// 게임을 재시작한다.
    /// 에디터에서는 플레이 모드를 종료하고, 빌드에서는 BootstrapScene 으로 이동한다.
    /// UI 스택을 먼저 정리하여 씬 전환 중 참조 오류를 방지한다.
    /// </summary>
    private static void RestartGame()
    {
#if UNITY_EDITOR
        // 에디터 환경: 플레이 모드 종료로 재시작 시뮬레이션
        UIManager.Instance.Clear();
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 빌드 환경: BootstrapScene 으로 씬 전환하여 초기화 흐름 재실행
        UIManager.Instance.Clear();
        SceneManager.Instance.LoadScene(Define.EScene.BootstrapScene);
#endif
    }
}

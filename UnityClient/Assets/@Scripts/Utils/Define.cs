using UnityEngine;

public static class Define
{
    // 씬 식별자 열거형 — SceneManager.LoadScene 인자로 사용
    public enum EScene
    {
        Unknown,
        LoginScene,
        MainScene,
        GameScene,
        BootstrapScene,    // 앱 부팅 최초 진입 씬 (버전체크/자동로그인)
        StageSelectScene,  // 스테이지 목록 씬
        LoadingScene,      // 씬 전환 중 로딩 화면 (페이드 인/아웃 전용)
    }

    // 전역 이벤트 식별자 열거형 — EventManager.TriggerEvent 인자로 사용
    public enum EEventType
    {
        None,
        GoldChanged,
        LanguageChanged,
        LoginSuccess,            // 로그인 성공 시 발행
        MaintenanceDetected,     // 503 점검 응답 감지 시 발행
        SessionExpired,          // 401 토큰 갱신 실패 시 발행 (강제 로그아웃)
    }

    public enum ESound
    {
        Bgm,
        Effect,

        MaxCount
    }

    public enum ELanguage
    {
        KOR,
        ENG,
    }

	public enum EAnimation
	{
		b_wait,
		b_walk,
		f_wait,
		f_walk
	}

	public enum ECatState
	{
		Idle,
		Move,
		Work
	}

    /// <summary>
    /// BusyMask 적용 범위 — BeginBusy 호출 시 어떤 수준의 입력 차단을 원하는지 구분
    /// </summary>
    public enum EBusyScope
    {
        /// <summary>전화면 반투명 마스크로 모든 입력을 차단 (API 호출 등 전역 대기)</summary>
        Global,
        /// <summary>개별 버튼만 비활성화 (글로벌 마스크 없이 버튼 단위 차단 — Phase 1에서 활용)</summary>
        Button,
        /// <summary>입력 차단 없이 재진입만 방지 (카운터만 증가)</summary>
        None,
    }
}

// PlayerPrefs 키 상수 — 매직 스트링 제거 및 키 중복 방지
public static class PlayerPrefsKey
{
    // 마지막으로 본 공지 ID — 동일 공지 재표시 차단용
    public const string LastSeenNoticeId = "LastSeenNoticeId";

    // 약관 동의 완료 여부 — 1이면 동의 완료, 미설정/0이면 미동의
    public const string TermsAccepted = "TermsAccepted";
}

// UI Canvas sortingOrder 계층 — 레이어 충돌 회피를 위해 한 곳에서 관리
// PopupStart(100) < Hud(500) < Toast(999) = Fade(999) < BusyMask(1000)
public static class UISortingOrder
{
    public const int PopupStart = 100;   // UIManager 팝업 시작 sortingOrder
    public const int Hud        = 500;   // ShoutManager HUD
    public const int Fade       = 999;   // FadeManager 페이드 인/아웃
    public const int Toast      = 999;   // UIManager 토스트 알림
    public const int BusyMask   = 1000;  // UIManager 전화면 입력 차단 마스크
}

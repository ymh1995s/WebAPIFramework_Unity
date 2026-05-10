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
}

// PlayerPrefs 키 상수 — 매직 스트링 제거 및 키 중복 방지
public static class PlayerPrefsKey
{
    // 마지막으로 본 공지 ID — 동일 공지 재표시 차단용
    public const string LastSeenNoticeId = "LastSeenNoticeId";
}

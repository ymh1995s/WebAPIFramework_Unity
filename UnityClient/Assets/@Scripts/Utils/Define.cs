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

using System;
using UnityEngine;

// 앱 생명주기(포그라운드/백그라운드 전환)를 관리하는 매니저
// Singleton<T>를 상속하므로 씬 전환 이후에도 단일 인스턴스가 유지된다
public class AppLifecycleManager : Singleton<AppLifecycleManager>
{
    // 세션 만료 이벤트 — AuthManager 등 외부 모듈이 발행하면 이 매니저가 처리한다
    public static event Action OnSessionExpired;

    // 첫 번째 Resume(앱 최초 포그라운드 진입)은 무시하기 위한 플래그
    // OnApplicationPause(false)는 앱 시작 직후에도 한 번 호출되므로 이를 걸러낸다
    private bool _firstResumeDone = false;

    // BootstrapFlow 재진입 방지 플래그 — 복귀 처리 중 중복 실행을 막는다
    private bool _resuming = false;

    // 세션 만료 이벤트 구독 등록
    // Singleton<T>에 virtual Awake()가 없으므로 private void Awake()로 선언한다
    private void Awake()
    {
        OnSessionExpired += HandleSessionExpired;
    }

    // 세션 만료 이벤트 구독 해제 — 오브젝트 파괴 시 반드시 정리
    private void OnDestroy()
    {
        OnSessionExpired -= HandleSessionExpired;
    }

    // OnApplicationPause: pause=false이면 포그라운드 복귀, true이면 백그라운드 진입
    private void OnApplicationPause(bool pause)
    {
        // 백그라운드 진입은 처리하지 않는다
        if (pause) return;

        // 앱 최초 기동 직후의 Resume 호출은 무시한다
        if (!_firstResumeDone)
        {
            _firstResumeDone = true;
            return;
        }

        // 로그인 상태가 아니면 부팅 흐름을 재실행하지 않는다
        if (!AuthManager.Instance.IsLoggedIn) return;

        // 이미 복귀 처리 중이면 중복 실행하지 않는다
        if (_resuming) return;

        // 백그라운드에서 포그라운드로 복귀한 경우 부팅 흐름을 재실행한다
        // (토큰 갱신, 서버 점검 재확인 등 부팅 시 처리와 동일한 흐름 적용)
        Debug.Log("[AppLifecycleManager] 앱 포그라운드 복귀 — BootstrapFlow 재실행");
        _resuming = true;
        try
        {
            BootstrapFlow.Run();
        }
        finally
        {
            _resuming = false;
        }
    }

    // 세션 만료 이벤트 핸들러 — 인증 정보를 초기화하고 로그인 씬으로 이동하여 재인증을 유도한다
    private void HandleSessionExpired()
    {
        Debug.Log("[AppLifecycleManager] 세션 만료 감지 — 로그인 씬으로 이동");
        // 로컬 인증 정보를 먼저 초기화한다
        AuthManager.Instance.Clear();
        SceneManager.Instance.LoadScene(Define.EScene.LoginScene);
    }

    // 외부에서 세션 만료를 알릴 때 호출하는 정적 헬퍼
    public static void NotifySessionExpired()
    {
        OnSessionExpired?.Invoke();
    }
}

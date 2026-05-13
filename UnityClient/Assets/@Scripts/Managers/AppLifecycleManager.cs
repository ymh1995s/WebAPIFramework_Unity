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

    // 백그라운드 진입 시각(UTC) — 복귀 시 경과 시간 계산에 사용
    private DateTime _pausedAtUtc;

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

    // OnApplicationPause: pause=true이면 백그라운드 진입, false이면 포그라운드 복귀
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            // 백그라운드 진입 시각을 기록한다
            _pausedAtUtc = DateTime.UtcNow;
            return;
        }

        // 앱 최초 기동 직후의 Resume 호출은 무시한다
        if (!_firstResumeDone)
        {
            _firstResumeDone = true;
            return;
        }

        // 로그인 상태가 아니면 복귀 흐름을 실행하지 않는다
        if (!AuthManager.Instance.IsLoggedIn) return;

        // 백그라운드 경과 시간이 임계값 미만이면 갱신을 생략한다
        double elapsedSec = (DateTime.UtcNow - _pausedAtUtc).TotalSeconds;
        if (elapsedSec < GameConfig.ResumeThresholdSec)
        {
            Debug.Log($"[AppLifecycleManager] 백그라운드 경과 {elapsedSec:F0}초 — 임계값({GameConfig.ResumeThresholdSec}초) 미만, 갱신 생략");
            return;
        }

        // 임계값 이상 백그라운드에 있었으면 복귀 흐름(토큰 갱신)을 실행한다
        // 재진입 가드는 AppResumeFlow 내부에서 관리한다
        Debug.Log($"[AppLifecycleManager] 앱 포그라운드 복귀 — 경과 {elapsedSec:F0}초, AppResumeFlow 실행");
        AppResumeFlow.Run();
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

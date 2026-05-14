using System;
using UnityEngine;

// 앱 생명주기(포그라운드/백그라운드 전환)를 관리하는 매니저
// Singleton<T>를 상속하므로 씬 전환 이후에도 단일 인스턴스가 유지된다
public class AppLifecycleManager : Singleton<AppLifecycleManager>
{
    // 로그인 상태 로컬 캐시 — EventManager 구독으로 동기화하여 AuthManager 직접 참조 제거
    private bool _isLoggedIn;

    // 첫 번째 Resume(앱 최초 포그라운드 진입)은 무시하기 위한 플래그
    // OnApplicationPause(false)는 앱 시작 직후에도 한 번 호출되므로 이를 걸러낸다
    private bool _firstResumeDone;

    // 백그라운드 진입 시각(UTC) — 복귀 시 경과 시간 계산에 사용
    private DateTime _pausedAtUtc;

    // EventManager 이벤트 구독 등록
    private void Awake()
    {
        EventManager.Instance.AddEvent(Define.EEventType.LoginSuccess, OnLoginSuccess);
        EventManager.Instance.AddEvent(Define.EEventType.Logout, OnLogout);
        EventManager.Instance.AddEvent(Define.EEventType.SessionExpired, HandleSessionExpired);
    }

    // EventManager 이벤트 구독 해제 — 오브젝트 파괴 시 반드시 정리
    // private이 아닌 protected override로 선언해야 Singleton 베이스의 _instance = null 처리가 실행된다
    protected override void OnDestroy()
    {
        EventManager.Instance.RemoveEvent(Define.EEventType.LoginSuccess, OnLoginSuccess);
        EventManager.Instance.RemoveEvent(Define.EEventType.Logout, OnLogout);
        EventManager.Instance.RemoveEvent(Define.EEventType.SessionExpired, HandleSessionExpired);
        base.OnDestroy();
    }

    // 로그인 성공 이벤트 핸들러 — 로컬 로그인 상태 캐시 갱신
    private void OnLoginSuccess(object _) => _isLoggedIn = true;

    // 로그아웃 이벤트 핸들러 — 로컬 로그인 상태 캐시 초기화
    private void OnLogout() => _isLoggedIn = false;

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
        if (!_isLoggedIn) return;

        // 백그라운드 경과 시간이 임계값 미만이면 갱신을 생략한다
        double elapsedSec = (DateTime.UtcNow - _pausedAtUtc).TotalSeconds;
        if (elapsedSec < GameConfig.ResumeThresholdSec)
        {
            Debug.Log($"[AppLifecycleManager] 백그라운드 경과 {elapsedSec:F0}초 — 임계값({GameConfig.ResumeThresholdSec}초) 미만, 갱신 생략");
            return;
        }

        // 임계값 이상 백그라운드에 있었으면 복귀 흐름(토큰 갱신)을 실행한다
        Debug.Log($"[AppLifecycleManager] 앱 포그라운드 복귀 — 경과 {elapsedSec:F0}초, AppResumeFlow 실행");
        AppResumeFlow.Run();
    }

    // 세션 만료 이벤트 핸들러 — 로그인 씬으로 이동하여 재인증을 유도한다
    // 토큰 초기화는 AuthManager가 SessionExpired를 구독하여 스스로 수행
    private void HandleSessionExpired()
    {
        Debug.Log("[AppLifecycleManager] 세션 만료 감지 — 로그인 씬으로 이동");
        _isLoggedIn = false;
        SceneManager.Instance.LoadScene(Define.EScene.LoginScene);
    }
}

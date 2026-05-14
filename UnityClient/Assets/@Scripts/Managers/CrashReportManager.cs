using UnityEngine;
using UnityEngine.Diagnostics;

// Unity Engine Diagnostics 크래시 수집 매니저
// 역할: 앱 크래시 발생 시 자동으로 보고서를 Unity Cloud로 전송하고,
//       로그인 상태에 따라 PlayerId 메타데이터를 갱신하여 어느 플레이어에서 크래시가 났는지 추적한다.
public class CrashReportManager : Singleton<CrashReportManager>
{
    // Init()이 중복 호출되더라도 이벤트 이중 구독이 발생하지 않도록 방어하는 플래그
    bool _initialized;

    // 부팅 흐름 최상단에서 1회 호출 — 크래시 수집 활성화 및 정적 메타데이터 등록
    public void Init()
    {
        // 중복 초기화 방지 — BootstrapFlow가 여러 번 Run()되더라도 1회만 설정
        if (_initialized) return;
        _initialized = true;

        // 에디터에서는 CrashReportHandler가 작동하지 않으므로 빌드에서만 활성화
        // 에디터 환경에서 호출하면 아무 효과가 없거나 경고가 발생할 수 있다
#if !UNITY_EDITOR
        CrashReportHandler.enableCaptureExceptions = true;
#endif

        // 기기/빌드 정보를 크래시 보고서에 정적 메타데이터로 첨부
        // — 크래시 발생 후 보고서만 봐도 어떤 환경에서 재현됐는지 즉시 파악 가능
        SetUserMetadata("buildVersion", Application.version);
        SetUserMetadata("unityVersion", Application.unityVersion);
        SetUserMetadata("platform",     Application.platform.ToString());
        SetUserMetadata("deviceModel",  SystemInfo.deviceModel);
        // deviceUniqueIdentifier: 기기를 구분하는 고유값 (iOS는 IDFV 기반)
        SetUserMetadata("deviceId",     SystemInfo.deviceUniqueIdentifier);
        SetUserMetadata("installMode",  Application.installMode.ToString());

        // 로그인/로그아웃 이벤트 구독 — EventManager 경유로 PlayerId 메타데이터를 실시간으로 갱신
        EventManager.Instance.AddEvent(Define.EEventType.LoginSuccess, OnLoginSuccess);
        EventManager.Instance.AddEvent(Define.EEventType.Logout, OnLogout);

        // Unity Cloud 프로젝트가 연결되지 않으면 크래시 보고서가 전송되지 않는다
        // — 로컬 빌드 테스트 시 흔히 빠뜨리는 설정이므로 빠른 발견을 위해 경고 출력
        if (string.IsNullOrEmpty(Application.cloudProjectId))
        {
            Debug.LogWarning("[CrashReport] Unity Cloud 프로젝트 미연결 — Project Settings → Services에서 연결 필요");
        }
        else
        {
            Debug.Log($"[CrashReport] 활성화 완료. projectId={Application.cloudProjectId}");
        }
    }

    // CrashReportHandler에 키-값 메타데이터를 등록하는 내부 헬퍼
    // — 에디터에서는 API가 동작하지 않으므로 빌드에서만 호출한다
    void SetUserMetadata(string key, string value)
    {
#if !UNITY_EDITOR
        CrashReportHandler.SetUserMetadata(key, value);
#endif
    }

    // 로그인 성공 핸들러 — PlayerId를 크래시 보고서에 첨부
    // 크래시 발생 시 어느 플레이어 계정에서 발생했는지 즉시 확인할 수 있다
    void OnLoginSuccess(object payload)
    {
        string playerId = (string)payload;
        SetUserMetadata("playerId", playerId);
    }

    // 로그아웃 핸들러 — PlayerId 메타데이터를 비워 이전 세션 정보가 남지 않도록 초기화
    void OnLogout()
    {
        SetUserMetadata("playerId", string.Empty);
    }

    // 오브젝트 파괴 시 이벤트 구독 해제 — 좀비 핸들러로 인한 NullReferenceException 방지
    protected override void OnDestroy()
    {
        EventManager.Instance.RemoveEvent(Define.EEventType.LoginSuccess, OnLoginSuccess);
        EventManager.Instance.RemoveEvent(Define.EEventType.Logout, OnLogout);
        base.OnDestroy();
    }
}

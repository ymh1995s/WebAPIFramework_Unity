using UnityEngine;

// 앱 부팅 흐름 정적 헬퍼 — ResourceManager 프리로드 완료 후 Run() 1회 호출
// 흐름: [1]버전체크 → [2]공지확인 → [3]자동로그인 → [4]약관·LoginScene
public static class BootstrapFlow
{
    // 점검 이벤트 중복 구독 방지 플래그
    static bool _subscribed;

    // 부팅 흐름 시작 — Bootstrap_Scene.Start의 LoadAll 콜백에서 호출
    public static async void Run()
    {
        // 씬 재진입 시 점검 팝업 플래그 초기화
        PopupService.ClearMaintenanceFlag();
        SubscribeMaintenanceEvent();

        // [1] 버전 체크 — 점검(503) 또는 강제 업데이트 시 흐름 중단
        bool versionOk = await CheckVersionAsync();
        if (!versionOk) return;

        // [2] 최신 공지 확인 — 실패해도 무시하고 계속 진행
        await CheckNoticeAsync();

        // [3] 저장된 RefreshToken으로 자동 로그인 시도
        AuthManager.Instance.LoadSavedToken();
        bool autoLoginSuccess = await TryAutoLoginAsync();
        if (autoLoginSuccess) return;

        // [4] 약관 동의 확인 후 LoginScene 진입
        ProceedToLogin();
    }

    // 서버 점검(503) 이벤트 구독 — 중복 구독 방지
    static void SubscribeMaintenanceEvent()
    {
        if (_subscribed) return;
        _subscribed = true;
        EventManager.Instance.AddEvent(
            Define.EEventType.MaintenanceDetected,
            OnMaintenanceDetected
        );
    }

    // 씬 전환 전 이벤트 구독 해제 — 메모리 누수 및 좀비 핸들러 방지
    static void UnsubscribeMaintenanceEvent()
    {
        if (!_subscribed) return;
        EventManager.Instance.RemoveEvent(
            Define.EEventType.MaintenanceDetected,
            OnMaintenanceDetected
        );
        _subscribed = false;
    }

    // 503 점검 이벤트 핸들러 — 점검 팝업 표시 후 앱 종료
    static void OnMaintenanceDetected()
    {
        PopupService.ShowMaintenance(
            "서버 점검 중입니다.\n잠시 후 다시 이용해주세요.",
            QuitApplication
        );
    }

    // [1] 버전 체크 — isForceUpdate이면 업데이트 팝업, 503이면 점검 인터셉터가 처리
    static async Awaitable<bool> CheckVersionAsync()
    {
        bool completed = false;
        bool passed    = false;

        VersionApi.Instance.Check(
            Application.version,
            onSuccess: res =>
            {
                if (res.isForceUpdate)
                {
                    // 강제 업데이트 팝업 — 확인 버튼 콜백에서 진행 허용
                    PopupService.ShowUpdate(res.latestVersion, () =>
                    {
                        Debug.Log($"[Bootstrap] 업데이트 안내 확인 — 최신버전: {res.latestVersion}");
                        passed    = true;
                        completed = true;
                    });
                    // 팝업 닫힐 때까지 아래 루프에서 대기
                }
                else
                {
                    // 최신 버전 — 정상 진행
                    passed    = true;
                    completed = true;
                }
            },
            onError: err =>
            {
                if (err.Status == 503)
                {
                    // 503 점검 — 인터셉터가 MaintenanceDetected 이벤트로 처리, 흐름 중단
                    passed    = false;
                    completed = true;
                }
                else
                {
                    // 그 외 오류는 경고만 남기고 진행 허용
                    Debug.LogWarning($"[Bootstrap] 버전 체크 오류 무시: {err.UserMessage}");
                    passed    = true;
                    completed = true;
                }
            }
        );

        // 콜백 완료 대기 — 매 프레임 체크
        while (!completed)
            await Awaitable.NextFrameAsync();

        return passed;
    }

    // [2] 최신 공지 확인 — 이미 본 공지(LastSeenNoticeId)는 스킵
    static async Awaitable CheckNoticeAsync()
    {
        bool completed = false;

        NoticeApi.Instance.GetLatest(
            onSuccess: notice =>
            {
                // 204 응답(활성 공지 없음) 또는 id 0 — 스킵
                if (notice == null || notice.id == 0)
                {
                    completed = true;
                    return;
                }

                // 이미 확인한 공지 — 스킵
                int lastSeenId = PlayerPrefs.GetInt("LastSeenNoticeId", 0);
                if (notice.id == lastSeenId)
                {
                    completed = true;
                    return;
                }

                // 신규 공지 — 팝업 표시 후 확인 ID 저장
                PopupService.ShowAnnouncement(notice.content);
                PlayerPrefs.SetInt("LastSeenNoticeId", notice.id);
                PlayerPrefs.Save();
                completed = true;
            },
            onError: _ =>
            {
                // 공지 조회 실패는 무시하고 진행
                completed = true;
            }
        );

        while (!completed)
            await Awaitable.NextFrameAsync();
    }

    // [3] 자동 로그인 — 저장된 RefreshToken으로 Access Token 재발급 후 MainScene 진입
    static async Awaitable<bool> TryAutoLoginAsync()
    {
        // RefreshToken이 없으면 자동 로그인 불가
        if (!AuthManager.Instance.IsLoggedIn)
            return false;

        bool completed = false;
        bool success   = false;

        AuthApi.Instance.Refresh(
            AuthManager.Instance.RefreshToken,
            onSuccess: token =>
            {
                AuthManager.Instance.SaveToken(token);
                Debug.Log("[Bootstrap] 자동 로그인 성공");
                success   = true;
                completed = true;
            },
            onError: err =>
            {
                // 토큰 만료 등 갱신 실패 — 저장된 토큰 초기화 후 로그인 화면으로
                Debug.LogWarning($"[Bootstrap] 자동 로그인 실패: {err.UserMessage}");
                AuthManager.Instance.Clear();
                completed = true;
            }
        );

        while (!completed)
            await Awaitable.NextFrameAsync();

        if (success)
        {
            // 자동 로그인 성공 — 구독 해제 후 MainScene 전환
            UnsubscribeMaintenanceEvent();
            SceneManager.Instance.LoadScene(Define.EScene.MainScene);
        }

        return success;
    }

    // [4] 약관 동의 여부에 따라 팝업 또는 즉시 LoginScene 진입
    static void ProceedToLogin()
    {
        if (PlayerPrefs.GetInt("TermsAccepted", 0) == 0)
        {
            // 첫 실행 — 약관 동의 팝업 표시, 동의 시 LoginScene으로 전환
            PopupService.ShowTerms(() =>
            {
                UnsubscribeMaintenanceEvent();
                SceneManager.Instance.LoadScene(Define.EScene.LoginScene);
            });
        }
        else
        {
            // 이미 약관 동의 완료 — 바로 LoginScene으로 전환
            UnsubscribeMaintenanceEvent();
            SceneManager.Instance.LoadScene(Define.EScene.LoginScene);
        }
    }

    // 플랫폼별 앱 종료 — 에디터에서는 플레이 모드 종료, 빌드에서는 Application.Quit
    static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

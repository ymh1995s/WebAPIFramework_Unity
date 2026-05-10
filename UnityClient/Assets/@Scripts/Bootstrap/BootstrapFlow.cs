using UnityEngine;

// 앱 부팅 흐름 정적 헬퍼 — ResourceManager 프리로드 완료 후 Run() 1회 호출
// 흐름: [1]버전체크 → [2]공지확인 → [3]자동로그인 → [4]약관·LoginScene
public static class BootstrapFlow
{
    // 점검 이벤트 중복 구독 방지 플래그
    static bool _subscribed;

    // 점검 자동 재시도 루프 중복 실행 방지 플래그
    static bool _pollingActive;

    // 부팅 흐름 시작 — Bootstrap_Scene.Start의 LoadAll 콜백에서 호출
    public static async void Run()
    {
        // 씬 재진입 시 점검 팝업 플래그 초기화
        PopupService.ClearMaintenanceFlag();
        // ApiClient의 503 중복 방지 플래그도 함께 리셋 — 재점검 시 이벤트가 재발행되도록 보장
        ApiClient.ResetMaintenanceFlag();
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
        // 구독 해제 시 폴링도 함께 중단
        _pollingActive = false;
    }

    // 503 점검 이벤트 핸들러 — 버튼 없는 팝업 표시 후 자동 재시도 루프 시작
    static void OnMaintenanceDetected()
    {
        // 버튼 없는 점검 안내 팝업 표시 — 재시도 성공 시 HideMaintenance 로 닫음
        PopupService.ShowMaintenance("서버 점검 중입니다.\n잠시 후 자동으로 재시도합니다.");
        // 30초마다 서버 상태를 자동으로 재확인하는 폴링 루프 시작
        StartMaintenancePollingAsync();
    }

    // 점검 자동 재시도 — 30초 간격으로 버전 체크 API를 호출해 점검 해제를 감지
    // 200 응답 시 팝업을 닫고 부팅 흐름을 재개, 503이면 대기 반복
    static async void StartMaintenancePollingAsync(int intervalSec = 30)
    {
        // 중복 폴링 방지 — 이미 실행 중이면 즉시 반환
        if (_pollingActive) return;
        _pollingActive = true;

        while (_pollingActive)
        {
            // intervalSec 초 대기 (매 프레임 체크로 대기)
            float elapsed = 0f;
            while (elapsed < intervalSec && _pollingActive)
            {
                await Awaitable.NextFrameAsync();
                elapsed += Time.deltaTime;
            }

            if (!_pollingActive) break;

            // 서버 상태 재확인
            bool resolved = false;
            bool completed = false;

            VersionApi.Instance.Check(
                Application.version,
                onSuccess: _ =>
                {
                    // 200 응답 — 점검 해제됨
                    resolved  = true;
                    completed = true;
                },
                onError: err =>
                {
                    // 503 이외의 오류도 점검 미해제로 간주하고 계속 대기
                    resolved  = false;
                    completed = true;
                }
            );

            while (!completed)
                await Awaitable.NextFrameAsync();

            if (resolved)
            {
                // 점검 해제 — 팝업 닫고 부팅 흐름 재개
                _pollingActive = false;
                PopupService.HideMaintenance();
                Run();
                return;
            }
            // 아직 점검 중 — 다음 대기 주기로 계속
        }
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
                    // 강제 업데이트 — 스토어로 이동 후 앱 종료, 진행 차단
                    PopupService.ShowUpdate(res.latestVersion, () =>
                    {
                        Debug.Log($"[Bootstrap] 강제 업데이트 — 스토어 이동: {res.latestVersion}");
#if UNITY_ANDROID
                        Application.OpenURL(GameConfig.ANDROID_STORE_URL);
#elif UNITY_IOS
                        Application.OpenURL(GameConfig.IOS_STORE_URL);
#endif
                        Application.Quit();
                    });
                    // passed = false 유지, completed = true — 대기 루프 탈출 후 흐름 중단
                    completed = true;
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
                int lastSeenId = PlayerPrefs.GetInt(PlayerPrefsKey.LastSeenNoticeId, 0);
                if (notice.id == lastSeenId)
                {
                    completed = true;
                    return;
                }

                // 신규 공지 — 팝업 표시 후 확인 ID 저장
                PopupService.ShowAnnouncement(notice.content);
                PlayerPrefs.SetInt(PlayerPrefsKey.LastSeenNoticeId, notice.id);
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
        bool banned    = false;

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
                AuthManager.Instance.Clear();
                if (err.ErrorCode == "AUTH_BANNED")
                {
                    // 밴 계정 안내 팝업 — OK 클릭 시 게임 재시작
                    banned = true;
                    PopupService.ShowBanned(err.UserMessage ?? "정지된 계정입니다.");
                }
                else
                {
                    // 토큰 만료 등 갱신 실패 — 조용히 로그인 화면으로 이동
                    Debug.LogWarning($"[Bootstrap] 자동 로그인 실패: {err.UserMessage}");
                }
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
        else if (banned)
        {
            // 밴 팝업 표시 중 — LoginScene 전환 없이 종료 (팝업 OK → RestartGame)
            UnsubscribeMaintenanceEvent();
        }

        // banned 반환 시 true → Run()의 ProceedToLogin() 호출 방지
        return success || banned;
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

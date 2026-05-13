using System.Threading.Tasks;
using UnityEngine;

// 백그라운드 복귀 흐름 정적 헬퍼
// 책임: 씬 전환 없이 토큰만 조용히 갱신한다.
// AppLifecycleManager가 임계값(GameConfig.ResumeThresholdSec) 판정 후 이 클래스를 호출한다.
public static class AppResumeFlow
{
    // 재진입 방지 플래그 — 복귀 처리 중 중복 실행을 막는다
    static bool _resuming;

    // 백그라운드 복귀 흐름 진입점 — AppLifecycleManager.OnApplicationPause(false)에서 호출
    public static async void Run()
    {
        // 이미 복귀 처리 중이면 즉시 반환 (재진입 가드)
        if (_resuming)
        {
            Debug.Log("[AppResumeFlow] 이미 복귀 처리 중 — 중복 호출 무시");
            return;
        }

        _resuming = true;
        try
        {
            await RunAsync();
        }
        finally
        {
            // 성공·실패 모두 플래그 해제
            _resuming = false;
        }
    }

    // 실제 갱신 흐름 — RefreshToken으로 토큰 재발급 시도
    static async Task RunAsync()
    {
        Debug.Log("[AppResumeFlow] 백그라운드 복귀 — 토큰 갱신 시도");

        var result = await AuthApi.RefreshAsync(AuthManager.Instance.RefreshToken);

        if (result.IsSuccess)
        {
            // 갱신 성공 — 씬/UI 변경 없이 인메모리 토큰만 교체
            AuthManager.Instance.SaveToken(result.Value);
            Debug.Log("[AppResumeFlow] 토큰 갱신 성공 — 현재 씬 유지");
            return;
        }

        // ── 실패 분기 처리 ────────────────────────────────────────────────
        var err = result.Error;

        // 서버 점검(503) — ApiClient 인터셉터가 MaintenanceDetected 이벤트로 자동 처리
        if (err.Status == 503)
        {
            Debug.Log("[AppResumeFlow] 503 점검 응답 — ApiClient 인터셉터가 처리");
            return;
        }

        // 밴 계정(AUTH_BANNED) — 인증 정보 초기화 후 밴 안내 팝업 표시
        if (err.ErrorCode == "AUTH_BANNED")
        {
            Debug.LogWarning("[AppResumeFlow] 계정 정지 감지 — 인증 정보 초기화 후 밴 팝업 표시");
            AuthManager.Instance.Clear();
            PopupService.ShowBanned(err.UserMessage ?? "정지된 계정입니다.");
            return;
        }

        // 그 외 실패(토큰 만료 등) — 인증 정보 초기화 후 세션 만료 안내 팝업 표시
        // ShowError의 OK 콜백은 RestartGame(BootstrapScene 재진입)으로 연결되어 있다
        Debug.LogWarning($"[AppResumeFlow] 토큰 갱신 실패 — 세션 만료 처리: {err.UserMessage}");
        AuthManager.Instance.Clear();
        PopupService.ShowError("세션이 만료되었습니다. 다시 로그인해주세요.");
    }
}

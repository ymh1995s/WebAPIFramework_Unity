using System;
using System.Threading.Tasks;

// 일일 출석 API 싱글톤 — 매일 1회 출석 보상 처리
public class DailyLoginApi : Singleton<DailyLoginApi>
{
    // ── Task 기반 비동기 정적 메서드 (await 호출용) ──────────────────────────

    // 출석 처리 비동기 버전 — 요청 본문 없는 POST, 이미 출석 시 rewarded=false 응답
    public static Task<ApiResult<DailyLoginResponse>> ProcessAsync()
        => ApiClient.Instance.PostAsync<DailyLoginResponse>(ApiConfig.DailyLogin.Process);


    // 출석 처리 요청 — POST /api/dailylogin (요청 본문 없음)
    // 이미 오늘 출석한 경우 rewarded=false 응답 (에러 아님)
    public void Process(
        Action<DailyLoginResponse> onSuccess, Action<ApiError> onError = null)
    {
        // 본문 없는 POST 오버로드 사용
        ApiClient.Instance.Post<DailyLoginResponse>(
            ApiConfig.DailyLogin.Process, onSuccess, onError);
    }
}

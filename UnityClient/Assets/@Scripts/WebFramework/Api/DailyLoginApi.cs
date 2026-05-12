using System.Threading.Tasks;

// 일일 출석 API — 매일 1회 출석 보상 처리
public static class DailyLoginApi
{
    // 출석 처리 비동기 버전 — 요청 본문 없는 POST, 이미 출석 시 rewarded=false 응답
    public static Task<ApiResult<DailyLoginResponse>> ProcessAsync()
        => ApiClient.Instance.PostAsync<DailyLoginResponse>(ApiConfig.DailyLogin.Process);
}

using System;

// 일일 출석 API 싱글톤 — 매일 1회 출석 보상 처리
public class DailyLoginApi : Singleton<DailyLoginApi>
{
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

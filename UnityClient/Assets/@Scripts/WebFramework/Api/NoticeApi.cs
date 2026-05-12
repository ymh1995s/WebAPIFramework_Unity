using System;
using System.Threading.Tasks;

// 공지사항 API 싱글톤 — 최신 공지 1건 조회 (백엔드는 GetLatest 1개만 제공)
public class NoticeApi : Singleton<NoticeApi>
{
    // ── Task 기반 비동기 정적 메서드 (await 호출용) ──────────────────────────

    // 최신 공지 조회 비동기 버전 — 활성 공지가 없으면 204 응답으로 null 반환 가능
    public static Task<ApiResult<NoticeDto>> GetLatestAsync()
        => ApiClient.Instance.GetAsync<NoticeDto>(ApiConfig.Notice.Latest);


    // 가장 최신 공지사항 1건 조회 — 활성 공지가 없으면 204 응답으로 onSuccess(null) 가능
    public void GetLatest(
        Action<NoticeDto> onSuccess, Action<ApiError> onError = null)
    {
        ApiClient.Instance.Get<NoticeDto>(
            ApiConfig.Notice.Latest, onSuccess, onError);
    }
}

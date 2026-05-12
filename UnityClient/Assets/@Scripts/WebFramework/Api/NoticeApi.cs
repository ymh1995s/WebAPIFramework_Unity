using System.Threading.Tasks;

// 공지사항 API — 최신 공지 1건 조회 (백엔드는 GetLatest 1개만 제공)
public static class NoticeApi
{
    // 최신 공지 조회 비동기 버전 — 활성 공지가 없으면 204 응답으로 null 반환 가능
    public static Task<ApiResult<NoticeDto>> GetLatestAsync()
        => ApiClient.Instance.GetAsync<NoticeDto>(ApiConfig.Notice.Latest);
}

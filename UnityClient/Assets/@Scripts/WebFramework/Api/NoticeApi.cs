using System;

// 공지사항 API 싱글톤 — 최신 공지 1건 조회 (백엔드는 GetLatest 1개만 제공)
public class NoticeApi : Singleton<NoticeApi>
{
    // 가장 최신 공지사항 1건 조회 — 활성 공지가 없으면 204 응답으로 onSuccess(null) 가능
    public void GetLatest(
        Action<NoticeDto> onSuccess, Action<ApiError> onError = null)
    {
        ApiClient.Instance.Get<NoticeDto>(
            ApiConfig.Notice.Latest, onSuccess, onError);
    }
}

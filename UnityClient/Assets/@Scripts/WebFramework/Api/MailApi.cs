using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 메일함 API 싱글톤 — 메일 목록 조회 및 보상 수령
public class MailApi : Singleton<MailApi>
{
    // ── Task 기반 비동기 정적 메서드 (await 호출용) ──────────────────────────

    // 메일 목록 조회 비동기 버전 — 본인 메일함 전체 반환
    public static Task<ApiResult<List<MailDto>>> GetListAsync()
        => ApiClient.Instance.GetListAsync<MailDto>(ApiConfig.Mail.List);

    // 메일 보상 수령 비동기 버전 — mailId를 경로에 바인딩, 요청 본문 없음
    public static Task<ApiResult<EmptyResponse>> ClaimAsync(int mailId)
        => ApiClient.Instance.PostAsync<EmptyResponse>(string.Format(ApiConfig.Mail.Claim, mailId));


    // 본인 메일함 전체 목록 조회 — GET /api/mails (백엔드가 최상위 배열 반환)
    public void GetList(
        Action<List<MailDto>> onSuccess, Action<ApiError> onError = null)
    {
        ApiClient.Instance.GetList<MailDto>(
            ApiConfig.Mail.List, onSuccess, onError);
    }

    // 특정 메일 보상 수령 — POST /api/mails/{mailId}/claim (요청 본문 없음)
    public void Claim(int mailId,
        Action<EmptyResponse> onSuccess, Action<ApiError> onError = null)
    {
        // mailId를 URL 경로에 바인딩
        string endpoint = string.Format(ApiConfig.Mail.Claim, mailId);
        ApiClient.Instance.Post<EmptyResponse>(endpoint, onSuccess, onError);
    }
}

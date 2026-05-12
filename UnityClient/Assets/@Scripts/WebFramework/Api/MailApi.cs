using System.Collections.Generic;
using System.Threading.Tasks;

// 메일함 API — 메일 목록 조회 및 보상 수령
public static class MailApi
{
    // 메일 목록 조회 비동기 버전 — 본인 메일함 전체 반환
    public static Task<ApiResult<List<MailDto>>> GetListAsync()
        => ApiClient.Instance.GetListAsync<MailDto>(ApiConfig.Mail.List);

    // 메일 보상 수령 비동기 버전 — mailId를 경로에 바인딩, 요청 본문 없음
    public static Task<ApiResult<EmptyResponse>> ClaimAsync(int mailId)
        => ApiClient.Instance.PostAsync<EmptyResponse>(string.Format(ApiConfig.Mail.Claim, mailId));
}

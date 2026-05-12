using System.Collections.Generic;
using System.Threading.Tasks;

// 문의 API — 문의 제출 및 목록 조회
public static class InquiryApi
{
    // 문의 제출 비동기 버전 — content를 본문으로 POST
    public static Task<ApiResult<EmptyResponse>> SubmitAsync(string content)
        => ApiClient.Instance.PostAsync<InquirySubmitRequest, EmptyResponse>(
            ApiConfig.Inquiry.Submit, new InquirySubmitRequest { content = content });

    // 문의 목록 조회 비동기 버전 — 본인이 제출한 문의 전체 반환
    public static Task<ApiResult<List<InquiryDto>>> GetListAsync()
        => ApiClient.Instance.GetListAsync<InquiryDto>(ApiConfig.Inquiry.List);
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 문의 API 싱글톤 — 문의 제출 및 목록 조회
public class InquiryApi : Singleton<InquiryApi>
{
    // ── Task 기반 비동기 정적 메서드 (await 호출용) ──────────────────────────

    // 문의 제출 비동기 버전 — content를 본문으로 POST
    public static Task<ApiResult<EmptyResponse>> SubmitAsync(string content)
        => ApiClient.Instance.PostAsync<InquirySubmitRequest, EmptyResponse>(
            ApiConfig.Inquiry.Submit, new InquirySubmitRequest { content = content });

    // 문의 목록 조회 비동기 버전 — 본인이 제출한 문의 전체 반환
    public static Task<ApiResult<List<InquiryDto>>> GetListAsync()
        => ApiClient.Instance.GetListAsync<InquiryDto>(ApiConfig.Inquiry.List);


    // 문의 내용 제출 — POST /api/inquiries
    public void Submit(string content,
        Action<EmptyResponse> onSuccess, Action<ApiError> onError = null)
    {
        var request = new InquirySubmitRequest { content = content };
        ApiClient.Instance.Post<InquirySubmitRequest, EmptyResponse>(
            ApiConfig.Inquiry.Submit, request, onSuccess, onError);
    }

    // 본인이 제출한 문의 목록 조회 — GET /api/inquiries (백엔드가 최상위 배열 반환)
    public void GetList(
        Action<List<InquiryDto>> onSuccess, Action<ApiError> onError = null)
    {
        ApiClient.Instance.GetList<InquiryDto>(
            ApiConfig.Inquiry.List, onSuccess, onError);
    }
}

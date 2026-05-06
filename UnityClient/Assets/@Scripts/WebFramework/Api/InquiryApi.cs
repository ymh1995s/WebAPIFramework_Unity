using System;
using System.Collections.Generic;

// 문의 API 싱글톤 — 문의 제출 및 목록 조회
public class InquiryApi : Singleton<InquiryApi>
{
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
